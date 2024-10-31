using System.Text.Json.Serialization;

namespace llassist.Common.Models;

public class QuestionDefinition
{
    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string Definition { get; set; } = string.Empty;

    public Ulid ResearchQuestionId { get; set; }

    [JsonIgnore]
    public virtual ResearchQuestion ResearchQuestion { get; set; } = null!;
}
