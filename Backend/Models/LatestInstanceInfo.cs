using System;

namespace HyPrism.Backend;

/// <summary>
/// Информация о последней установленной версии игровой instance.
/// </summary>
public sealed class LatestInstanceInfo
{
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}
