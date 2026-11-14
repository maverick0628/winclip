using System.Diagnostics;
using Microsoft.Win32;

namespace WinClip.Integrations.Blip;

/// <summary>
/// Detects Blip installation and runtime status on Windows systems.
/// Checks registry keys and common installation paths.
/// </summary>
public class BlipDetector : IBlipDetector
{
    private bool? _isInstalledCache;
    private string? _executablePathCache;

    /// <summary>
    /// Checks if Blip is installed on the system.
    /// Only caches positive results — negative results are re-checked each time
    /// so that installing Blip after WinClip is already running is detected.
    /// </summary>
    /// <returns>True if Blip is installed, false otherwise.</returns>
    public bool IsInstalled()
    {
        // Only cache positive results; re-check when previously not found
        if (_isInstalledCache == true)
            return true;

        var executablePath = GetBlipExecutablePath();
        if (!string.IsNullOrEmpty(executablePath))
        {
            _isInstalledCache = true;
            return true;
        }

        // Fallback: if a Blip process is running, it's clearly installed
        if (IsBlipRunning())
        {
            _isInstalledCache = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the path to the Blip executable if installed.
    /// Checks registry keys first, then falls back to common installation paths.
    /// </summary>
    /// <returns>The full path to the Blip executable, or null if not found.</returns>
    public string? GetBlipExecutablePath()
    {
        if (_executablePathCache != null)
        {
            // Re-verify cached path still exists (app may have been uninstalled)
            if (File.Exists(_executablePathCache))
                return _executablePathCache;
            _executablePathCache = null;
        }

        // Check registry keys first
        var registryPath = CheckRegistryForBlipPath();
        if (!string.IsNullOrEmpty(registryPath) && File.Exists(registryPath))
        {
            _executablePathCache = registryPath;
            return _executablePathCache;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Fall back to common installation paths
        // Covers: standard MSI, Electron/NSIS, Squirrel, portable, scoop, and MSIX/Store installs
        var commonPaths = new[]
        {
            // MSIX / Microsoft Store app execution alias (highest priority — most common on Windows 10/11)
            Path.Combine(localAppData, "Microsoft", "WindowsApps", "blip.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Blip", "Blip.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Blip", "Blip.exe"),
            Path.Combine(localAppData, "Blip", "Blip.exe"),
            // Electron / NSIS user-install pattern (e.g., Discord, Slack, VS Code)
            Path.Combine(localAppData, "Programs", "Blip", "Blip.exe"),
            Path.Combine(localAppData, "Programs", "blip", "Blip.exe"),
            // Squirrel-style installs (e.g., Slack, Teams)
            Path.Combine(localAppData, "blip", "Blip.exe"),
            // Scoop install
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "blip", "current", "Blip.exe"),
        };

        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                _executablePathCache = path;
                return _executablePathCache;
            }
        }

        // Last resort: check running Blip processes for their executable path
        try
        {
            var processes = Process.GetProcessesByName("Blip");
            foreach (var proc in processes)
            {
                try
                {
                    var path = proc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        _executablePathCache = path;
                        return _executablePathCache;
                    }
                }
                catch { /* Access denied for protected/MSIX processes */ }
                finally { proc.Dispose(); }
            }
        }
        catch { /* Process enumeration failed */ }

        return null;
    }

    /// <summary>
    /// Checks the Windows registry for Blip installation path.
    /// Checks both HKLM and HKCU registry hives.
    /// </summary>
    /// <returns>The path from registry, or null if not found.</returns>
    private string? CheckRegistryForBlipPath()
    {
        try
        {
            // Check HKEY_LOCAL_MACHINE (system-wide installation)
            var hklmPath = GetRegistryValue(Registry.LocalMachine, @"SOFTWARE\Blip", "InstallPath");
            if (!string.IsNullOrEmpty(hklmPath))
            {
                var executablePath = Path.Combine(hklmPath, "Blip.exe");
                if (File.Exists(executablePath))
                {
                    return executablePath;
                }
            }

            // Check HKEY_CURRENT_USER (user-specific installation)
            var hkcuPath = GetRegistryValue(Registry.CurrentUser, @"SOFTWARE\Blip", "InstallPath");
            if (!string.IsNullOrEmpty(hkcuPath))
            {
                var executablePath = Path.Combine(hkcuPath, "Blip.exe");
                if (File.Exists(executablePath))
                {
                    return executablePath;
                }
            }
        }
        catch
        {
            // Silently handle registry access errors
        }

        return null;
    }

    /// <summary>
    /// Gets a value from the Windows registry.
    /// </summary>
    /// <param name="hive">The registry hive to check.</param>
    /// <param name="keyPath">The path to the registry key.</param>
    /// <param name="valueName">The name of the value to retrieve.</param>
    /// <returns>The registry value as a string, or null if not found.</returns>
    private string? GetRegistryValue(RegistryKey hive, string keyPath, string valueName)
    {
        try
        {
            using var key = hive.OpenSubKey(keyPath);
            if (key != null)
            {
                var value = key.GetValue(valueName);
                return value?.ToString();
            }
        }
        catch
        {
            // Silently handle registry access errors
        }

        return null;
    }

    /// <summary>
    /// Checks if Blip is currently running.
    /// </summary>
    /// <returns>True if Blip process is running, false otherwise.</returns>
    public bool IsBlipRunning()
    {
        Process[]? processes = null;
        try
        {
            processes = Process.GetProcessesByName("Blip");
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            // Process.GetProcessesByName allocates a Process per match; each one
            // holds a kernel handle and must be disposed.
            if (processes != null)
                foreach (var p in processes) p.Dispose();
        }
    }
}
