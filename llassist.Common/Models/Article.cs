using System.Text.Json.Serialization;

namespace llassist.Common.Models;

public class Article : IEntity<Ulid>
{
    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string Authors { get; set; } = string.Empty;

    public int Year { get; set; }

    public string Title { get; set; } = string.Empty;

    public string DOI { get; set; } = string.Empty;

    public string Link { get; set; } = string.Empty;

    public string Abstract { get; set; } = string.Empty;

    public Ulid ProjectId { get; set; }

    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;

    public bool MustRead { get; set; } = false;

    public ICollection<ArticleKeySemantic> ArticleKeySemantics { get; set; } = [];

    public ICollection<ArticleRelevance> ArticleRelevances { get; set; } = [];
}
