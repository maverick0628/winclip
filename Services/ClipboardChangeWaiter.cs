namespace WinClip.Services;

/// <summary>
/// Waits for a simulated copy to land on the clipboard. Uses the clipboard sequence number,
/// so copying a word identical to the current clipboard still counts as a new copy, and a
/// copy that never happened is not mistaken for the stale clipboard text.
/// </summary>
public static class ClipboardChangeWaiter
{
    public static readonly IReadOnlyList<int> DefaultDelaysMs = [8, 15, 30, 50];

    /// <summary>
    /// Polls after each delay. Returns the clipboard text once the sequence number has moved
    /// past <paramref name="sequenceBefore"/> and the text is non-blank; null if the clipboard
    /// never changed or holds no text (files, images) by the last delay.
    /// </summary>
    public static async Task<string?> WaitForNewTextAsync(
        uint sequenceBefore,
        Func<uint> readSequence,
        Func<string?> readText,
        Func<int, Task> delay,
        IReadOnlyList<int>? delaysMs = null)
    {
        foreach (var ms in delaysMs ?? DefaultDelaysMs)
        {
            await delay(ms);
            if (readSequence() == sequenceBefore)
                continue;

            // An app may empty the clipboard and set it in two steps, so keep polling
            // until text appears.
            var text = readText();
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }
        return null;
    }
}
