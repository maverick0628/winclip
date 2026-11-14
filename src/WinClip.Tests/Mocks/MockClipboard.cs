namespace WinClip.Tests.Mocks;

/// <summary>
/// Mock implementation of clipboard functionality for testing.
/// This mock can be used to test actions without requiring actual clipboard access.
/// </summary>
public class MockClipboard
{
    private string? _clipboardText;
    private bool _throwOnSetText;

    /// <summary>
    /// Gets the text that was set to the clipboard.
    /// </summary>
    public string? ClipboardText => _clipboardText;

    /// <summary>
    /// Gets the number of times SetText was called.
    /// </summary>
    public int SetTextCallCount { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the clipboard should throw an exception on SetText.
    /// </summary>
    public bool ThrowOnSetText
    {
        get => _throwOnSetText;
        set => _throwOnSetText = value;
    }

    /// <summary>
    /// Simulates setting text to the clipboard.
    /// </summary>
    /// <param name="text">The text to set.</param>
    /// <exception cref="InvalidOperationException">Thrown when ThrowOnSetText is true.</exception>
    public void SetText(string text)
    {
        if (_throwOnSetText)
        {
            throw new InvalidOperationException("Clipboard access denied");
        }

        _clipboardText = text;
        SetTextCallCount++;
    }

    /// <summary>
    /// Resets the mock clipboard to its initial state.
    /// </summary>
    public void Reset()
    {
        _clipboardText = null;
        SetTextCallCount = 0;
        _throwOnSetText = false;
    }
}
