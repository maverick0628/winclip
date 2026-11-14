namespace WinClip.Native;

/// <summary>
/// Top-level window facts sampled in the mouse hook, so a click that opened or closed a
/// menu, dropdown or dialog can be told apart from a click on text.
/// </summary>
internal static class WindowSnapshot
{
    internal static HashSet<IntPtr> VisibleTopLevelWindows()
    {
        var windows = new HashSet<IntPtr>();
        try
        {
            NativeMethods.EnumWindows((hwnd, _) =>
            {
                if (NativeMethods.IsWindowVisible(hwnd)) windows.Add(hwnd);
                return true;
            }, IntPtr.Zero);
        }
        catch
        {
            windows.Clear();
        }
        return windows;
    }

    internal static IntPtr RootWindowAt(int x, int y)
    {
        var hwnd = NativeMethods.WindowFromPoint(new NativeMethods.POINT { X = x, Y = y });
        return hwnd == IntPtr.Zero ? IntPtr.Zero : NativeMethods.GetAncestor(hwnd, NativeMethods.GA_ROOT);
    }

    internal static bool IsVisible(IntPtr hwnd) => hwnd != IntPtr.Zero && NativeMethods.IsWindowVisible(hwnd);

    internal static bool BelongsToProcess(IntPtr hwnd, int processId)
    {
        if (hwnd == IntPtr.Zero) return false;
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        return pid == processId;
    }

    /// <summary>Lowercase process name without ".exe", or null.</summary>
    internal static string? ProcessName(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        try
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
            return proc.ProcessName.ToLowerInvariant();
        }
        catch
        {
            return null;
        }
    }

    internal static string? ClassName(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        var buffer = new System.Text.StringBuilder(256);
        return NativeMethods.GetClassName(hwnd, buffer, buffer.Capacity) > 0 ? buffer.ToString() : null;
    }
}
