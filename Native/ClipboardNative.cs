using System.Runtime.InteropServices;
using System.Text;

namespace WinClip.Native;

/// <summary>
/// Raw Win32 clipboard calls, used to snapshot and restore clipboard bytes without
/// going through WPF's <c>DataObject</c>, which deserializes some formats.
/// </summary>
internal static class ClipboardNative
{
    internal const uint CF_BITMAP = 2;
    internal const uint CF_METAFILEPICT = 3;
    internal const uint CF_TEXT = 1;
    internal const uint CF_OEMTEXT = 7;
    internal const uint CF_PALETTE = 9;
    internal const uint CF_UNICODETEXT = 13;
    internal const uint CF_ENHMETAFILE = 14;
    internal const uint CF_OWNERDISPLAY = 0x80;
    internal const uint CF_DSPBITMAP = 0x82;
    internal const uint CF_DSPMETAFILEPICT = 0x83;
    internal const uint CF_DSPENHMETAFILE = 0x8E;
    internal const uint CF_PRIVATEFIRST = 0x200;
    internal const uint CF_GDIOBJLAST = 0x3FF;

    internal const uint GMEM_MOVEABLE = 0x0002;
    internal static readonly IntPtr HWND_MESSAGE = new(-3);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    internal static extern int GetClipboardFormatName(uint format, StringBuilder lpszFormatName, int cchMaxCount);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern UIntPtr GlobalSize(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GlobalFree(IntPtr hMem);

    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern uint GetEnhMetaFileBits(IntPtr hemf, uint nSize, byte[]? lpbBuffer);

    [DllImport("gdi32.dll", SetLastError = true)]
    internal static extern IntPtr SetEnhMetaFileBits(uint nSize, byte[] pb);

    [DllImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteEnhMetaFile(IntPtr hemf);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    internal static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string? lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    /// <summary>Registered name of a clipboard format, or a CF_ name for a standard one.</summary>
    internal static string FormatName(uint format)
    {
        var buffer = new StringBuilder(256);
        return GetClipboardFormatName(format, buffer, buffer.Capacity) > 0
            ? buffer.ToString()
            : $"#{format}";
    }

    /// <summary>Opens the clipboard, retrying while another app holds it.</summary>
    internal static bool OpenWithRetry(IntPtr owner, int attempts = 10)
    {
        for (int i = 0; i < attempts; i++)
        {
            if (OpenClipboard(owner)) return true;
            Thread.Sleep(10);
        }
        return false;
    }
}
