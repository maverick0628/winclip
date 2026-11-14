using WinClip.Services;

namespace WinClip.Tests.Services;

public class SelectionPolicyTests
{
    private static PolicyEffects Eval(SelectionPolicy policy, string? process, string? windowClass = null, SelectionGesture? gesture = null)
        => policy.Evaluate(new PolicyContext(process, windowClass, gesture));

    [Fact]
    public void ShippedPolicy_ParsesWithRules()
    {
        var policy = SelectionPolicy.Parse(SelectionPolicy.ReadShippedJson());
        Assert.True(policy.RuleCount > 0);
        Assert.Equal(policy.RuleCount, SelectionPolicy.Default.RuleCount);
    }

    [Theory]
    [InlineData("mstsc", PolicyEffects.Deny)]
    [InlineData("photoshop", PolicyEffects.Deny)]
    [InlineData("gimp-2.10", PolicyEffects.Deny)]
    [InlineData("alacritty", PolicyEffects.Terminal)]
    [InlineData("notepad", PolicyEffects.None)]
    public void ShippedPolicy_ClassifiesKnownApps(string process, PolicyEffects expected)
    {
        Assert.Equal(expected, Eval(SelectionPolicy.Default, process));
    }

    [Fact]
    public void ShippedPolicy_LeavesBrowsersAlone()
    {
        // Tab and scrollbar drags show the arrow too, so browsers get no arrow-drag allowance
        Assert.Equal(PolicyEffects.None, Eval(SelectionPolicy.Default, "chrome", gesture: SelectionGesture.DragSelect));
    }

    [Fact]
    public void GestureCondition_LimitsTheRule()
    {
        var policy = SelectionPolicy.Parse("""{ "rules": [ { "process": ["app"], "gesture": ["DragSelect"], "effect": "allowArrowDrag" } ] }""");
        Assert.Equal(PolicyEffects.AllowArrowDrag, Eval(policy, "app", gesture: SelectionGesture.DragSelect));
        Assert.Equal(PolicyEffects.None, Eval(policy, "app", gesture: SelectionGesture.DoubleClick));
        Assert.Equal(PolicyEffects.None, Eval(policy, "app"));
    }

    [Fact]
    public void Glob_MatchesCaseInsensitively_AndAnchors()
    {
        var policy = SelectionPolicy.Parse("""{ "rules": [ { "process": ["foo*"], "effect": "deny" } ] }""");
        Assert.Equal(PolicyEffects.Deny, Eval(policy, "FooBar"));
        Assert.Equal(PolicyEffects.None, Eval(policy, "barfoo"));
        Assert.Equal(PolicyEffects.None, Eval(policy, null));
    }

    [Fact]
    public void Glob_TreatsRegexCharactersLiterally()
    {
        var policy = SelectionPolicy.Parse("""{ "rules": [ { "process": ["a.b"], "effect": "deny" } ] }""");
        Assert.Equal(PolicyEffects.Deny, Eval(policy, "a.b"));
        Assert.Equal(PolicyEffects.None, Eval(policy, "axb"));
    }

    [Fact]
    public void Groups_Expand_AndConditionsCombine()
    {
        var policy = SelectionPolicy.Parse("""
            {
              "groups": { "eds": ["code", "notepad++"] },
              "rules": [
                { "process": [":eds:"], "windowClass": ["Chrome_*"], "effect": "terminal" },
                { "process": ["code"], "effect": "allowArrowDrag" }
              ]
            }
            """);
        Assert.Equal(PolicyEffects.Terminal | PolicyEffects.AllowArrowDrag, Eval(policy, "code", "Chrome_WidgetWin_1"));
        Assert.Equal(PolicyEffects.AllowArrowDrag, Eval(policy, "code", "Other"));
        Assert.Equal(PolicyEffects.Terminal, Eval(policy, "notepad++", "Chrome_X"));
    }

    [Theory]
    [InlineData("""{ "rules": [ { "process": [":missing:"], "effect": "deny" } ] }""")]
    [InlineData("""{ "rules": [ { "process": ["x"], "effect": "explode" } ] }""")]
    [InlineData("""{ "rules": [ { "process": ["x"], "effect": "none" } ] }""")]
    [InlineData("""{ "rules": [ { "process": ["x"], "gesture": ["Swipe"], "effect": "deny" } ] }""")]
    [InlineData("""{ "rules": [ { "process": ["x"], "effect": "8" } ] }""")]
    [InlineData("""{ "rules": [ { "processes": ["x"], "effect": "deny" } ] }""")]
    [InlineData("""{ "rules": [ { "effect": "deny" } ] }""")]
    [InlineData("""{ "groups": { "g": [] }, "rules": [ { "process": [":g:"], "effect": "deny" } ] }""")]
    [InlineData("""{ "rules": [ """)]
    public void InvalidPolicy_Throws(string json)
    {
        Assert.Throws<FormatException>(() => SelectionPolicy.Parse(json));
    }
}
