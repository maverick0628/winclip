namespace WinClip.Services;

/// <summary>How WinClip should get the selected text for one request.</summary>
public enum CaptureRoute
{
    /// <summary>Use the text UI Automation returned; send no keystrokes.</summary>
    UseUiaText,
    /// <summary>Send Ctrl+C (or Ctrl+X) and read the clipboard.</summary>
    SendKeystroke,
    /// <summary>Do nothing: there is no selection, or a keystroke would be unsafe.</summary>
    Skip,
}

public enum CapturePurpose
{
    /// <summary>The user expects a real copy (gesture copy, Copy button): formatting matters.</summary>
    Copy,
    /// <summary>The app must remove the text, so only Ctrl+X works.</summary>
    Cut,
    /// <summary>WinClip only needs the text (actions, hotkey popup); the clipboard is a side effect.</summary>
    ReadText,
}

/// <summary>
/// Chooses between UI Automation and a simulated keystroke. In a terminal Ctrl+C with no
/// selection interrupts the running program, so terminals never get a keystroke.
/// </summary>
public static class SelectionCapturePolicy
{
    public static CaptureRoute Choose(CapturePurpose purpose, bool isTerminal, bool uiaHasText)
    {
        if (isTerminal)
        {
            // Terminals can't cut, and Ctrl+C without a selection is SIGINT.
            if (purpose == CapturePurpose.Cut) return CaptureRoute.Skip;
            return uiaHasText ? CaptureRoute.UseUiaText : CaptureRoute.Skip;
        }

        return purpose switch
        {
            // Ctrl+C keeps rich formatting (HTML, RTF) that UIA plain text would drop.
            CapturePurpose.Copy or CapturePurpose.Cut => CaptureRoute.SendKeystroke,
            _ => uiaHasText ? CaptureRoute.UseUiaText : CaptureRoute.SendKeystroke,
        };
    }

    /// <summary>
    /// Whether the UIA read is worth its latency: always for terminals and text-only reads,
    /// never for a real copy or cut outside a terminal.
    /// </summary>
    public static bool ShouldReadUia(CapturePurpose purpose, bool isTerminal)
        => isTerminal ? purpose != CapturePurpose.Cut : purpose == CapturePurpose.ReadText;
}
