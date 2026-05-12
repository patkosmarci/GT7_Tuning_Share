namespace GT7TuningShare.Module.Models;

public class RatingRecord
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string SetupContentItemId { get; set; } = string.Empty;
    public int Stars { get; set; }
    public DateTime CreatedUtc { get; set; }
}
