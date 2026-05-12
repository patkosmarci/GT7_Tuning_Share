namespace GT7TuningShare.Module.Services;

public record EngineSwapOption(string EngineName, int OriginalCarGameId);

public interface IEngineSwapCatalog
{
    /// Engines that can be swapped INTO the given car's GameId. Empty list when none.
    IReadOnlyList<EngineSwapOption> GetSwapsFor(int carGameId);

    /// Map of carGameId -> list of engine swaps. For shipping to the client as JSON.
    IReadOnlyDictionary<int, IReadOnlyList<EngineSwapOption>> All { get; }
}

public sealed class EngineSwapCatalog : IEngineSwapCatalog
{
    private readonly Dictionary<int, IReadOnlyList<EngineSwapOption>> _byCar;

    public EngineSwapCatalog()
    {
        _byCar = LoadFromAsset();
    }

    public IReadOnlyDictionary<int, IReadOnlyList<EngineSwapOption>> All => _byCar;

    public IReadOnlyList<EngineSwapOption> GetSwapsFor(int carGameId)
        => _byCar.GetValueOrDefault(carGameId, Array.Empty<EngineSwapOption>());

    private static Dictionary<int, IReadOnlyList<EngineSwapOption>> LoadFromAsset()
    {
        var assembly = typeof(EngineSwapCatalog).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("engineswaps.csv", StringComparison.OrdinalIgnoreCase));

        var result = new Dictionary<int, List<EngineSwapOption>>();
        if (resourceName is null) return result.ToDictionary(p => p.Key, p => (IReadOnlyList<EngineSwapOption>)p.Value);

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null) return result.ToDictionary(p => p.Key, p => (IReadOnlyList<EngineSwapOption>)p.Value);

        using var reader = new StreamReader(stream);
        reader.ReadLine(); // header: NewCar,OriginalCar,EngineName

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var cols = line.Split(',');
            if (cols.Length < 3) continue;
            if (!int.TryParse(cols[0], out var newCar)) continue;
            if (!int.TryParse(cols[1], out var origCar)) continue;
            var engineName = cols[2];

            if (!result.TryGetValue(newCar, out var list))
            {
                list = new List<EngineSwapOption>();
                result[newCar] = list;
            }
            list.Add(new EngineSwapOption(engineName, origCar));
        }

        return result.ToDictionary(p => p.Key, p => (IReadOnlyList<EngineSwapOption>)p.Value);
    }
}
