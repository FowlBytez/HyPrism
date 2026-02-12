namespace HyPrism.Models;

/// <summary>
/// Represents a detected GPU adapter.
/// </summary>
public class GpuAdapterInfo
{
    /// <summary>Full name of the GPU (e.g., "NVIDIA GeForce RTX 4070")</summary>
    public string Name { get; set; } = "";
    
    /// <summary>Vendor name (e.g., "NVIDIA", "AMD", "Intel")</summary>
    public string Vendor { get; set; } = "";
    
    /// <summary>GPU type: "dedicated" or "integrated"</summary>
    public string Type { get; set; } = "dedicated";
}
