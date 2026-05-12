using OrchardCore.ContentManagement;

namespace GT7TuningShare.Module.ViewModels;

public class SetupsIndexViewModel
{
    public List<SetupListItem> Setups { get; set; } = new();
    public List<CarOption> AllCars { get; set; } = new();

    public string? CarId { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "recent";

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class SetupListItem
{
    public string ContentItemId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CarDisplayText { get; set; } = string.Empty;
    public int CarGameId { get; set; }
    public string? Author { get; set; }
    public DateTime? CreatedUtc { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int CommentCount { get; set; }
}
