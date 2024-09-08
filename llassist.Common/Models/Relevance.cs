namespace llassist.Common.Models;

public class Relevance
{
    public string Question { get; set; } = string.Empty;
    public double RelevanceScore { get; set; } = 0.9;
    public double ContributionScore { get; set; } = 0.9;
    public bool IsRelevant { get; set; } = false;
    public bool IsContributing { get; set; } = false;
    public string RelevanceReason { get; set; } = string.Empty;
    public string ContributionReason { get; set; } = string.Empty;
}
