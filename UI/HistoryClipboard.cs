namespace WinClip.UI;

internal static class HistoryClipboard
{
    internal static bool TryCopy(string text) => TryCopy(text, value => { System.Windows.Clipboard.SetText(value); return true; });

    internal static bool TryCopy(string text, Func<string, bool> write)
    {
        try { return write(text); }
        catch { return false; }
    }
}
