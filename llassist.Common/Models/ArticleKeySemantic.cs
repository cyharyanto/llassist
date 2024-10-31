using System.Text.Json.Serialization;

namespace llassist.Common.Models;

public class ArticleKeySemantic
{
    public const string TypeTopic = "topic";
    public const string TypeEntity = "entity";
    public const string TypeKeyword = "keyword";

    public Ulid ArticleId { get; set; }

    public int KeySemanticIndex { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    [JsonIgnore]
    public virtual Article Article { get; set; } = null!;
}
