using System.Text.Json.Serialization;

namespace llassist.Common.Models;

public class ArticleRelevance
{
    public Ulid ArticleId { get; set; }

    public Ulid EstimateRelevanceJobId { get; set; }

    public int RelevanceIndex { get; set; }

    public string Question { get; set; } = string.Empty;

    public double RelevanceScore { get; set; } = 0.9;

    public double ContributionScore { get; set; } = 0.9;

    public bool IsRelevant { get; set; } = false;

    public bool IsContributing { get; set; } = false;

    public string RelevanceReason { get; set; } = string.Empty;

    public string ContributionReason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public virtual Article Article { get; set; } = null!;

}
