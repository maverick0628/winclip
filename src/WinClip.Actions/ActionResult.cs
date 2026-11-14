namespace WinClip.Actions;

/// <summary>
/// Represents the result of an action execution.
/// </summary>
public class ActionResult
{
    /// <summary>
    /// Gets a value indicating whether the action executed successfully.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets an optional message describing the result (e.g., success confirmation or error details).
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Gets the exception that occurred during execution, if any.
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the result should be automatically pasted
    /// back into the active application (replacing the selected text).
    /// Only relevant for in-place text transform actions (e.g., uppercase, URL-encode).
    /// </summary>
    public bool AutoPaste { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionResult"/> class.
    /// </summary>
    /// <param name="success">Whether the action executed successfully.</param>
    /// <param name="message">Optional message describing the result.</param>
    /// <param name="error">Optional exception that occurred during execution.</param>
    /// <param name="autoPaste">Whether to auto-paste the clipboard content back into the active app.</param>
    public ActionResult(bool success, string? message = null, Exception? error = null, bool autoPaste = false)
    {
        Success = success;
        Message = message;
        Error = error;
        AutoPaste = autoPaste;
    }

    /// <summary>
    /// Creates a successful action result.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <returns>A successful <see cref="ActionResult"/>.</returns>
    public static ActionResult SuccessResult(string? message = null)
    {
        return new ActionResult(true, message);
    }

    /// <summary>
    /// Creates a successful action result that triggers auto-paste.
    /// Use for in-place text transforms where the clipboard content should replace the selection.
    /// </summary>
    /// <param name="message">Optional success message.</param>
    /// <returns>A successful <see cref="ActionResult"/> with AutoPaste enabled.</returns>
    public static ActionResult SuccessWithPaste(string? message = null)
    {
        return new ActionResult(true, message, autoPaste: true);
    }

    /// <summary>
    /// Creates a failed action result.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="error">Optional exception that caused the failure.</param>
    /// <returns>A failed <see cref="ActionResult"/>.</returns>
    public static ActionResult FailureResult(string message, Exception? error = null)
    {
        return new ActionResult(false, message, error);
    }
}
