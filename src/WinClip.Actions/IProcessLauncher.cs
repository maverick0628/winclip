using System.Diagnostics;

namespace WinClip.Actions;

/// <summary>
/// Abstraction over starting an OS process / shell-executing a URI.
/// Exists so actions that hand a URL or protocol (e.g. mailto:) to the shell
/// can be unit-tested without launching a real process, which is impossible on
/// headless CI runners that have no GUI or registered protocol handler.
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Starts the process described by <paramref name="startInfo"/>.
    /// </summary>
    /// <param name="startInfo">The process/shell-execute parameters.</param>
    void Start(ProcessStartInfo startInfo);
}
