using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WinClip.Actions;

namespace WinClip.Core.Services;

/// <summary>
/// Captures context information about the active window and application.
/// </summary>
public class ContextCaptureService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

    /// <summary>
    /// Captures the current action context from the active window.
    /// </summary>
    /// <returns>ActionContext with source app and URL if available.</returns>
    public ActionContext CaptureContext()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return new ActionContext();
            }

            // Get window title
            var titleBuilder = new StringBuilder(256);
            GetWindowText(hwnd, titleBuilder, 256);
            var windowTitle = titleBuilder.ToString();

            // Get process name (dispose the Process handle so we don't leak
            // a kernel handle on every context capture — this method runs
            // frequently during active use).
            GetWindowThreadProcessId(hwnd, out int processId);
            string processName;
            using (var process = Process.GetProcessById(processId))
            {
                processName = process.ProcessName;
            }

            // Extract URL from browser window titles
            string? url = ExtractUrlFromTitle(windowTitle, processName);

            return new ActionContext
            {
                SourceApp = processName,
                SourceUrl = url
            };
        }
        catch
        {
            return new ActionContext();
        }
    }

    private string? ExtractUrlFromTitle(string windowTitle, string processName)
    {
        // Common browser process names
        var browsers = new[] { "chrome", "firefox", "msedge", "brave", "opera" };

        if (!browsers.Contains(processName.ToLowerInvariant()))
        {
            return null;
        }

        // Try to extract URL from window title
        // Many browsers show "Title - URL" or just "URL"
        // This is a simple heuristic - may need refinement
        if (windowTitle.Contains("http://") || windowTitle.Contains("https://"))
        {
            var urlStart = windowTitle.LastIndexOf("http");
            if (urlStart >= 0)
            {
                var url = windowTitle[urlStart..];
                // Clean up common suffixes like browser name
                var separators = new[] { " - ", " — ", " | " };
                foreach (var sep in separators)
                {
                    var sepIndex = url.IndexOf(sep);
                    if (sepIndex > 0)
                    {
                        url = url[..sepIndex];
                        break;
                    }
                }
                return url.Trim();
            }
        }

        return null;
    }
}
