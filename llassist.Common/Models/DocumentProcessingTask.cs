using llassist.Common;

namespace llassist.Common.Models;

public class DocumentProcessingTask : IEntity<Ulid>
{
    public Ulid Id { get; set; }
    public required Ulid JobId { get; init; }
    public required DocumentTaskType Type { get; init; }
    public Dictionary<string, object> Parameters { get; init; } = new();
}

public enum DocumentTaskType
{
    METADATA,
    SUMMARY,
    CHUNKING,
    FINALIZATION
} 