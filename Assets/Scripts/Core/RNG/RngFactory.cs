public static class RngFactory
{
    public static IRng CreateStream(int masterSeed, string streamName)
    {
        int streamHash = masterSeed ^ streamName.GetHashCode();
        var stream = new RngStream(streamHash);
        RngStateTracker.RegisterStream(streamName, stream);
        return stream;
    }
}
