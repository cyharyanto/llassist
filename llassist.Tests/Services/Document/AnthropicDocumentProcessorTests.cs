using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using DotNetWorkQueue;
using DotNetWorkQueue.Messages;
using llassist.Common;
using llassist.Common.Models;
using llassist.ApiService.Repositories;
using llassist.ApiService.Repositories.Specifications;
using llassist.ApiService.Services.Document;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace llassist.Tests.Services.Document;

public class AnthropicDocumentProcessorTests
{
    private readonly Mock<IProducerQueue<DocumentProcessingTask>> _mockProducerQueue;
    private readonly Mock<ICRUDRepository<Ulid, DocumentProcessingJob, BaseSearchSpec>> _mockJobRepository;
    private readonly Mock<ILogger<AnthropicDocumentProcessor>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;

    public AnthropicDocumentProcessorTests()
    {
        _mockProducerQueue = new Mock<IProducerQueue<DocumentProcessingTask>>();
        _mockJobRepository = new Mock<ICRUDRepository<Ulid, DocumentProcessingJob, BaseSearchSpec>>();
        _mockLogger = new Mock<ILogger<AnthropicDocumentProcessor>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
    }

    private AnthropicDocumentProcessor CreateService()
    {
        return new AnthropicDocumentProcessor(_mockProducerQueue.Object, _mockJobRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateProcessingJobAsync_ShouldCreateJobAndQueueTasks()
    {
        // Arrange
        var service = CreateService();
        var content = Encoding.UTF8.GetBytes("Test PDF content");
        var stream = new MemoryStream(content);
        
        _mockJobRepository.Setup(r => r.CreateAsync(It.IsAny<DocumentProcessingJob>()))
            .ReturnsAsync((DocumentProcessingJob job) => job);
        
        _mockProducerQueue.Setup(q => q.SendAsync(It.IsAny<DocumentProcessingTask>(), null))
            .ReturnsAsync(new QueueOutputMessage(Mock.Of<ISentMessage>(), null));

        // Act
        var result = await service.CreateProcessingJobAsync(stream, "test.pdf", "application/pdf");

        // Assert
        Xunit.Assert.NotNull(result);
        Xunit.Assert.Equal(3, result.TotalTasks);
        _mockJobRepository.Verify(r => r.CreateAsync(It.IsAny<DocumentProcessingJob>()), Times.Once);
        _mockProducerQueue.Verify(q => q.SendAsync(It.IsAny<DocumentProcessingTask>(), null), Times.Exactly(3));
    }

    [Fact]
    public async Task ExecuteTaskAsync_MetadataTask_ShouldProcessAndUpdateJob()
    {
        // Arrange
        var service = CreateService();
        var job = CreateSampleJob();
        var task = new DocumentProcessingTask 
        { 
            Id = Ulid.NewUlid(),
            JobId = job.Id,
            Type = DocumentTaskType.METADATA 
        };

        SetupMockAnthropicResponse("<metadata><title>Test</title></metadata>");
        _mockJobRepository.Setup(r => r.ReadAsync(job.Id)).ReturnsAsync(job);
        _mockJobRepository.Setup(r => r.UpdateAsync(It.IsAny<DocumentProcessingJob>()))
            .ReturnsAsync((DocumentProcessingJob j) => j);

        // Act
        await service.ExecuteTaskAsync(task);

        // Assert
        _mockJobRepository.Verify(r => r.UpdateAsync(It.Is<DocumentProcessingJob>(j => 
            j.CompletedTasks == 1 && 
            j.Parameters.ContainsKey("metadata"))), Times.Once);
    }

    private void SetupMockAnthropicResponse(string response)
    {
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new { content = response }))
            });
    }

    private static DocumentProcessingJob CreateSampleJob()
    {
        return new DocumentProcessingJob
        {
            Id = Ulid.NewUlid(),
            FileName = "test.pdf",
            ContentType = "application/pdf",
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test content")),
            TotalTasks = 3
        };
    }
} 