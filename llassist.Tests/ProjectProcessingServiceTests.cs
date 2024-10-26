using DotNetWorkQueue;
using llassist.ApiService.Repositories.Specifications;
using llassist.ApiService.Services;
using llassist.Common.Models;
using llassist.Common;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;
using Assert = Xunit.Assert;
using DotNetWorkQueue.Messages;
using llassist.Common.Mappers;
using llassist.Common.ViewModels;

namespace llassist.Tests;

public class ProjectProcessingServiceTests
{
    private readonly Mock<IProducerQueue<BackgroundTask>> _mockProducerQueue;
    private readonly Mock<ILogger<ProjectProcessingService>> _mockLogger;
    private readonly Mock<ICRUDRepository<Ulid, Project, BaseSearchSpec>> _mockProjectRepository;
    private readonly Mock<ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec>> _mockJobRepository;
    private readonly ProjectProcessingService _service;

    public ProjectProcessingServiceTests()
    {
        _mockProducerQueue = new Mock<IProducerQueue<BackgroundTask>>();
        _mockLogger = new Mock<ILogger<ProjectProcessingService>>();
        _mockProjectRepository = new Mock<ICRUDRepository<Ulid, Project, BaseSearchSpec>>();
        _mockJobRepository = new Mock<ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec>>();

        _service = new ProjectProcessingService(_mockProducerQueue.Object, _mockProjectRepository.Object,
            _mockJobRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task EnqueuePreprocessingTask_ShouldEnqueueTaskAndCreateJob_WhenProjectExists()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var project = CreateSampleProject(projectId);
        var estimateRelevanceJob = CreateSampleEstimateRelevanceJob(project);

        _mockProjectRepository.Setup(r => r.ReadAsync(projectId)).ReturnsAsync(project);
        _mockJobRepository.Setup(r => r.CreateAsync(It.IsAny<EstimateRelevanceJob>())).ReturnsAsync(estimateRelevanceJob);
        _mockProducerQueue.Setup(q => q.SendAsync(It.IsAny<BackgroundTask>(), null))
            .ReturnsAsync(new QueueOutputMessage(It.IsAny<ISentMessage>(), null));

        // Act
        await _service.EnqueuePreprocessingTask(projectId);

        // Assert
        _mockProjectRepository.Verify(r => r.ReadAsync(projectId), Times.Once);
        _mockProducerQueue.Verify(q => q.SendAsync(It.IsAny<BackgroundTask>(), null), Times.Once);
        _mockJobRepository.Verify(r => r.CreateAsync(It.IsAny<EstimateRelevanceJob>()), Times.Once);
    }

    [Fact]
    public async Task EnqueuePreprocessingTask_ShouldThrowException_WhenProjectNotFound()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        _mockProjectRepository.Setup(r => r.ReadAsync(projectId)).ReturnsAsync((Project)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(() => _service.EnqueuePreprocessingTask(projectId));
    }

    [Fact]
    public async Task GetJobProgress_ProjectNotFound_ReturnsEmptyProcess()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        _mockProjectRepository.Setup(r => r.ReadAsync(projectId)).ReturnsAsync((Project)null);
        _mockJobRepository.Setup(r => r.ReadWithSearchSpecAsync(It.IsAny<EstimateRelevanceJobSearchSpec>()))
            .ReturnsAsync(new List<EstimateRelevanceJob>());

        // Act
        var result = await _service.GetJobProgress(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Progress);
        Assert.Empty(result.ProcessedArticles);
    }

    [Fact]
    public async Task GetJobProgress_NoPreviousJob_ReturnsEmptyProcess()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var project = CreateSampleProject(projectId);
        _mockProjectRepository.Setup(r => r.ReadAsync(projectId)).ReturnsAsync(project);
        _mockJobRepository.Setup(r => r.ReadWithSearchSpecAsync(It.IsAny<EstimateRelevanceJobSearchSpec>()))
            .ReturnsAsync(new List<EstimateRelevanceJob>());

        // Act
        var result = await _service.GetJobProgress(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Progress);
        Assert.Empty(result.ProcessedArticles);
    }

    [Fact]
    public async Task GetJobProgress_ReturnProcess()
    {
        // Arrange
        var projectId = Ulid.NewUlid();
        var project = CreateSampleProject(projectId);
        var job = CreateSampleEstimateRelevanceJob(project);
        job.TotalArticles = 3;

        var article1 = new Article { Id = Ulid.NewUlid(), Title = "Article 1", Authors = "Author 1", Year = 2021, Abstract = "Abstract 1" };
        var article2 = new Article { Id = Ulid.NewUlid(), Title = "Article 2", Authors = "Author 2", Year = 2022, Abstract = "Abstract 2" };
        var article3 = new Article { Id = Ulid.NewUlid(), Title = "Article 3", Authors = "Author 3", Year = 2023, Abstract = "Abstract 3" };

        var articleRelevance1 = new ArticleRelevance
        {
            EstimateRelevanceJobId = job.Id,
            ArticleId = article1.Id,
            Question = "Question 1",
            RelevanceScore = 0.8,
            IsRelevant = true,
            ContributionScore = 0.7,
            IsContributing = true,
            RelevanceReason = "Relevant reason",
            ContributionReason = "Contribution reason"
        };
        article1.ArticleRelevances.Add(articleRelevance1);

        project.Articles.Add(article1);
        project.Articles.Add(article2);

        _mockProjectRepository.Setup(r => r.ReadAsync(projectId)).ReturnsAsync(project);
        _mockJobRepository.Setup(r => r.ReadWithSearchSpecAsync(It.IsAny<EstimateRelevanceJobSearchSpec>()))
            .ReturnsAsync(new List<EstimateRelevanceJob> { job });

        // Act
        var result = await _service.GetJobProgress(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(33, result.Progress); // 1 out of 3 articles processed
        Assert.Single(result.ProcessedArticles);

        var processedArticle = result.ProcessedArticles[0];
        Assert.Equal(article1.Title, processedArticle.Title);
        Assert.Equal(article1.Authors, processedArticle.Authors);
        Assert.Equal(article1.Year, processedArticle.Year);
        Assert.Equal(article1.Abstract, processedArticle.Abstract);
        Assert.Equal(article1.MustRead, processedArticle.MustRead);
        Assert.Single(processedArticle.Relevances);

        var relevance = processedArticle.Relevances[0];
        Assert.Equal(articleRelevance1.Question, relevance.Question);
        Assert.Equal(articleRelevance1.RelevanceScore, relevance.RelevanceScore);
        Assert.Equal(articleRelevance1.IsRelevant, relevance.IsRelevant);
    }

    private static Project CreateSampleProject(Ulid projectId)
    {
        return new Project
        {
            Id = projectId,
            ProjectDefinitions =
            [
                new ProjectDefinition
                {
                    Definition = "Definition 1"
                },
                new ProjectDefinition
                {
                    Definition = "Definition 2"
                }
            ],
            ResearchQuestions =
            [
                new ResearchQuestion
                {
                    QuestionText = "Question 1",
                    QuestionDefinitions =
                    [
                        new QuestionDefinition { Definition = "Q1 Definition" }
                    ]
                },
                new ResearchQuestion
                {
                    QuestionText = "Question 2",
                    QuestionDefinitions =
                    [
                        new QuestionDefinition { Definition = "Q2 Definition" }
                    ]
                }
            ],
            Articles = new List<Article>()
        };
    }

    private static EstimateRelevanceJob CreateSampleEstimateRelevanceJob(Project project)
    {
        var job = new EstimateRelevanceJob
        {
            Id = Ulid.NewUlid(),
            ModelName = "stubbed-value",
            ProjectId = project.Id,
        };

        job.Snapshots = ModelMappers.ToSnapshots(job.Id, project.ProjectDefinitions, project.ResearchQuestions);

        return job;
    }
}