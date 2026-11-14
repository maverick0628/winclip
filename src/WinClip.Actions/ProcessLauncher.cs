using System.Diagnostics;

namespace WinClip.Actions;

/// <summary>
/// Default <see cref="IProcessLauncher"/> that delegates to <see cref="Process.Start(ProcessStartInfo)"/>.
/// This is the production implementation used at runtime.
/// </summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    public void Start(ProcessStartInfo startInfo) => Process.Start(startInfo);
}
