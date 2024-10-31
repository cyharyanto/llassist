namespace llassist.Common.Models;

public class BackgroundTask
{
    public Ulid TaskId { get; set; } = Ulid.NewUlid();
    public Ulid EstimateRelevanceJobId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public Ulid ProjectId { get; set; }
    public TaskType TaskType { get; set; } = TaskType.PREPROCESSING;
    public Ulid ArticleId { get; set; }
    public IList<ResearchQuestionDTO> ResearchQuestions { get; set; } = [];
}

public enum TaskType
{
    PREPROCESSING,
    EXECUTION,
    FINALIZATION,
}

public class ResearchQuestionDTO
{
    public string Question { get; set; } = string.Empty;
    public IList<string> CombinedDefinitions { get; set; } = [];
}