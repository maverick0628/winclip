using WinClip.UI;

namespace WinClip.Tests.UI;

public class SettingsSearchFilterTests
{
    [Theory]
    [InlineData("AI Features Provider LM Studio Model Endpoint", "  studio endpoint ", true)]
    [InlineData("Preferences Start with Windows", "WINDOWS", true)]
    [InlineData("Preferences Sound effects", "sound windows", false)]
    [InlineData("Appearance Popup theme", "", true)]
    public void MatchesAllWordsWithoutDependingOnCase(string text, string query, bool expected)
        => Assert.Equal(expected, SettingsSearchFilter.Matches(text, query));
}
