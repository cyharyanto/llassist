namespace llassist.Common.Models;

public class EstimateRelevanceJob : IEntity<Ulid>
{
    public Ulid Id { get; set; } = Ulid.NewUlid();

    public string ModelName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int CompletedArticles { get; set; }

    public int TotalArticles { get; set; }

    public Ulid ProjectId { get; set; }

    public ICollection<Snapshot> Snapshots { get; set; } = [];
}
