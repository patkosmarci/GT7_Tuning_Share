using OrchardCore.ContentManagement;

namespace GT7TuningShare.Module.Models;

public class RatingPart : ContentPart
{
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
}
