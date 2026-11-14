using System.Windows;
using WinClip.Native;

namespace WinClip.UI;

internal readonly record struct MonitorGeometry(double ScaleX, double ScaleY, Rect WorkArea)
{
    internal static MonitorGeometry FromPoint(int x, int y)
    {
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(x, y)).WorkingArea;
        double scaleX = 1, scaleY = 1;
        var monitor = NativeMethods.MonitorFromPoint(new NativeMethods.POINT { X = x, Y = y },
            NativeMethods.MONITOR_DEFAULTTONEAREST);
        if (monitor != IntPtr.Zero && NativeMethods.GetDpiForMonitor(monitor, 0, out uint dpiX, out uint dpiY) == 0
            && dpiX > 0 && dpiY > 0)
        {
            scaleX = 96.0 / dpiX;
            scaleY = 96.0 / dpiY;
        }
        return FromPixels(new Rect(screen.Left, screen.Top, screen.Width, screen.Height), scaleX, scaleY);
    }

    internal static MonitorGeometry FromPixels(Rect workArea, double scaleX, double scaleY) => new(
        scaleX, scaleY, new Rect(workArea.X * scaleX, workArea.Y * scaleY,
            workArea.Width * scaleX, workArea.Height * scaleY));

    internal (double Left, double Top) Center(double width, double height) =>
        (WorkArea.Left + (WorkArea.Width - width) / 2, WorkArea.Top + (WorkArea.Height - height) / 2);
}
