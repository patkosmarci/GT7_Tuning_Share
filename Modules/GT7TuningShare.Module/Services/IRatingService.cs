namespace GT7TuningShare.Module.Services;

public interface IRatingService
{
    Task<int?> GetMyRatingAsync(string userName, string setupContentItemId);
    Task<RatingResult> UpsertAsync(string userName, string setupContentItemId, int stars);
    Task<bool> RemoveAsync(string userName, string setupContentItemId);
}

public record RatingResult(double Average, int Count, int MyRating);
