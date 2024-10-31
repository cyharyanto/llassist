namespace llassist.ApiService.Executors;

public class QueueOptions
{
    public const string SectionName = "Queue";

    public string QueueName { get; set; } = String.Empty;
    public bool EnableDelayedProcessing { get; set; } = false;
    public bool EnableHeartBeat { get; set; } = false;
    public bool EnableMessageExpiration { get; set; } = false;
    public bool EnableStatus { get; set; } = false;
    public int ConsumerWorkerCount { get; set; } = 1;
    public string HeartBeatUpdateTime { get; set; } = "sec(*%10)";
    public int HeartBeatMonitorTimeInSec { get; set; } = 15;
    public int HeartBeatTimeInSec { get; set; } = 35;
    public bool EnableMessageExpirationTime { get; set; } = false;
    public int MessageExpirationMonitorTimeInSec { get; set; } = 20;

}
