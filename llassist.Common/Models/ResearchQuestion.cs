using System.Text.Json.Serialization;

namespace llassist.Common.Models;

public class ResearchQuestion
{
    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string QuestionText { get; set; } = string.Empty;

    public Ulid ProjectId { get; set; }

    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;

    public ICollection<QuestionDefinition> QuestionDefinitions { get; set; } = [];
}
