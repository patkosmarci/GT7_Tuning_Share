using GT7TuningShare.Module.Indexes;
using GT7TuningShare.Module.Models;
using OrchardCore.ContentManagement;
using YesSql;

namespace GT7TuningShare.Module.Services;

public sealed class RatingService : IRatingService
{
    private readonly ISession _session;
    private readonly IContentManager _contentManager;

    public RatingService(ISession session, IContentManager contentManager)
    {
        _session = session;
        _contentManager = contentManager;
    }

    public async Task<int?> GetMyRatingAsync(string userName, string setupContentItemId)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(setupContentItemId)) return null;

        var existing = await _session.Query<RatingRecord, RatingIndex>(
            x => x.UserId == userName && x.SetupContentItemId == setupContentItemId)
            .FirstOrDefaultAsync();

        return existing?.Stars;
    }

    public async Task<RatingResult> UpsertAsync(string userName, string setupContentItemId, int stars)
    {
        if (stars < 1 || stars > 5) throw new ArgumentOutOfRangeException(nameof(stars), "Stars must be between 1 and 5.");
        if (string.IsNullOrEmpty(userName)) throw new ArgumentException("User name is required.", nameof(userName));
        if (string.IsNullOrEmpty(setupContentItemId)) throw new ArgumentException("Setup id is required.", nameof(setupContentItemId));

        var existing = await _session.Query<RatingRecord, RatingIndex>(
            x => x.UserId == userName && x.SetupContentItemId == setupContentItemId)
            .FirstOrDefaultAsync();

        if (existing is not null)
        {
            existing.Stars = stars;
            existing.CreatedUtc = DateTime.UtcNow;
            await _session.SaveAsync(existing);
        }
        else
        {
            await _session.SaveAsync(new RatingRecord
            {
                UserId = userName,
                SetupContentItemId = setupContentItemId,
                Stars = stars,
                CreatedUtc = DateTime.UtcNow,
            });
        }

        await _session.SaveChangesAsync();

        var allRatings = (await _session.Query<RatingRecord, RatingIndex>(
            x => x.SetupContentItemId == setupContentItemId).ListAsync()).ToList();

        var count = allRatings.Count;
        var avg = count > 0 ? allRatings.Average(r => (double)r.Stars) : 0d;

        var setup = await _contentManager.GetAsync(setupContentItemId, VersionOptions.Latest);
        if (setup is not null)
        {
            setup.Alter<RatingPart>(p =>
            {
                p.AverageRating = avg;
                p.RatingCount = count;
            });

            await _contentManager.UpdateAsync(setup);
            await _contentManager.PublishAsync(setup);
        }

        return new RatingResult(avg, count, stars);
    }

    public async Task<bool> RemoveAsync(string userName, string setupContentItemId)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(setupContentItemId)) return false;

        var existing = await _session.Query<RatingRecord, RatingIndex>(
            x => x.UserId == userName && x.SetupContentItemId == setupContentItemId)
            .FirstOrDefaultAsync();

        if (existing is null) return false;

        _session.Delete(existing);
        await _session.SaveChangesAsync();

        // Recompute aggregate after removal.
        var allRatings = (await _session.Query<RatingRecord, RatingIndex>(
            x => x.SetupContentItemId == setupContentItemId).ListAsync()).ToList();

        var count = allRatings.Count;
        var avg = count > 0 ? allRatings.Average(r => (double)r.Stars) : 0d;

        var setup = await _contentManager.GetAsync(setupContentItemId, VersionOptions.Latest);
        if (setup is not null)
        {
            setup.Alter<RatingPart>(p =>
            {
                p.AverageRating = avg;
                p.RatingCount = count;
            });

            await _contentManager.UpdateAsync(setup);
            await _contentManager.PublishAsync(setup);
        }

        return true;
    }
}
