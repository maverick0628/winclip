using System.Runtime.InteropServices;
using WinClip.Models;

namespace WinClip.Native;

/// <summary>
/// Samples the physical modifier keys and the current cursor shape. Called from the
/// low-level mouse hook, so both reads must stay cheap.
/// </summary>
internal static class InputStateReader
{
    private static readonly Lazy<IReadOnlyDictionary<IntPtr, CursorKind>> SystemCursors = new(LoadSystemCursors);

    internal static HeldModifiers ReadModifiers()
    {
        var held = HeldModifiers.None;
        if (IsDown(NativeMethods.VK_SHIFT_ANY)) held |= HeldModifiers.Shift;
        if (IsDown(NativeMethods.VK_CONTROL_ANY)) held |= HeldModifiers.Control;
        if (IsDown(NativeMethods.VK_MENU)) held |= HeldModifiers.Alt;
        if (IsDown(NativeMethods.VK_LWIN) || IsDown(NativeMethods.VK_RWIN)) held |= HeldModifiers.Windows;
        return held;
    }

    internal static CursorKind ReadCursor()
    {
        try
        {
            var info = new NativeMethods.CURSORINFO { cbSize = Marshal.SizeOf<NativeMethods.CURSORINFO>() };
            if (!NativeMethods.GetCursorInfo(ref info))
                return CursorKind.Unknown;
            if ((info.flags & NativeMethods.CURSOR_SHOWING) == 0)
                return CursorKind.Unknown;
            return CursorClassifier.Classify(info.hCursor, SystemCursors.Value);
        }
        catch
        {
            return CursorKind.Unknown;
        }
    }

    private static bool IsDown(int vk) => (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;

    private static IReadOnlyDictionary<IntPtr, CursorKind> LoadSystemCursors()
    {
        var map = new Dictionary<IntPtr, CursorKind>();
        void Add(int id, CursorKind kind)
        {
            var handle = NativeMethods.LoadCursor(IntPtr.Zero, (IntPtr)id);
            if (handle != IntPtr.Zero) map.TryAdd(handle, kind);
        }

        Add(NativeMethods.IDC_IBEAM, CursorKind.IBeam);
        Add(NativeMethods.IDC_ARROW, CursorKind.Arrow);
        Add(NativeMethods.IDC_HAND, CursorKind.Hand);
        foreach (var id in new[]
        {
            NativeMethods.IDC_WAIT, NativeMethods.IDC_CROSS, NativeMethods.IDC_UPARROW,
            NativeMethods.IDC_SIZENWSE, NativeMethods.IDC_SIZENESW, NativeMethods.IDC_SIZEWE,
            NativeMethods.IDC_SIZENS, NativeMethods.IDC_SIZEALL, NativeMethods.IDC_NO,
            NativeMethods.IDC_APPSTARTING,
        })
        {
            Add(id, CursorKind.NonText);
        }
        return map;
    }
}

/// <summary>
/// Maps a cursor handle to a <see cref="CursorKind"/>. System cursors are shared handles,
/// so a handle equal to <c>LoadCursor(NULL, IDC_IBEAM)</c> is the system I-beam in any process.
/// </summary>
internal static class CursorClassifier
{
    internal static CursorKind Classify(IntPtr hCursor, IReadOnlyDictionary<IntPtr, CursorKind> systemCursors)
    {
        if (hCursor == IntPtr.Zero) return CursorKind.Unknown;
        return systemCursors.TryGetValue(hCursor, out var kind) ? kind : CursorKind.Unknown;
    }
}
