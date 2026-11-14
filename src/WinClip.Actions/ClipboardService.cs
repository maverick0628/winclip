using System.Windows;
using System.Windows.Threading;

namespace WinClip.Actions;

/// <summary>
/// Production clipboard service that delegates to the WPF clipboard,
/// marshalling to the dispatcher when called from a non-STA thread.
/// </summary>
public class ClipboardService : IClipboardService
{
    /// <inheritdoc />
    public void SetText(string text)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            ClipboardService.Set(text);
            return;
        }

        var dispatcher = Application.Current?.Dispatcher
            ?? throw new InvalidOperationException(
                "Cannot access clipboard: no UI dispatcher available");

        dispatcher.Invoke(() => ClipboardService.Set(text));
    }

    /// <summary>
    /// Robust clipboard set with bounded retry. Windows fires OLE 0x800401D0
    /// ("Cannot open clipboard") when another app holds the lock for a few ms
    /// (clipboard managers, browsers, Office). Retries with brief backoff
    /// resolve nearly all real-world contention.
    /// </summary>
    /// <summary>
    /// Throwing variant of <see cref="TrySet"/>. Drop-in replacement for
    /// <c>Clipboard.SetText</c> in existing try/catch blocks.
    /// </summary>
    public static void Set(string text, int retries = 30)
    {
        if (!TrySet(text, retries))
            throw new System.Runtime.InteropServices.ExternalException(
                "Clipboard is busy (likely PowerToys Advanced Paste or another clipboard manager). Try disabling clipboard hooks and retry.");
    }

    /// <summary>
    /// Tries to write <paramref name="text"/> to the clipboard with bounded retry.
    /// Default budget: 30 retries × 100ms = 3s grace. PowerToys Advanced Paste,
    /// Ditto, ClipboardFusion can each hold the lock for 1–2 seconds at a time;
    /// 3s of grace covers nearly all real-world contention.
    /// </summary>
    /// <returns>true on success, false if all retries failed.</returns>
    public static bool TrySet(string text, int retries = 30)
    {
        // Brief initial delay lets in-flight clipboard ops complete before we contend.
        Thread.Sleep(20);
        for (var i = 0; i < retries; i++)
        {
            try
            {
                // SetDataObject(copy:true) is more resilient than SetText —
                // it persists after app close and handles delayed rendering.
                Clipboard.SetDataObject(text, copy: true);
                return true;
            }
            catch when (i < retries - 1)
            {
                Thread.Sleep(100);
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
}
