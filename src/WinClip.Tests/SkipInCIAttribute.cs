using Xunit;

namespace WinClip.Tests;

public sealed class SkipInCIAttribute : FactAttribute
{
    public SkipInCIAttribute()
    {
        if (Environment.GetEnvironmentVariable("CI") == "true")
            Skip = "Skipped in CI: requires system GUI / email client";
    }
}
