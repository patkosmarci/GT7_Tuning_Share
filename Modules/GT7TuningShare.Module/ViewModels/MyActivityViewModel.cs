namespace GT7TuningShare.Module.ViewModels;

public class MyActivityViewModel
{
    public string UserName { get; set; } = string.Empty;

    /// One of "all", "created", "rated", "commented".
    public string ActiveView { get; set; } = "all";

    public List<SetupListItem> Created { get; set; } = new();
    public List<RatedSetupItem> Rated { get; set; } = new();
    public List<MyCommentItem> Commented { get; set; } = new();
}

public class MyCommentItem
{
    public long CommentId { get; set; }
    public string SetupContentItemId { get; set; } = string.Empty;
    public string SetupTitle { get; set; } = string.Empty;
    public string CarDisplayText { get; set; } = string.Empty;
    public int CarGameId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}

public class RatedSetupItem : SetupListItem
{
    public int MyStars { get; set; }
}
