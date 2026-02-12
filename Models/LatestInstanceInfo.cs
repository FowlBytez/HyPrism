using System;

namespace HyPrism.Models;

/// <summary>
/// Информация о последней установленной версии игровой instance.
/// </summary>
public sealed class LatestInstanceInfo
{
    public int Version { get; set; }
    public DateTime UpdatedAt { get; set; }
}
