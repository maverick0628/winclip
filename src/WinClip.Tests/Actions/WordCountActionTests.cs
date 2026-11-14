using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Actions;

/// <summary>
/// Unit tests for the WordCountAction class.
/// </summary>
public class WordCountActionTests
{
    [Fact]
    public void Name_ShouldReturnWordCount()
    {
        // Arrange
        var action = new WordCountAction();

        // Act
        var name = action.Name;

        // Assert
        Assert.Equal("Word Count", name);
    }

    [Fact]
    public void Description_ShouldReturnExpectedDescription()
    {
        // Arrange
        var action = new WordCountAction();

        // Act
        var description = action.Description;

        // Assert
        Assert.Equal("Count words and characters in selected text", description);
    }

    [Fact]
    public void IconName_ShouldReturnCalculator()
    {
        // Arrange
        var action = new WordCountAction();

        // Act
        var iconName = action.IconName;

        // Assert
        Assert.Equal("Calculator", iconName);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyString_ShouldReturnFailure()
    {
        // Arrange
        var action = new WordCountAction();

        // Act
        var result = await action.ExecuteAsync(string.Empty);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to count", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithNull_ShouldReturnFailure()
    {
        // Arrange
        var action = new WordCountAction();

        // Act
        var result = await action.ExecuteAsync(null!);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to count", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithWhitespace_ShouldReturnFailure()
    {
        // Arrange
        var action = new WordCountAction();

        // Act
        var result = await action.ExecuteAsync("   ");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No text selected to count", result.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleWord_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Hello";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 1, Characters: 5, Characters (no spaces): 5, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleWords_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Hello World Test";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 3, Characters: 16, Characters (no spaces): 14, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultilineText_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Line 1\nLine 2\nLine 3";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 6, Characters: 20, Characters (no spaces): 15, Lines: 3", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithSpecialCharacters_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Hello! How are you?";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 4, Characters: 19, Characters (no spaces): 16, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnicodeCharacters_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Hello \u2713 World \u2714 Test";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // Unicode characters are counted as single characters
        Assert.Equal("Words: 5, Characters: 20, Characters (no spaces): 16, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithTabSeparatedWords_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Hello\tWorld\tTest";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 3, Characters: 16, Characters (no spaces): 14, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedWhitespace_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Hello  \t  World\n\nTest";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // Mixed whitespace: space, tab, newlines
        Assert.Equal("Words: 3, Characters: 21, Characters (no spaces): 14, Lines: 2", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithWindowsLineEndings_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Line 1\r\nLine 2\r\nLine 3";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 6, Characters: 22, Characters (no spaces): 15, Lines: 3", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyLines_ShouldNotCountEmptyLines()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Line 1\n\nLine 2\n\n\nLine 3";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // Empty lines are not counted, only non-whitespace lines
        Assert.Equal("Words: 6, Characters: 23, Characters (no spaces): 15, Lines: 3", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithPunctuationOnly_ShouldCountAsWords()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "!@# $%^ &*(";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // Punctuation without spaces is treated as words when split
        Assert.Equal("Words: 3, Characters: 11, Characters (no spaces): 9, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithNumbers_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "123 456 789";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 3, Characters: 11, Characters (no spaces): 9, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithLongText_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = string.Join(" ", Enumerable.Repeat("word", 1000)); // 1000 words

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // 1000 words, each "word" is 4 chars, plus 999 spaces
        Assert.Equal("Words: 1000, Characters: 4999, Characters (no spaces): 4000, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithLeadingAndTrailingWhitespace_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "  Hello World  ";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 2, Characters: 15, Characters (no spaces): 10, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithSingleCharacter_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "A";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Words: 1, Characters: 1, Characters (no spaces): 1, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithContractions_ShouldCountAsOneWord()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Don't can't won't";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // Contractions are counted as single words since they don't contain whitespace
        Assert.Equal("Words: 3, Characters: 17, Characters (no spaces): 15, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithHyphenatedWords_ShouldCountAsOneWord()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "well-known state-of-the-art user-friendly";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // Hyphenated words are counted as single words
        Assert.Equal("Words: 3, Characters: 41, Characters (no spaces): 39, Lines: 1", result.Message);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmoji_ShouldReturnCorrectCounts()
    {
        // Arrange
        var action = new WordCountAction();
        var testText = "Hello 😀 World 🌍";

        // Act
        var result = await action.ExecuteAsync(testText);

        // Assert
        Assert.True(result.Success);
        // Emojis are counted as characters (may be more than 1 char each in UTF-16)
        Assert.Contains("Words: 4", result.Message);
        Assert.Contains("Lines: 1", result.Message);
        Assert.Null(result.Error);
    }
}
