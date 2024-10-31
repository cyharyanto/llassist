using System.Text.Json.Serialization;

namespace llassist.Common.Models;

public class Snapshot
{
    public const string EntityTypeProjectDefinition = "ProjectDefinition";
    public const string EntityTypeResearchQuestion = "ResearchQuestion";
    public const string EntityTypeQuestionDefinition = "QuestionDefinition";

    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string EntityType { get; set; } = string.Empty;

    public Ulid EntityId { get; set; }

    // JSON representation of the entity (ProjectDefinition, ResearchQuestion, or QuestionDefinition)
    public string SerializedEntity { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Ulid EstimateRelevanceJobId { get; set; }

    [JsonIgnore]
    public EstimateRelevanceJob EstimateRelevanceJob { get; set; } = null!;
}
