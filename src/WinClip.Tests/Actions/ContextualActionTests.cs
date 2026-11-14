using Moq;
using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Actions;

/// <summary>
/// Tests for IContextualAction.IsRelevantFor across all contextual action classes.
/// </summary>
public class ContextualActionTests
{
    // ---------------------------------------------------------------
    // SmartExtractAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("Contact us at support@company.org for help")]
    [InlineData("https://example.com")]
    [InlineData("Visit http://foo.bar/baz for details")]
    [InlineData("(555) 123-4567")]
    [InlineData("Call me at 555-123-4567 anytime")]
    [InlineData("+1 800 555 1234")]
    [InlineData("192.168.1.1")]
    [InlineData("Server IP is 10.0.0.1 on port 80")]
    public void SmartExtract_IsRelevantFor_ReturnsTrue_WhenPatternDetected(string input)
    {
        var action = new SmartExtractAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("just plain text with no patterns")]
    [InlineData("hello world")]
    [InlineData("12345")]
    public void SmartExtract_IsRelevantFor_ReturnsFalse_WhenNoPatterns(string input)
    {
        var action = new SmartExtractAction();
        Assert.False(action.IsRelevantFor(input));
    }

    [Fact]
    public void SmartExtract_IsRelevantFor_ReturnsFalse_ForNull()
    {
        var action = new SmartExtractAction();
        Assert.False(action.IsRelevantFor(null!));
    }

    // ---------------------------------------------------------------
    // MarkdownTableAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("Name,Age\nAlice,30")]
    [InlineData("Col1,Col2,Col3\nA,B,C\nD,E,F")]
    [InlineData("Name\tAge\nAlice\t30")]
    public void MarkdownTable_IsRelevantFor_ReturnsTrue_WhenCsvOrTsv(string input)
    {
        var action = new MarkdownTableAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("single line with commas, here, and here")]
    [InlineData("no commas\njust lines")]
    [InlineData("single line no delimiter")]
    public void MarkdownTable_IsRelevantFor_ReturnsFalse_WhenNotCsvOrTsv(string input)
    {
        var action = new MarkdownTableAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // RemoveBlankLinesAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("line1\nline2")]
    [InlineData("line1\n\nline3")]
    [InlineData("a\nb\nc")]
    public void RemoveBlankLines_IsRelevantFor_ReturnsTrue_WhenMultiLine(string input)
    {
        var action = new RemoveBlankLinesAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("single line no newline")]
    public void RemoveBlankLines_IsRelevantFor_ReturnsFalse_WhenSingleLine(string input)
    {
        var action = new RemoveBlankLinesAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // AddLineNumbersAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("line1\nline2")]
    [InlineData("first\nsecond\nthird")]
    public void AddLineNumbers_IsRelevantFor_ReturnsTrue_WhenMultiLine(string input)
    {
        var action = new AddLineNumbersAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no newlines here")]
    public void AddLineNumbers_IsRelevantFor_ReturnsFalse_WhenSingleLine(string input)
    {
        var action = new AddLineNumbersAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // NumberBaseConvertAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("42")]
    [InlineData("-7")]
    [InlineData("0xFF")]
    [InlineData("0XAB")]
    [InlineData("0b1010")]
    [InlineData("0B1100")]
    [InlineData("  255  ")]
    [InlineData("0")]
    public void NumberBaseConvert_IsRelevantFor_ReturnsTrue_WhenNumberDetected(string input)
    {
        var action = new NumberBaseConvertAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a number")]
    [InlineData("12.34")]
    [InlineData("0xGHIJ")]
    [InlineData("0b222")]
    [InlineData("12 34")]
    public void NumberBaseConvert_IsRelevantFor_ReturnsFalse_WhenNotNumber(string input)
    {
        var action = new NumberBaseConvertAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // HtmlStripAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("<p>Hello</p>")]
    [InlineData("<div class=\"test\">content</div>")]
    [InlineData("Some text <br/> more text")]
    [InlineData("<!-- comment -->")]
    [InlineData("<!DOCTYPE html>")]
    [InlineData("<a href=\"url\">link</a>")]
    public void HtmlStrip_IsRelevantFor_ReturnsTrue_WhenHtmlDetected(string input)
    {
        var action = new HtmlStripAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plain text with no html")]
    [InlineData("2 < 3 and 5 > 4")]
    [InlineData("less than < greater than")]
    public void HtmlStrip_IsRelevantFor_ReturnsFalse_WhenNoHtml(string input)
    {
        var action = new HtmlStripAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // LineSortAction (all modes)
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(LineSortMode.SortAscending)]
    [InlineData(LineSortMode.SortDescending)]
    [InlineData(LineSortMode.RemoveDuplicates)]
    [InlineData(LineSortMode.Reverse)]
    public void LineSort_IsRelevantFor_ReturnsTrue_WhenMultiLine(LineSortMode mode)
    {
        var action = new LineSortAction(mode);
        Assert.True(action.IsRelevantFor("line1\nline2"));
    }

    [Theory]
    [InlineData(LineSortMode.SortAscending)]
    [InlineData(LineSortMode.SortDescending)]
    [InlineData(LineSortMode.RemoveDuplicates)]
    [InlineData(LineSortMode.Reverse)]
    public void LineSort_IsRelevantFor_ReturnsFalse_WhenSingleLine(LineSortMode mode)
    {
        var action = new LineSortAction(mode);
        Assert.False(action.IsRelevantFor("single line"));
    }

    [Theory]
    [InlineData(LineSortMode.SortAscending)]
    [InlineData(LineSortMode.SortDescending)]
    [InlineData(LineSortMode.RemoveDuplicates)]
    [InlineData(LineSortMode.Reverse)]
    public void LineSort_IsRelevantFor_ReturnsFalse_WhenEmpty(LineSortMode mode)
    {
        var action = new LineSortAction(mode);
        Assert.False(action.IsRelevantFor(""));
        Assert.False(action.IsRelevantFor("   "));
        Assert.False(action.IsRelevantFor(null!));
    }

    // ---------------------------------------------------------------
    // TimestampConvertAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("1700000000")]
    [InlineData("1700000000000")]
    [InlineData("2024-01-15")]
    [InlineData("2024-01-15T10:30:00Z")]
    [InlineData("January 15, 2024")]
    [InlineData("  1700000000  ")]
    public void TimestampConvert_IsRelevantFor_ReturnsTrue_WhenTimestampOrDate(string input)
    {
        var action = new TimestampConvertAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a date")]
    [InlineData("hello world")]
    [InlineData("12345")]
    [InlineData("123456789012345")]
    public void TimestampConvert_IsRelevantFor_ReturnsFalse_WhenNotTimestamp(string input)
    {
        var action = new TimestampConvertAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // ColorConvertAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#fff")]
    [InlineData("FF0000")]
    [InlineData("abc")]
    [InlineData("rgb(255, 0, 0)")]
    [InlineData("rgba(255, 128, 0, 0.5)")]
    [InlineData("hsl(120, 50, 50)")]
    [InlineData("hsl(120, 50%, 50%)")]
    [InlineData("hsla(240, 100%, 50%, 1)")]
    public void ColorConvert_IsRelevantFor_ReturnsTrue_WhenColorDetected(string input)
    {
        var action = new ColorConvertAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a color")]
    [InlineData("hello world")]
    [InlineData("#ZZZZZZ")]
    [InlineData("rgb(abc)")]
    [InlineData("#12345")]
    public void ColorConvert_IsRelevantFor_ReturnsFalse_WhenNotColor(string input)
    {
        var action = new ColorConvertAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // JsonFormatAction (Prettify and Minify)
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(JsonFormatMode.Prettify)]
    [InlineData(JsonFormatMode.Minify)]
    public void JsonFormat_IsRelevantFor_ReturnsTrue_WhenJsonLike(JsonFormatMode mode)
    {
        var action = new JsonFormatAction(mode);
        Assert.True(action.IsRelevantFor("{\"key\": \"value\"}"));
        Assert.True(action.IsRelevantFor("[1, 2, 3]"));
        Assert.True(action.IsRelevantFor("  {\"nested\": true}"));
        Assert.True(action.IsRelevantFor("  [\"array\"]"));
    }

    [Theory]
    [InlineData(JsonFormatMode.Prettify)]
    [InlineData(JsonFormatMode.Minify)]
    public void JsonFormat_IsRelevantFor_ReturnsFalse_WhenNotJson(JsonFormatMode mode)
    {
        var action = new JsonFormatAction(mode);
        Assert.False(action.IsRelevantFor(""));
        Assert.False(action.IsRelevantFor("   "));
        Assert.False(action.IsRelevantFor("plain text"));
        Assert.False(action.IsRelevantFor("not json at all"));
        Assert.False(action.IsRelevantFor("<html>not json</html>"));
    }

    // ---------------------------------------------------------------
    // Base64DecodeAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=")]
    [InlineData("dGVzdA==")]
    [InlineData("AQIDBA==")]
    [InlineData("YWJjZA==")]
    public void Base64Decode_IsRelevantFor_ReturnsTrue_WhenBase64Detected(string input)
    {
        var action = new Base64DecodeAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("not base64!@#")]
    [InlineData("sh")]
    public void Base64Decode_IsRelevantFor_ReturnsFalse_WhenNotBase64(string input)
    {
        var action = new Base64DecodeAction();
        Assert.False(action.IsRelevantFor(input));
    }

    [Fact]
    public void Base64Decode_IsRelevantFor_ReturnsFalse_WhenLengthNotMultipleOf4()
    {
        var action = new Base64DecodeAction();
        // 5 chars, not padded to multiple of 4
        Assert.False(action.IsRelevantFor("ABCDE"));
    }

    // ---------------------------------------------------------------
    // MarkdownStripAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("# Heading")]
    [InlineData("## Sub Heading")]
    [InlineData("**bold text**")]
    [InlineData("__also bold__")]
    [InlineData("~~strikethrough~~")]
    [InlineData("```code block```")]
    [InlineData("- list item")]
    [InlineData("* another item")]
    [InlineData("+ plus item")]
    [InlineData("1. numbered item")]
    [InlineData("> blockquote")]
    [InlineData("[link text](http://example.com)")]
    public void MarkdownStrip_IsRelevantFor_ReturnsTrue_WhenMarkdownDetected(string input)
    {
        var action = new MarkdownStripAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("just plain text")]
    [InlineData("no markdown here")]
    public void MarkdownStrip_IsRelevantFor_ReturnsFalse_WhenNoMarkdown(string input)
    {
        var action = new MarkdownStripAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // CsvJsonConvertAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("[{\"name\": \"Alice\"}]")]
    [InlineData("[1, 2, 3]")]
    [InlineData("  [{\"key\": \"val\"}]")]
    public void CsvJsonConvert_IsRelevantFor_ReturnsTrue_WhenJsonArray(string input)
    {
        var action = new CsvJsonConvertAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("Name,Age\nAlice,30")]
    [InlineData("A,B,C\n1,2,3")]
    public void CsvJsonConvert_IsRelevantFor_ReturnsTrue_WhenCsv(string input)
    {
        var action = new CsvJsonConvertAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plain text")]
    [InlineData("single line, no newlines")]
    [InlineData("{\"object\": true}")]
    [InlineData("no commas\nno array")]
    public void CsvJsonConvert_IsRelevantFor_ReturnsFalse_WhenNeitherCsvNorJsonArray(string input)
    {
        var action = new CsvJsonConvertAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // OpenUrlAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("ftp://files.example.com")]
    [InlineData("www.example.com")]
    [InlineData("example.com")]
    [InlineData("foo.co.uk")]
    public void OpenUrl_IsRelevantFor_ReturnsTrue_WhenUrlDetected(string input)
    {
        var action = new OpenUrlAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("just some words")]
    [InlineData("not a url at all")]
    [InlineData("hello world with spaces")]
    public void OpenUrl_IsRelevantFor_ReturnsFalse_WhenNotUrl(string input)
    {
        var action = new OpenUrlAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // UrlDecodeAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("hello%20world")]
    [InlineData("https://example.com/path%2Fto%2Fpage")]
    [InlineData("%3Cscript%3E")]
    [InlineData("a%20b%20c")]
    public void UrlDecode_IsRelevantFor_ReturnsTrue_WhenPercentEncoded(string input)
    {
        var action = new UrlDecodeAction();
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plain text")]
    [InlineData("no percent encoding")]
    [InlineData("100% complete")]
    [InlineData("50%GG")]
    public void UrlDecode_IsRelevantFor_ReturnsFalse_WhenNoPercentEncoding(string input)
    {
        var action = new UrlDecodeAction();
        Assert.False(action.IsRelevantFor(input));
    }

    // ---------------------------------------------------------------
    // DictionaryAction
    // ---------------------------------------------------------------

    [Theory]
    [InlineData("hello")]
    [InlineData("test")]
    [InlineData("ice cream")]
    [InlineData("hot dog stand")]
    public void Dictionary_IsRelevantFor_ReturnsTrue_WhenShortText(string input)
    {
        var mockBrowser = new Mock<IBrowserService>();
        var action = new DictionaryAction(mockBrowser.Object);
        Assert.True(action.IsRelevantFor(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("this is a longer phrase with many words")]
    [InlineData("one two three four")]
    public void Dictionary_IsRelevantFor_ReturnsFalse_WhenLongTextOrEmpty(string input)
    {
        var mockBrowser = new Mock<IBrowserService>();
        var action = new DictionaryAction(mockBrowser.Object);
        Assert.False(action.IsRelevantFor(input));
    }

    [Fact]
    public void Dictionary_IsRelevantFor_ReturnsFalse_ForNull()
    {
        var mockBrowser = new Mock<IBrowserService>();
        var action = new DictionaryAction(mockBrowser.Object);
        Assert.False(action.IsRelevantFor(null!));
    }

    // ---------------------------------------------------------------
    // Null/whitespace edge cases across all parameterless actions
    // ---------------------------------------------------------------

    [Theory]
    [MemberData(nameof(AllParameterlessContextualActions))]
    public void AllActions_IsRelevantFor_ReturnsFalse_ForNull(IContextualAction action)
    {
        Assert.False(action.IsRelevantFor(null!));
    }

    [Theory]
    [MemberData(nameof(AllParameterlessContextualActions))]
    public void AllActions_IsRelevantFor_ReturnsFalse_ForEmptyString(IContextualAction action)
    {
        Assert.False(action.IsRelevantFor(""));
    }

    [Theory]
    [MemberData(nameof(AllParameterlessContextualActions))]
    public void AllActions_IsRelevantFor_ReturnsFalse_ForWhitespace(IContextualAction action)
    {
        Assert.False(action.IsRelevantFor("   "));
    }

    /// <summary>
    /// Provides instances of all contextual actions that can be constructed
    /// without external dependencies (i.e. no IBrowserService required).
    /// </summary>
    public static IEnumerable<object[]> AllParameterlessContextualActions()
    {
        yield return new object[] { new SmartExtractAction() };
        yield return new object[] { new MarkdownTableAction() };
        yield return new object[] { new RemoveBlankLinesAction() };
        yield return new object[] { new AddLineNumbersAction() };
        yield return new object[] { new NumberBaseConvertAction() };
        yield return new object[] { new HtmlStripAction() };
        yield return new object[] { new LineSortAction(LineSortMode.SortAscending) };
        yield return new object[] { new LineSortAction(LineSortMode.SortDescending) };
        yield return new object[] { new LineSortAction(LineSortMode.RemoveDuplicates) };
        yield return new object[] { new LineSortAction(LineSortMode.Reverse) };
        yield return new object[] { new TimestampConvertAction() };
        yield return new object[] { new ColorConvertAction() };
        yield return new object[] { new JsonFormatAction(JsonFormatMode.Prettify) };
        yield return new object[] { new JsonFormatAction(JsonFormatMode.Minify) };
        yield return new object[] { new Base64DecodeAction() };
        yield return new object[] { new MarkdownStripAction() };
        yield return new object[] { new CsvJsonConvertAction() };
        yield return new object[] { new OpenUrlAction() };
        yield return new object[] { new UrlDecodeAction() };
    }
}
