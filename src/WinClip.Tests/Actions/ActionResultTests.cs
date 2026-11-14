using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Actions;

public class ActionResultTests
{
    [Fact]
    public void SuccessResult_SetsSuccessTrue()
    {
        var result = ActionResult.SuccessResult();

        Assert.True(result.Success);
        Assert.Null(result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public void SuccessResult_WithMessage_SetsMessage()
    {
        var result = ActionResult.SuccessResult("Done");

        Assert.True(result.Success);
        Assert.Equal("Done", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureResult_SetsSuccessFalse()
    {
        var result = ActionResult.FailureResult("Something went wrong");

        Assert.False(result.Success);
        Assert.Equal("Something went wrong", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public void FailureResult_WithException_CapturesError()
    {
        var ex = new InvalidOperationException("test error");
        var result = ActionResult.FailureResult("Failed", ex);

        Assert.False(result.Success);
        Assert.Equal("Failed", result.Message);
        Assert.Same(ex, result.Error);
    }

    [Fact]
    public void Constructor_AllParameters()
    {
        var ex = new Exception("inner");
        var result = new ActionResult(true, "msg", ex);

        Assert.True(result.Success);
        Assert.Equal("msg", result.Message);
        Assert.Same(ex, result.Error);
    }

    [Fact]
    public void Constructor_DefaultsToNullMessageAndError()
    {
        var result = new ActionResult(false);

        Assert.False(result.Success);
        Assert.Null(result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public void SuccessResult_NullMessage_IsAllowed()
    {
        var result = ActionResult.SuccessResult(null);

        Assert.True(result.Success);
        Assert.Null(result.Message);
    }

    [Fact]
    public void FailureResult_NullException_IsAllowed()
    {
        var result = ActionResult.FailureResult("error", null);

        Assert.False(result.Success);
        Assert.Equal("error", result.Message);
        Assert.Null(result.Error);
    }
}
