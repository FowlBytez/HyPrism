namespace HyPrism.Services.Core.Ipc;

/// <summary>
/// Base interface for all IPC handlers.
/// Each handler is responsible for registering its own IPC channels.
/// </summary>
public interface IIpcHandler
{
    /// <summary>
    /// Registers all IPC channels handled by this handler.
    /// </summary>
    void Register();
}
