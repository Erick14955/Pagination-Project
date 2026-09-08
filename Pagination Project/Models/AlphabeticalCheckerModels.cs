namespace Pagination_Project.Models;

public sealed class PsBookAnalysisResult
{
    public int FilesAnalyzed { get; init; }
    public int PagesAnalyzed { get; init; }
    public int ListingsAnalyzed { get; init; }
    public List<PsListingOrderError> Errors { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public bool IsCorrect => Errors.Count == 0;
}

public sealed class PsListingOrderError
{
    public required string ListingName { get; init; }
    public required string CurrentFile { get; init; }
    public required string CurrentPage { get; init; }
    public int CurrentPositionInPage { get; init; }

    public required string RecommendedFile { get; init; }
    public required string RecommendedPage { get; init; }
    public int RecommendedPositionInPage { get; init; }

    public string? ShouldGoAfter { get; init; }
    public string? ShouldGoBefore { get; init; }
}

internal sealed class PsListing
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required string SortKey { get; init; }
    public required string FileName { get; init; }
    public int FileOrder { get; init; }
    public int PageOrderInFile { get; init; }
    public int GlobalPageOrder { get; init; }
    public required string PageLabel { get; init; }
    public int PositionInPage { get; init; }
    public int GlobalPosition { get; init; }
}
