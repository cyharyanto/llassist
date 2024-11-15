using DotNetWorkQueue;
using llassist.ApiService.Repositories.Specifications;
using llassist.Common;
using llassist.Common.Mappers;
using llassist.Common.Models;
using llassist.Common.ViewModels;

namespace llassist.ApiService.Services;

public class ProjectProcessingService
{
    private readonly IProducerQueue<BackgroundTask> _producerQueue;
    private readonly ILogger<ProjectProcessingService> _logger;
    private readonly ICRUDRepository<Ulid, Project, BaseSearchSpec> _projectRepository;
    private readonly ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec> _jobRepository;

    public ProjectProcessingService(
        IProducerQueue<BackgroundTask> producerQueue,
        ICRUDRepository<Ulid, Project, BaseSearchSpec> projectRepository,
        ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec> jobRepository,
        ILogger<ProjectProcessingService> logger)
    {
        _producerQueue = producerQueue;
        _logger = logger;
        _projectRepository = projectRepository;
        _jobRepository = jobRepository;
    }

    public async Task EnqueuePreprocessingTask(Ulid projectId)
    {
        var project = await _projectRepository.ReadAsync(projectId);
        if (project == null)
        {
            _logger.LogError("Project {projectId} not found", projectId);
            throw new InvalidDataException("Project not found");
        }

        var estimateRelevanceJob = CreateEstimateRelevanceJob(project);

        _logger.LogInformation("Inserting job {jobId} for project {projectId}", estimateRelevanceJob.Id, estimateRelevanceJob.ProjectId);
        var createJobTask = _jobRepository.CreateAsync(estimateRelevanceJob);

        var enqueueResult = await _producerQueue.SendAsync(CreateBackgroundTask(estimateRelevanceJob.Id, project));
        _logger.LogInformation("Preprocessing task enqueue for Project: {projectId}, Job: {jobId} has error: {hasError}", 
            projectId, estimateRelevanceJob.Id, enqueueResult.HasError);

        var createdJob = await createJobTask;
        _logger.LogInformation("Inserted job {jobId} for project {projectId}", createdJob.Id, createdJob.ProjectId);
    }

    public async Task<ProcessResultViewModel> GetJobProgress(Ulid projectId)
    {
        var projectJobs = await _jobRepository.ReadWithSearchSpecAsync(
            new EstimateRelevanceJobSearchSpec { ProjectId = projectId });
        var project = await _projectRepository.ReadAsync(projectId);

        if (project == null || !projectJobs.Any())
        {
            return new ProcessResultViewModel();
        }

        var latestJob = projectJobs.OrderByDescending(j => j.Id).First();

        var processedArticles = project.Articles
            .Select(article =>
            {
                var filteredRelevances = article.ArticleRelevances
                    .Where(relevance => relevance.EstimateRelevanceJobId == latestJob.Id)
                    .ToList();

                return filteredRelevances.Count != 0 ? ModelMappers.ToArticleViewModel(article, filteredRelevances) : null;
            })
            .Where(articleViewModel => articleViewModel != null)
            .ToList();


        return new ProcessResultViewModel
        {
            JobId = latestJob.Id.ToString(),
            Progress = processedArticles.Count * 100 / latestJob.TotalArticles,
            ProcessedArticles = processedArticles,
        };
    }

    private static EstimateRelevanceJob CreateEstimateRelevanceJob(Project project)
    {
        var job = new EstimateRelevanceJob
        {
            ModelName = "stubbed-value",
            TotalArticles = project.Articles.Count,
            ProjectId = project.Id,
        };

        job.Snapshots = ModelMappers.ToSnapshots(job.Id, project.ProjectDefinitions, project.ResearchQuestions);

        return job;
    }

    private static BackgroundTask CreateBackgroundTask(Ulid jobId, Project project)
    {
        return new BackgroundTask
        {
            EstimateRelevanceJobId = jobId,
            ModelName = "stubbed-value",
            TaskType = TaskType.PREPROCESSING,
            ProjectId = project.Id,
            ResearchQuestions = ModelMappers.ToResearchQuestionDTOList(project.ProjectDefinitions, project.ResearchQuestions),
        };
    }
}
