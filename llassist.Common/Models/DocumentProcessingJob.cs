using System;
using System.Collections.Generic;
using llassist.Common;

namespace llassist.Common.Models;

public class DocumentProcessingJob : IEntity<Ulid>
{
    public Ulid Id { get; set; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required string Base64Content { get; init; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public Dictionary<string, string> Parameters { get; init; } = new();
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
} 