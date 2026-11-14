using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Actions;

public class HashAndBase64ActionTests
{
    [Theory]
    [InlineData(HashAlgorithmKind.MD5, "Hash MD5")]
    [InlineData(HashAlgorithmKind.SHA1, "Hash SHA1")]
    [InlineData(HashAlgorithmKind.SHA256, "Hash SHA256")]
    public void HashAction_Name_IsCorrect(HashAlgorithmKind algorithm, string expectedName)
    {
        var action = new HashAction(algorithm);

        Assert.Equal(expectedName, action.Name);
        Assert.Equal("Hash", action.IconName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HashAction_EmptyInput_ReturnsFailure(string input)
    {
        var action = new HashAction(HashAlgorithmKind.SHA256);
        var result = await action.ExecuteAsync(input);

        Assert.False(result.Success);
        Assert.Equal("No text selected to hash", result.Message);
    }

    [Fact]
    public async Task Base64Encode_EmptyInput_ReturnsFailure()
    {
        var action = new Base64EncodeAction();
        var result = await action.ExecuteAsync("");

        Assert.False(result.Success);
        Assert.Equal("No text selected to encode", result.Message);
    }

    [Fact]
    public async Task Base64Decode_InvalidInput_ReturnsFailure()
    {
        var action = new Base64DecodeAction();
        var result = await action.ExecuteAsync("this-is-not-base64");

        Assert.False(result.Success);
        Assert.Equal("Invalid Base64 — could not decode", result.Message);
    }
}
