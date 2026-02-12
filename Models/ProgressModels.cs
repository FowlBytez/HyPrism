namespace HyPrism.Models;

public class DownloadProgress
{
    public bool Success { get; set; }
    public int Progress { get; set; }
    public string? Error { get; set; }
    public bool Cancelled { get; set; }
}

public class ProgressUpdateMessage
{
    public string State { get; set; } = "unknown";
    public double Progress { get; set; }
    public string MessageKey { get; set; } = "common.loading";
    public object[]? Args { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
}
