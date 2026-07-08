namespace YBBvideozip.Mac.Ads;

public sealed class AdConfig
{
    public List<AdItem> Ads { get; } = [];
}

public sealed class AdItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "赞助内容";
    public string VideoUrl { get; set; } = "";
    public string CoverUrl { get; set; } = "";
    public string ClickUrl { get; set; } = "";
    public string Platform { get; set; } = "macOS";
    public string AppVersion { get; set; } = "*";
    public string StartAt { get; set; } = "";
    public string EndAt { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public int Weight { get; set; } = 1;
    public int MinPlaySeconds { get; set; } = 15;
}
