using System.Collections.Generic;

public static class RngStateTracker
{
    private static readonly Dictionary<string, RngStream> _streams = new Dictionary<string, RngStream>();

    public static void Reset()
    {
        _streams.Clear();
    }

    public static void RegisterStream(string streamName, RngStream stream)
    {
        _streams[streamName] = stream;
    }

    public static Dictionary<string, int> GetInvocationCounts()
    {
        var counts = new Dictionary<string, int>(_streams.Count);
        foreach (var kvp in _streams)
        {
            counts[kvp.Key] = kvp.Value.InvocationCount;
        }
        return counts;
    }

    public static void RestoreStreams(int masterSeed, Dictionary<string, int> counts)
    {
        _streams.Clear();
        foreach (var kvp in counts)
        {
            int streamHash = masterSeed ^ kvp.Key.GetHashCode();
            var stream = new RngStream(streamHash);
            stream.AdvanceTo(kvp.Value);
            _streams[kvp.Key] = stream;
        }
    }

    public static IRng GetStream(string streamName)
    {
        if (_streams.TryGetValue(streamName, out RngStream stream))
            return stream;
        return null;
    }
}
