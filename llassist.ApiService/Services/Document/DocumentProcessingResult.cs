public record MetadataResult
{
    public required string Title { get; init; }
    public required string[] Authors { get; init; }
    public required int? Year { get; init; }
    public required string Abstract { get; init; }
    public required string[] Keywords { get; init; }
    public string? DOI { get; init; }
}

public record SummaryResult
{
    public required string Overview { get; init; }
    public required string[] KeyPoints { get; init; }
    public required string DetailedSummary { get; init; }
}

public record PageChunk
{
    public required int PageNumber { get; init; }
    public required string ContentSummary { get; init; }
    public required string[] Figures { get; init; }
    public required string[] Tables { get; init; }
    public required string[] Citations { get; init; }
    public string? PreviousPageContext { get; init; }
    public string? NextPageContext { get; init; }
} 