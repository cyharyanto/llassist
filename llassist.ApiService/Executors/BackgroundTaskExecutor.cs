using DotNetWorkQueue;
using llassist.ApiService.Services;
using llassist.Common.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace llassist.ApiService.Executors;

public class BackgroundTaskExecutor
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConsumerQueue _consumerQueue;
    private readonly ILogger<BackgroundTaskExecutor> _logger;
    private readonly QueueOptions _queueOptions;

    public BackgroundTaskExecutor(
        IServiceScopeFactory serviceScopeFactory,
        IConsumerQueue consumerQueue,
        ILogger<BackgroundTaskExecutor> logger,
        IOptions<QueueOptions> queueOptions)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _consumerQueue = consumerQueue;
        _logger = logger;
        _queueOptions = queueOptions.Value;

        ConfigureConsumerQueue(_consumerQueue, _queueOptions, _logger);

        _consumerQueue.Start<BackgroundTask>(HandleMessages, CreateNotifications.Create(_logger));
    }

    private static void ConfigureConsumerQueue(IConsumerQueue consumerQueue, QueueOptions queueOptions, ILogger<BackgroundTaskExecutor> logger)
    {
        logger.LogInformation("Configuring ConsumerQueue with: {config}", JsonSerializer.Serialize(queueOptions));

        consumerQueue.Configuration.HeartBeat.UpdateTime = queueOptions.HeartBeatUpdateTime;
        consumerQueue.Configuration.Worker.WorkerCount = queueOptions.ConsumerWorkerCount;
        consumerQueue.Configuration.HeartBeat.UpdateTime = queueOptions.HeartBeatUpdateTime;
        consumerQueue.Configuration.HeartBeat.MonitorTime = TimeSpan.FromSeconds(queueOptions.HeartBeatMonitorTimeInSec);
        consumerQueue.Configuration.HeartBeat.Time = TimeSpan.FromSeconds(queueOptions.HeartBeatTimeInSec);
        consumerQueue.Configuration.MessageExpiration.Enabled = queueOptions.EnableMessageExpiration;
        consumerQueue.Configuration.MessageExpiration.MonitorTime = TimeSpan.FromSeconds(queueOptions.MessageExpirationMonitorTimeInSec);
        consumerQueue.Configuration.TransportConfiguration.RetryDelayBehavior.Add(typeof(InvalidDataException),
            [TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(9)]); // TODO move hardcoded values to config
    }

    public void HandleMessages(IReceivedMessage<BackgroundTask> message, IWorkerNotification notifications)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var estimateRelevanceProcessingService = scope.ServiceProvider.GetRequiredService<IEstimateRelevanceService>();
        estimateRelevanceProcessingService.HandleMessages(message, notifications).Wait();
    }
}
