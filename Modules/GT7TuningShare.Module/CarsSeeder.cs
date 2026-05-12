using GT7TuningShare.Module.Models;
using Microsoft.Extensions.Logging;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Records;
using OrchardCore.Modules;
using OrchardCore.Title.Models;
using YesSql;

namespace GT7TuningShare.Module;

/// Seeds the Cars catalog after the GT7 feature's migration has committed.
/// Idempotent — checks for existing Car items and skips if already seeded.
public sealed class CarsSeeder : ModularTenantEvents
{
    private readonly IContentManager _contentManager;
    private readonly ISession _session;
    private readonly ILogger<CarsSeeder> _logger;

    public CarsSeeder(IContentManager contentManager, ISession session, ILogger<CarsSeeder> logger)
    {
        _contentManager = contentManager;
        _session = session;
        _logger = logger;
    }

    public override async Task ActivatedAsync()
    {
        try
        {
            var anyCar = await _session.Query<ContentItem, ContentItemIndex>(
                x => x.ContentType == "Car" && x.Latest).FirstOrDefaultAsync();

            if (anyCar is not null) return;

            var makers = new Dictionary<int, string>();
            await using (var stream = OpenAsset("maker.csv"))
            {
                if (stream is null) return;
                using var reader = new StreamReader(stream);
                await reader.ReadLineAsync();
                string? line;
                while ((line = await reader.ReadLineAsync()) is not null)
                {
                    var cols = line.Split(',');
                    if (cols.Length < 2) continue;
                    if (int.TryParse(cols[0], out var id))
                    {
                        makers[id] = cols[1];
                    }
                }
            }

            await using var carsStream = OpenAsset("cars.csv");
            if (carsStream is null) return;
            using var carsReader = new StreamReader(carsStream);
            await carsReader.ReadLineAsync();

            var created = 0;
            string? carLine;
            while ((carLine = await carsReader.ReadLineAsync()) is not null)
            {
                var cols = carLine.Split(',');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[0], out var gameId)) continue;
                var shortName = cols[1];
                if (!int.TryParse(cols[2], out var makerId)) continue;

                var make = makers.GetValueOrDefault(makerId, "Unknown");
                var displayText = $"{make} {shortName}";

                var car = await _contentManager.NewAsync("Car");
                car.DisplayText = displayText;
                car.Alter<TitlePart>(p => p.Title = displayText);
                car.Alter<CarPart>(p =>
                {
                    p.GameId = gameId;
                    p.Make = make;
                    p.ShortName = shortName;
                });

                await _contentManager.CreateAsync(car, VersionOptions.Published);
                created++;

                // Flush in batches so the YesSql working set stays small for ~575 inserts.
                if (created % 100 == 0)
                {
                    await _session.SaveChangesAsync();
                }
            }

            await _session.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GT7 CarsSeeder failed during seed");
            throw;
        }
    }

    private static Stream? OpenAsset(string fileName)
    {
        var assembly = typeof(CarsSeeder).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        return resourceName is null ? null : assembly.GetManifestResourceStream(resourceName);
    }
}
