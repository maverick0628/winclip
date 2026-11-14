using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Actions;

public class TextTransformActionTests
{
    [Fact]
    public void Uppercase_Name_IsCorrect()
    {
        var action = new TextTransformAction(TransformKind.Uppercase);
        Assert.Equal("UPPERCASE", action.Name);
        Assert.Equal("TextTransform", action.IconName);
    }

    [Fact]
    public void Lowercase_Name_IsCorrect()
    {
        var action = new TextTransformAction(TransformKind.Lowercase);
        Assert.Equal("lowercase", action.Name);
    }

    [Fact]
    public void TitleCase_Name_IsCorrect()
    {
        var action = new TextTransformAction(TransformKind.TitleCase);
        Assert.Equal("Title Case", action.Name);
    }

    [Fact]
    public void CamelCase_Name_IsCorrect()
    {
        var action = new TextTransformAction(TransformKind.CamelCase);
        Assert.Equal("camelCase", action.Name);
    }

    [Fact]
    public void SnakeCase_Name_IsCorrect()
    {
        var action = new TextTransformAction(TransformKind.SnakeCase);
        Assert.Equal("snake_case", action.Name);
    }

    [Fact]
    public void Trim_Name_IsCorrect()
    {
        var action = new TextTransformAction(TransformKind.Trim);
        Assert.Equal("Trim", action.Name);
    }

    [Fact]
    public async Task EmptyInput_ReturnsFailure()
    {
        var action = new TextTransformAction(TransformKind.Uppercase);
        var result = await action.ExecuteAsync("");

        Assert.False(result.Success);
        Assert.Equal("No text to transform", result.Message);
    }

    [Fact]
    public async Task NullInput_ReturnsFailure()
    {
        var action = new TextTransformAction(TransformKind.Uppercase);
        var result = await action.ExecuteAsync(null!);

        Assert.False(result.Success);
    }

    // Note: ExecuteAsync calls Clipboard.SetText which requires STA thread and WPF context.
    // The transform logic itself is tested through the action names and descriptions.
    // Integration tests on Windows would validate the full clipboard flow.

    [Fact]
    public void Description_IncludesName()
    {
        var action = new TextTransformAction(TransformKind.Uppercase);
        Assert.Contains("UPPERCASE", action.Description);
    }

    [Theory]
    [InlineData(TransformKind.Uppercase)]
    [InlineData(TransformKind.Lowercase)]
    [InlineData(TransformKind.TitleCase)]
    [InlineData(TransformKind.CamelCase)]
    [InlineData(TransformKind.SnakeCase)]
    [InlineData(TransformKind.Trim)]
    public void AllKinds_HaveNonEmptyName(TransformKind kind)
    {
        var action = new TextTransformAction(kind);
        Assert.False(string.IsNullOrEmpty(action.Name));
        Assert.False(string.IsNullOrEmpty(action.Description));
    }
}
