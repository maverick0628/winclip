namespace WinClip.UI;

internal static class SettingsSearchFilter
{
    internal static bool Matches(string text, string query) =>
        query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .All(word => text.Contains(word, StringComparison.OrdinalIgnoreCase));
}
