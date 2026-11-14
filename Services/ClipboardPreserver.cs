using System.Diagnostics;
using System.Runtime.InteropServices;
using WinClip.Native;

namespace WinClip.Services;

/// <summary>
/// Saves and restores the whole clipboard around a Ctrl+C that WinClip sends only to read
/// the selection, so the user's clipboard looks untouched afterwards. Works on raw Win32
/// clipboard bytes: WPF's <c>DataObject</c> would run its deserializer on custom formats
/// that a web page or remote session can write.
/// </summary>
public static class ClipboardPreserver
{
    /// <summary>
    /// Formats that password managers and clipboard tools set to say "don't keep this".
    /// WinClip never holds such content, so a later restore can't defeat their auto-clear.
    /// </summary>
    internal static readonly string[] ConcealedMarkers =
    [
        "ExcludeClipboardContentFromMonitorProcessing",
        "Clipboard Viewer Ignore",
    ];

    // Windows clipboard history and cloud sync skip items carrying these as DWORD 0.
    private const string CanIncludeInHistory = "CanIncludeInClipboardHistory";
    private const string CanUploadToCloud = "CanUploadToCloudClipboard";

    // OLE's own bookkeeping: a handle to the source's clipboard window and the source's
    // original format list. Restored, they make OLE readers see the stale source object and
    // formats the restore never wrote. Without them OLE reads the plain formats.
    internal static readonly string[] OleBookkeepingFormats = ["DataObject", "Ole Private Data", "OleClipboardPersistOnFlush"];

    // Virtual files: the descriptor is useless without its contents, which aren't HGLOBAL.
    private static readonly string[] VirtualFileFormats = ["FileGroupDescriptorW", "FileGroupDescriptor"];
    private const string VirtualFileContents = "FileContents";

    // A big Excel or image copy can take seconds to render; past these, WinClip gives up
    // on restoring and leaves the transient copy, as it did before restores existed. The time
    // budget is checked between formats, so one slow delayed render can still overrun it.
    internal static readonly TimeSpan SaveBudget = TimeSpan.FromSeconds(1);
    internal const long MaxSnapshotBytes = 64L * 1024 * 1024;

    [ThreadStatic] private static IntPtr _ownerWindow;

    internal static bool IsConcealed(IEnumerable<string> formats)
        => formats.Any(f => ConcealedMarkers.Contains(f, StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// <see cref="Save"/> on a dedicated thread, so a slow clipboard owner never blocks the
    /// UI thread, which also runs the mouse hook.
    /// </summary>
    public static Task<ClipboardSnapshot?> SaveAsync()
    {
        var done = new TaskCompletionSource<ClipboardSnapshot?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try { done.TrySetResult(Save()); }
            catch { done.TrySetResult(null); }
        })
        {
            IsBackground = true,
            Name = "WinClip clipboard snapshot",
        };
        thread.Start();
        return done.Task;
    }

    /// <summary>
    /// Captures every format that can be copied as bytes. Returns null if the clipboard is
    /// concealed, locked, too big or too slow, or nothing on it could be read; an empty
    /// snapshot means the clipboard was empty.
    /// </summary>
    internal static ClipboardSnapshot? Save()
    {
        if (!ClipboardNative.OpenWithRetry(IntPtr.Zero))
            return null;
        try
        {
            var formats = new List<uint>();
            for (uint f = ClipboardNative.EnumClipboardFormats(0); f != 0; f = ClipboardNative.EnumClipboardFormats(f))
                formats.Add(f);
            if (formats.Count == 0)
                return new ClipboardSnapshot([]);

            var names = formats.ToDictionary(f => f, ClipboardNative.FormatName);
            if (IsConcealed(names.Values))
                return null;

            bool hasUnicodeText = formats.Contains(ClipboardNative.CF_UNICODETEXT);
            var watch = Stopwatch.StartNew();
            long total = 0;
            var items = new List<ClipboardFormatData>();
            var unreadable = new List<string>();

            foreach (var format in formats)
            {
                if (watch.Elapsed > SaveBudget)
                    return null;
                // Windows synthesizes these from formats we do keep (DIB, EMF, Unicode text),
                // and the GDI ones hold handles, not bytes
                if (IsHandleOrSynthesized(format, hasUnicodeText)
                    || OleBookkeepingFormats.Contains(names[format], StringComparer.OrdinalIgnoreCase))
                    continue;

                var data = Read(format);
                if (data == null)
                {
                    unreadable.Add(names[format]);
                    continue;
                }
                total += data.Length;
                if (total > MaxSnapshotBytes)
                    return null;
                items.Add(new ClipboardFormatData(format, names[format], data));
            }

            if (unreadable.Contains(VirtualFileContents, StringComparer.OrdinalIgnoreCase))
                items.RemoveAll(i => VirtualFileFormats.Contains(i.Name, StringComparer.OrdinalIgnoreCase));

            return items.Count == 0 ? null : new ClipboardSnapshot(items);
        }
        finally
        {
            ClipboardNative.CloseClipboard();
        }
    }

    /// <summary>
    /// Puts the snapshot back, marked so Windows clipboard history and cloud sync don't record
    /// it a second time. Returns false if the clipboard could not be written.
    /// </summary>
    public static bool Restore(ClipboardSnapshot snapshot)
    {
        // Without an owner, EmptyClipboard would succeed and every SetClipboardData fail
        var owner = OwnerWindow();
        if (owner == IntPtr.Zero || !ClipboardNative.OpenWithRetry(owner))
            return false;
        try
        {
            if (!ClipboardNative.EmptyClipboard())
                return false;
            if (snapshot.Items.Count == 0)
                return true;

            bool any = false;
            foreach (var item in snapshot.Items)
                any |= Write(item.Format, item.Data);

            // Not a concealed marker: the next transient copy must still be able to save this.
            var zero = BitConverter.GetBytes(0);
            Write(ClipboardNative.RegisterClipboardFormat(CanIncludeInHistory), zero);
            Write(ClipboardNative.RegisterClipboardFormat(CanUploadToCloud), zero);
            return any;
        }
        finally
        {
            ClipboardNative.CloseClipboard();
        }
    }

    /// <summary>
    /// Restore only when the transient copy changed the clipboard and nothing has changed it
    /// since the text was read: a later change is the user's or the app's, and wins.
    /// </summary>
    internal static bool ShouldRestore(uint sequenceBefore, uint sequenceAtRead, uint sequenceNow)
        => sequenceAtRead != sequenceBefore && sequenceNow == sequenceAtRead;

    internal static bool IsHandleOrSynthesized(uint format, bool hasUnicodeText) => format switch
    {
        ClipboardNative.CF_BITMAP or ClipboardNative.CF_METAFILEPICT or ClipboardNative.CF_PALETTE
            or ClipboardNative.CF_OWNERDISPLAY or ClipboardNative.CF_DSPBITMAP
            or ClipboardNative.CF_DSPMETAFILEPICT or ClipboardNative.CF_DSPENHMETAFILE => true,
        ClipboardNative.CF_TEXT or ClipboardNative.CF_OEMTEXT => hasUnicodeText,
        >= ClipboardNative.CF_PRIVATEFIRST and <= ClipboardNative.CF_GDIOBJLAST => true,
        _ => false,
    };

    private static byte[]? Read(uint format)
    {
        var handle = ClipboardNative.GetClipboardData(format);
        if (handle == IntPtr.Zero)
            return null;

        if (format == ClipboardNative.CF_ENHMETAFILE)
        {
            uint size = ClipboardNative.GetEnhMetaFileBits(handle, 0, null);
            if (size == 0 || size > MaxSnapshotBytes) return null;
            var bits = new byte[size];
            return ClipboardNative.GetEnhMetaFileBits(handle, size, bits) == size ? bits : null;
        }

        long length = (long)ClipboardNative.GlobalSize(handle).ToUInt64();
        if (length <= 0 || length > MaxSnapshotBytes)
            return null;
        var pointer = ClipboardNative.GlobalLock(handle);
        if (pointer == IntPtr.Zero)
            return null;
        try
        {
            var bytes = new byte[length];
            Marshal.Copy(pointer, bytes, 0, (int)length);
            return bytes;
        }
        finally
        {
            ClipboardNative.GlobalUnlock(handle);
        }
    }

    private static bool Write(uint format, byte[] data)
    {
        if (format == 0) return false;

        if (format == ClipboardNative.CF_ENHMETAFILE)
        {
            var emf = ClipboardNative.SetEnhMetaFileBits((uint)data.Length, data);
            if (emf == IntPtr.Zero) return false;
            if (ClipboardNative.SetClipboardData(format, emf) != IntPtr.Zero) return true;
            ClipboardNative.DeleteEnhMetaFile(emf);
            return false;
        }

        var memory = ClipboardNative.GlobalAlloc(ClipboardNative.GMEM_MOVEABLE, (UIntPtr)(ulong)data.Length);
        if (memory == IntPtr.Zero) return false;
        var pointer = ClipboardNative.GlobalLock(memory);
        if (pointer == IntPtr.Zero)
        {
            ClipboardNative.GlobalFree(memory);
            return false;
        }
        Marshal.Copy(data, 0, pointer, data.Length);
        ClipboardNative.GlobalUnlock(memory);

        // On success the system owns the memory
        if (ClipboardNative.SetClipboardData(format, memory) != IntPtr.Zero) return true;
        ClipboardNative.GlobalFree(memory);
        return false;
    }

    // EmptyClipboard with no owner window makes SetClipboardData fail, so restores need a
    // window. A message-only window on the calling thread, which must pump messages (the
    // UI thread does) so the next app's EmptyClipboard isn't kept waiting.
    private static IntPtr OwnerWindow()
    {
        if (_ownerWindow == IntPtr.Zero)
        {
            _ownerWindow = ClipboardNative.CreateWindowEx(0, "Message", null, 0, 0, 0, 0, 0,
                ClipboardNative.HWND_MESSAGE, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }
        return _ownerWindow;
    }
}

/// <summary>One clipboard format's raw bytes.</summary>
public sealed record ClipboardFormatData(uint Format, string Name, byte[] Data);

/// <summary>
/// Immutable snapshot of every clipboard format at a point in time.
/// </summary>
public sealed class ClipboardSnapshot
{
    public IReadOnlyList<ClipboardFormatData> Items { get; }

    public ClipboardSnapshot(IReadOnlyList<ClipboardFormatData> items)
    {
        Items = items;
    }
}
