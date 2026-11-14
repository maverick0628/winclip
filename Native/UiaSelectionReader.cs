using System.Windows.Automation;

namespace WinClip.Native;

internal enum UiaSelectionStatus
{
    /// <summary>No text pattern found, the read failed or it ran past its budget.</summary>
    Unavailable,
    /// <summary>The focused text control reports no selected text.</summary>
    Empty,
    Text,
}

internal readonly record struct UiaSelection(UiaSelectionStatus Status, string? Text)
{
    internal static readonly UiaSelection Unavailable = new(UiaSelectionStatus.Unavailable, null);
    internal static readonly UiaSelection Empty = new(UiaSelectionStatus.Empty, null);
}

/// <summary>
/// Reads the focused control's selection through UI Automation, so WinClip can get the text
/// without sending Ctrl+C. Runs off the UI thread with a time budget, because a cross-process
/// UIA call can block on a busy target app.
/// </summary>
internal static class UiaSelectionReader
{
    internal static readonly TimeSpan DefaultBudget = TimeSpan.FromMilliseconds(300);

    // Text patterns often live on an ancestor of the focused element (a document, a terminal control).
    private const int MaxAncestorDepth = 5;

    // 1 while a read is in flight. A read stuck in a hung app is never cancelled, so later
    // reads give up at once instead of piling up more blocked threads.
    private static int _inFlight;

    internal static bool IsIdle => Volatile.Read(ref _inFlight) == 0;

    internal static Task<UiaSelection> ReadAsync(TimeSpan budget) => ReadAsync(budget, ReadNow);

    internal static async Task<UiaSelection> ReadAsync(TimeSpan budget, Func<UiaSelection> read)
    {
        if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0)
            return UiaSelection.Unavailable;

        var done = new TaskCompletionSource<UiaSelection>(TaskCreationOptions.RunContinuationsAsynchronously);
        // A dedicated thread, so a blocked cross-process call never holds a pool thread
        var thread = new Thread(() =>
        {
            UiaSelection result;
            try { result = read(); }
            catch { result = UiaSelection.Unavailable; }
            Volatile.Write(ref _inFlight, 0);
            done.TrySetResult(result);
        })
        {
            IsBackground = true,
            Name = "WinClip UIA read",
        };
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();

        var finished = await Task.WhenAny(done.Task, Task.Delay(budget)).ConfigureAwait(true);
        return finished == done.Task ? done.Task.Result : UiaSelection.Unavailable;
    }

    private static UiaSelection ReadNow()
    {
        try
        {
            var element = AutomationElement.FocusedElement;
            for (int depth = 0; element != null && depth <= MaxAncestorDepth; depth++)
            {
                if (element.TryGetCurrentPattern(TextPattern.Pattern, out var pattern))
                    return FromPattern((TextPattern)pattern);
                element = TreeWalker.ControlViewWalker.GetParent(element);
                if (element == AutomationElement.RootElement) break;
            }
        }
        catch
        {
            // Element vanished, target app not responding, or access denied
        }
        return UiaSelection.Unavailable;
    }

    private static UiaSelection FromPattern(TextPattern pattern)
    {
        if (pattern.SupportedTextSelection == SupportedTextSelection.None)
            return UiaSelection.Unavailable;

        var ranges = pattern.GetSelection();
        var text = string.Join(Environment.NewLine, ranges.Select(r => r.GetText(-1)));
        return string.IsNullOrWhiteSpace(text)
            ? UiaSelection.Empty
            : new UiaSelection(UiaSelectionStatus.Text, text);
    }
}

/// <summary>
/// Recognises terminal windows, where a stray Ctrl+C interrupts the running program
/// instead of copying.
/// </summary>
internal static class TerminalWindows
{
    private static readonly HashSet<string> TerminalClasses = new(StringComparer.Ordinal)
    {
        "ConsoleWindowClass",              // conhost
        "CASCADIA_HOSTING_WINDOW_CLASS",   // Windows Terminal
        "PseudoConsoleWindow",
        "mintty",                          // Git Bash, Cygwin, MSYS2
        "VirtualConsoleClass",             // ConEmu, Cmder
        "PuTTY",
        "KiTTY",
        "TMobaXtermForm",
    };

    internal static bool IsTerminalClass(string? className)
        => className != null && TerminalClasses.Contains(className);

    internal static bool IsTerminal(IntPtr hwnd) => IsTerminalClass(WindowSnapshot.ClassName(hwnd));
}
