using System.Text.Json.Serialization;

namespace llassist.Common.Models;

public class ProjectDefinition
{
    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string Definition { get; set; } = string.Empty;

    public Ulid ProjectId { get; set; }

    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;
}
