using DotNetWorkQueue;
using llassist.ApiService.Repositories.Specifications;
using llassist.ApiService.Services;
using llassist.Common.Models;
using llassist.Common;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Assert = Xunit.Assert;
using System.Threading.Tasks;

namespace llassist.Tests;

public class EstimateRelevanceServiceTests
{
    private readonly Mock<IProducerQueue<BackgroundTask>> _mockProducerQueue;
    private readonly Mock<ILogger<EstimateRelevanceService>> _mockLogger;
    private readonly Mock<ICRUDRepository<Ulid, Article, ArticleSearchSpec>> _mockArticleRepository;
    private readonly Mock<ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec>> _mockJobRepository;
    private readonly Mock<IArticleRelevanceService> _mockArticleService;
    private readonly Mock<INLPService> _mockNlpService;

    public EstimateRelevanceServiceTests()
    {
        _mockProducerQueue = new Mock<IProducerQueue<BackgroundTask>>();
        _mockLogger = new Mock<ILogger<EstimateRelevanceService>>();
        _mockArticleRepository = new Mock<ICRUDRepository<Ulid, Article, ArticleSearchSpec>>();
        _mockJobRepository = new Mock<ICRUDRepository<Ulid, EstimateRelevanceJob, EstimateRelevanceJobSearchSpec>>();
        _mockArticleService = new Mock<IArticleRelevanceService>();
        _mockNlpService = new Mock<INLPService>();
    }

    [Fact]
    public async Task HandleMessages_PreprocessingTask_ShouldExecutePreprocessingTask()
    {
        // Arrange
        var service = CreateService();
        var task = new BackgroundTask { TaskType = TaskType.PREPROCESSING, ProjectId = Ulid.NewUlid() };
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        var articles = new List<Article> { new Article(), new Article() };
        _mockArticleRepository
            .Setup(x => x.ReadWithSearchSpecAsync(It.IsAny<ArticleSearchSpec>()))
            .ReturnsAsync(articles);

        _mockProducerQueue
            .Setup(x => x.SendAsync(It.IsAny<List<BackgroundTask>>()))
            .ReturnsAsync(Mock.Of<IQueueOutputMessages>());

        // Act
        await Task.Run(() => service.HandleMessages(message, notifications));

        // Assert
        _mockProducerQueue.Verify(
            x => x.SendAsync(It.Is<List<BackgroundTask>>(tasks => tasks.Count == 2 && tasks.All(t => t.TaskType == TaskType.EXECUTION))),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleMessages_ExecutionTask_ShouldExecuteExecutionTask()
    {
        // Arrange
        var service = CreateService();
        var articleId = Ulid.NewUlid();
        var task = new BackgroundTask
        {
            TaskType = TaskType.EXECUTION,
            ArticleId = articleId,
            ResearchQuestions = new List<ResearchQuestionDTO> {
                new ResearchQuestionDTO { Question = "Test Question", CombinedDefinitions = [ "Test-Definition" ] }
            },
            EstimateRelevanceJobId = Ulid.NewUlid()
        };
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        var article = new Article { Id = articleId };
        _mockArticleRepository
            .Setup(x => x.ReadAsync(articleId))
            .ReturnsAsync(article);

        _mockNlpService
            .Setup(x => x.ExtractKeySemantics(It.IsAny<string>()))
            .ReturnsAsync(new KeySemantics());

        _mockNlpService
            .Setup(x => x.EstimateRevelance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(new Relevance { Question = "Test Question" });

        _mockArticleService
            .Setup(x => x.InsertRelevancesAndUpdateJobProgress(It.IsAny<Ulid>(), It.IsAny<Ulid>(), It.IsAny<List<Relevance>>()))
            .ReturnsAsync(true);

        var job = new EstimateRelevanceJob { TotalArticles = 2, CompletedArticles = 1 };
        _mockJobRepository.Setup(x => x.ReadAsync(task.EstimateRelevanceJobId)).ReturnsAsync(job);

        // Act
        await Task.Run(() => service.HandleMessages(message, notifications));

        // Assert
        _mockArticleRepository.Verify(x => x.UpdateAsync(It.IsAny<Article>()), Times.Once);
        _mockNlpService.Verify(x => x.ExtractKeySemantics(It.IsAny<string>()), Times.Once);
        _mockNlpService.Verify(x => x.EstimateRevelance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);

        _mockArticleService.Verify(x => x.InsertRelevancesAndUpdateJobProgress(
             It.Is<Ulid>(id => id == articleId),
             It.Is<Ulid>(id => id == task.EstimateRelevanceJobId),
             It.Is<List<Relevance>>(relevances =>
                 relevances.Count == task.ResearchQuestions.Count &&
                 relevances.First().Question == "Test Question"
             )
         ), Times.Once);

        _mockArticleRepository.Verify(x => x.UpdateAsync(It.Is<Article>(a =>
            a.Id == articleId &&
            a.ArticleKeySemantics != null &&
            a.MustRead == false
        )), Times.Once);

        _mockProducerQueue.Verify(
            x => x.SendAsync(It.IsAny<BackgroundTask>(), null),
            Times.Never
        );
    }

    [Fact]
    public async Task HandleMessages_ExecutionTask_AlreadyProcessed_ShouldNotProcessAndUpdateArticle()
    {
        // Arrange
        var service = CreateService();
        var articleId = Ulid.NewUlid();
        var jobId = Ulid.NewUlid();
        var task = new BackgroundTask
        {
            TaskType = TaskType.EXECUTION,
            ArticleId = articleId,
            EstimateRelevanceJobId = jobId,
            ResearchQuestions = new List<ResearchQuestionDTO> {
                new ResearchQuestionDTO { Question = "Test Question", CombinedDefinitions = [ "Test-Definition" ] }
            }
        };
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        var article = new Article 
        { 
            Id = articleId, 
            ArticleRelevances =
            [
                new ArticleRelevance { EstimateRelevanceJobId = jobId }
            ]
        };
        _mockArticleRepository
            .Setup(x => x.ReadAsync(articleId))
            .ReturnsAsync(article);

        // Act
        await Task.Run(() => service.HandleMessages(message, notifications));

        // Assert
        _mockNlpService.Verify(x => x.ExtractKeySemantics(It.IsAny<string>()), Times.Never);
        _mockNlpService.Verify(x => x.EstimateRevelance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        _mockArticleRepository.Verify(x => x.UpdateAsync(It.IsAny<Article>()), Times.Never);
        _mockArticleService.Verify(x => x.InsertRelevancesAndUpdateJobProgress(
             It.IsAny<Ulid>(), It.IsAny<Ulid>(), It.IsAny<List<Relevance>>()), Times.Never);
    }

    [Fact]
    public async Task HandleMessages_ExecutionTask_ArticleNotFound_ShouldNotProcessAndUpdateArticle()
    {
        // Arrange
        var service = CreateService();
        var articleId = Ulid.NewUlid();
        var task = new BackgroundTask
        {
            TaskType = TaskType.EXECUTION,
            ArticleId = articleId,
        };
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        _mockArticleRepository
            .Setup(x => x.ReadAsync(articleId))
            .ReturnsAsync((Article)null);

        // Act
        await Task.Run(() => service.HandleMessages(message, notifications));

        // Assert
        _mockNlpService.Verify(x => x.ExtractKeySemantics(It.IsAny<string>()), Times.Never);
        _mockNlpService.Verify(x => x.EstimateRevelance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        _mockArticleRepository.Verify(x => x.UpdateAsync(It.IsAny<Article>()), Times.Never);
        _mockArticleService.Verify(x => x.InsertRelevancesAndUpdateJobProgress(
             It.IsAny<Ulid>(), It.IsAny<Ulid>(), It.IsAny<List<Relevance>>()), Times.Never);
    }

    [Fact]
    public async Task HandleMessages_InvalidTaskType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var task = new BackgroundTask { TaskType = (TaskType)999 }; // Invalid TaskType
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.HandleMessages(message, notifications));
    }

    [Fact]
    public async Task HandleMessages_FinalizationTask_ShouldExecuteFinalizationTask()
    {
        // Arrange
        var service = CreateService();
        var task = new BackgroundTask { TaskType = TaskType.FINALIZATION, ProjectId = Ulid.NewUlid() };
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        // Act
        await Task.Run(() => service.HandleMessages(message, notifications));

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Executing FINALIZATION task")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleMessages_ExecutionTask_ShouldCreateFinalizationTaskWhenAllArticlesProcessed()
    {
        // Arrange
        var service = CreateService();
        var articleId = Ulid.NewUlid();
        var jobId = Ulid.NewUlid();
        var task = new BackgroundTask
        {
            TaskType = TaskType.EXECUTION,
            ArticleId = articleId,
            EstimateRelevanceJobId = jobId,
            ResearchQuestions = new List<ResearchQuestionDTO> {
                new ResearchQuestionDTO { Question = "Test Question", CombinedDefinitions = [ "Test-Definition" ] }
            }
        };
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        var article = new Article { Id = articleId };
        _mockArticleRepository.Setup(x => x.ReadAsync(articleId)).ReturnsAsync(article);
        _mockNlpService.Setup(x => x.ExtractKeySemantics(It.IsAny<string>())).ReturnsAsync(new KeySemantics());
        _mockNlpService.Setup(x => x.EstimateRevelance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
            .ReturnsAsync(new Relevance { Question = "Test Question" });
        _mockArticleService.Setup(x => x.InsertRelevancesAndUpdateJobProgress(It.IsAny<Ulid>(), It.IsAny<Ulid>(), It.IsAny<List<Relevance>>()))
            .ReturnsAsync(true);

        var job = new EstimateRelevanceJob { TotalArticles = 1, CompletedArticles = 1 };
        _mockJobRepository.Setup(x => x.ReadAsync(jobId)).ReturnsAsync(job);

        // Act
        await Task.Run(() => service.HandleMessages(message, notifications));

        // Assert
        _mockProducerQueue.Verify(
            x => x.SendAsync(It.Is<BackgroundTask>(t =>
                t.TaskType == TaskType.FINALIZATION &&
                t.EstimateRelevanceJobId == jobId), null),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleMessages_ExecutionTask_ShouldHandleNLPServiceException()
    {
        // Arrange
        var service = CreateService();
        var articleId = Ulid.NewUlid();
        var task = new BackgroundTask
        {
            TaskType = TaskType.EXECUTION,
            ArticleId = articleId,
            ResearchQuestions = new List<ResearchQuestionDTO> {
                new ResearchQuestionDTO { Question = "Test Question", CombinedDefinitions = [ "Test-Definition" ] }
            }
        };
        var message = CreateMockReceivedMessage(task);
        var notifications = Mock.Of<IWorkerNotification>();

        var article = new Article { Id = articleId };
        _mockArticleRepository.Setup(x => x.ReadAsync(articleId)).ReturnsAsync(article);
        _mockNlpService.Setup(x => x.ExtractKeySemantics(It.IsAny<string>()))
            .ThrowsAsync(new Exception("NLP Service Error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.HandleMessages(message, notifications));

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Failed to extract semantics for Article")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private EstimateRelevanceService CreateService()
    {
        return new EstimateRelevanceService(
            _mockLogger.Object,
            _mockProducerQueue.Object,
            _mockArticleRepository.Object,
            _mockJobRepository.Object,
            _mockArticleService.Object,
            _mockNlpService.Object
        );
    }

    private static IReceivedMessage<BackgroundTask> CreateMockReceivedMessage(BackgroundTask task)
    {
        var mockMessage = new Mock<IReceivedMessage<BackgroundTask>>();
        mockMessage.Setup(x => x.Body).Returns(task);
        return mockMessage.Object;
    }
}