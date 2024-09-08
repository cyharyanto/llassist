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
    public KeySemantics KeySemantics { get; set; } = new KeySemantics();
    public bool MustRead { get; set; }
    public IList<Relevance> Relevances { get; set; } = [];
    [JsonIgnore]
    public virtual Project Project { get; set; } = null!;
    public Ulid ProjectId { get; set; }
}
