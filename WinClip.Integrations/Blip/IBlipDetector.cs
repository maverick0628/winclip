namespace WinClip.Integrations.Blip;

/// <summary>
/// Interface for detecting Blip installation and runtime status.
/// </summary>
public interface IBlipDetector
{
    /// <summary>
    /// Checks if Blip is installed on the system.
    /// </summary>
    /// <returns>True if Blip is installed, false otherwise.</returns>
    bool IsInstalled();

    /// <summary>
    /// Gets the path to the Blip executable if installed.
    /// </summary>
    /// <returns>The full path to the Blip executable, or null if not found.</returns>
    string? GetBlipExecutablePath();

    /// <summary>
    /// Checks if Blip is currently running.
    /// </summary>
    /// <returns>True if Blip process is running, false otherwise.</returns>
    bool IsBlipRunning();
}
