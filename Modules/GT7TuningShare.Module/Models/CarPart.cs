using OrchardCore.ContentManagement;

namespace GT7TuningShare.Module.Models;

public class CarPart : ContentPart
{
    public int GameId { get; set; }
    public string Make { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Drivetrain { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}
