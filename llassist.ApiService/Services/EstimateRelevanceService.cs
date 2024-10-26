using DotNetWorkQueue;
using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Mappers;
using llassist.Common.Models;
using System.Text.Json;

namespace llassist.ApiService.Services;

public interface IEstimateRelevanceService
{
    public Task HandleMessages(IReceivedMessage<BackgroundTask> message, IWorkerNotification notifications);
}

public class EstimateRelevanceService : IEstimateRelevanceService
{
    private readonly IProducerQueue<BackgroundTask> _producerQueue;
    private readonly ILogger<EstimateRelevanceService> _logger;
    private readonly ICRUDRepository<Ulid, Article, ArticleSearchSpec> _articleRepository;
    private readonly ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec> _jobRepository;
    private readonly IArticleRelevanceService _articleRelevanceService;
    private readonly INLPService _nlpService;

    public EstimateRelevanceService(
        ILogger<EstimateRelevanceService> logger,
        IProducerQueue<BackgroundTask> producerQueue,
        ICRUDRepository<Ulid, Article, ArticleSearchSpec> articleRepository,
        ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec> jobRepository,
        IArticleRelevanceService articleRelevanceService,
        INLPService nlpService)
    {
        _producerQueue = producerQueue;
        _logger = logger;
        _articleRepository = articleRepository;
        _jobRepository = jobRepository;
        _articleRelevanceService = articleRelevanceService;
        _nlpService = nlpService;
    }

    public async Task HandleMessages(IReceivedMessage<BackgroundTask> message, IWorkerNotification notifications)
    {
        try
        {
            switch (message.Body.TaskType)
            {
                case TaskType.PREPROCESSING:
                    await ExecutePreprocessingTask(message.Body);
                    break;
                case TaskType.EXECUTION:
                    await ExecuteExecutionTask(message.Body);
                    break;
                case TaskType.FINALIZATION:
                    ExecuteFinalizationTask(message.Body);
                    break;
                default:
                    throw new InvalidOperationException("Invalid TaskType value");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle message: {message}", JsonSerializer.Serialize(message));

            if (ex is InvalidOperationException)
            {
                throw;
            }
            else
            {
                throw new InvalidDataException("Failed to handle message", ex);
            }
        }

    }

    private async Task ExecutePreprocessingTask(BackgroundTask task)
    {
        _logger.LogInformation("Executing PREPROCESSING task {task}", JsonSerializer.Serialize(task));

        var articleList = await _articleRepository.ReadWithSearchSpecAsync(
            new ArticleSearchSpec { ProjectId = task.ProjectId });

        await _producerQueue.SendAsync(CreateExecutionTasks(task, articleList));
    }

    private static List<BackgroundTask> CreateExecutionTasks(BackgroundTask preprocessingTask, IEnumerable<Article> articles)
    {
        // Create individual execution tasks for each article
        return articles.Select(article => new BackgroundTask
        {
            EstimateRelevanceJobId = preprocessingTask.EstimateRelevanceJobId,
            ModelName = preprocessingTask.ModelName,
            TaskType = TaskType.EXECUTION,
            ProjectId = preprocessingTask.ProjectId,
            ArticleId = article.Id,
            ResearchQuestions = preprocessingTask.ResearchQuestions,
        }).ToList();
    }

    private async Task ExecuteExecutionTask(BackgroundTask task)
    {
        _logger.LogInformation("Executing EXECUTION task {task}", JsonSerializer.Serialize(task));

        var article = await FetchAndValidateArticle(task.ArticleId, task.EstimateRelevanceJobId);
        if (article == null) return;


        var keySemantics = await ExtractKeySemantics(article);
        var relevanceList = await EstimateRelevances(task, article, keySemantics);
        var relevancesInserted = await UpdateArticleRelevance(article, relevanceList, task.EstimateRelevanceJobId);

        if (relevancesInserted)
        {
            await UpdateArticle(article, keySemantics, relevanceList);

            await CheckAndCreateFinalizationTask(task);
        }
    }

    private async Task<Article?> FetchAndValidateArticle(Ulid articleId, Ulid jobId)
    {
        var article = await _articleRepository.ReadAsync(articleId);

        if (article == null)
        {
            _logger.LogError("Failed to find article with id: {articleId}", articleId);
            return null;
        }

        if (article.ArticleRelevances.Any(relevance => relevance.EstimateRelevanceJobId == jobId))
        {
            _logger.LogInformation("Article {articleId} with Job {jobId} already processed", articleId, jobId);
            return null;
        }

        return article;
    }

    private async Task<KeySemantics> ExtractKeySemantics(Article article)
    {
        try
        {
            _logger.LogInformation("Extracting semantics for Article: {articleId}", article.Id);
            return await _nlpService.ExtractKeySemantics($"Title: {article.Title}\n Abstract: {article.Abstract}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract semantics for Article: {articleId}", article.Id);
            throw new InvalidOperationException("Failed to extract semantics for Article", ex);
        }
    }
    private async Task<List<Relevance>> EstimateRelevances(BackgroundTask task, Article article, KeySemantics keySemantics)
    {
        var relevanceList = new List<Relevance>();
        foreach (var researchQuestion in task.ResearchQuestions)
        {
            var relevance = await EstimateRelevance(article, researchQuestion.Question, [.. researchQuestion.CombinedDefinitions], keySemantics);
            relevanceList.Add(relevance);
        }
        return relevanceList;
    }

    private async Task<Relevance> EstimateRelevance(
        Article article,
        string questionText,
        string[] combinedDefinitions,
        KeySemantics keySemantics)
    {
        try
        {
            _logger.LogInformation("Estimating relevance for Article: {articleId} and ResearchQuestion: {question}",
                article.Id, questionText);

            return await _nlpService.EstimateRevelance(
                $"Title: {article.Title}\n Abstract: {article.Abstract} \n Metadata: {JsonSerializer.Serialize(keySemantics)}",
                "abstract", questionText, combinedDefinitions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to estimate relevance for Article: {articleId}", article.Id);
            throw new InvalidOperationException("Failed to estimate relevance for Article", ex);
        }
    }

    private async Task<bool> UpdateArticleRelevance(Article article, List<Relevance> relevanceList, Ulid jobId)
    {
        var writeSuccessful = await _articleRelevanceService.InsertRelevancesAndUpdateJobProgress(article.Id, jobId, relevanceList);

        _logger.LogInformation("ArticleRelevance for Article: {articleId} and Job: {jobId} successfully inserted: {writeSuccessful}",
            article.Id, jobId, writeSuccessful);

        return writeSuccessful;
    }
    private async Task UpdateArticle(Article article, KeySemantics keySemantics, List<Relevance> relevanceList)
    {
        article.ArticleKeySemantics = ModelMappers.ToArticleKeySemantics(article.Id, keySemantics);
        article.MustRead = relevanceList.Any(r => r.IsRelevant || r.IsContributing);
        await _articleRepository.UpdateAsync(article);
    }

    private async Task CheckAndCreateFinalizationTask(BackgroundTask task)
    {
        var job = await _jobRepository.ReadAsync(task.EstimateRelevanceJobId);

        // If all articles are processed, create finalization task
        if (job != null && job.TotalArticles == job.CompletedArticles)
        {
            await _producerQueue.SendAsync(new BackgroundTask
            {
                EstimateRelevanceJobId = task.EstimateRelevanceJobId,
                ModelName = task.ModelName,
                TaskType = TaskType.FINALIZATION,
                ProjectId = task.ProjectId,
            });
        }
    }

    private void ExecuteFinalizationTask(BackgroundTask body)
    {
        _logger.LogInformation("Executing FINALIZATION task {task}", JsonSerializer.Serialize(body));
    }
}
