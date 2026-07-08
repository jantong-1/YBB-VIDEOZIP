namespace YBBvideozip.Mac.Ads;

public sealed class AdGateState
{
    private bool minimumReachedRaised;

    private AdGateState(DateTime startedAtUtc, int minPlaySeconds)
    {
        StartedAtUtc = startedAtUtc;
        MinPlaySeconds = Math.Max(1, minPlaySeconds);
    }

    public DateTime StartedAtUtc { get; }

    public int MinPlaySeconds { get; }

    public static AdGateState Start(AdItem ad, DateTime startedAtUtc)
    {
        return new AdGateState(startedAtUtc, ad.MinPlaySeconds);
    }

    public bool CanClose(DateTime nowUtc)
    {
        return (nowUtc - StartedAtUtc).TotalSeconds >= MinPlaySeconds;
    }

    public string CountdownText(DateTime nowUtc)
    {
        var remaining = Math.Max(0, MinPlaySeconds - (int)Math.Floor((nowUtc - StartedAtUtc).TotalSeconds));
        return remaining > 0 ? remaining + " 秒后可关闭" : "可以关闭广告";
    }

    public bool TakeMinimumReached(DateTime nowUtc)
    {
        if (minimumReachedRaised || !CanClose(nowUtc))
        {
            return false;
        }

        minimumReachedRaised = true;
        return true;
    }
}
