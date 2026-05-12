namespace GT7TuningShare.Module.Models;

public class CommentRecord
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SetupContentItemId { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
