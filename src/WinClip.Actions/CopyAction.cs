using System.Windows;
using System.Windows.Threading;

namespace WinClip.Actions;

/// <summary>
/// Action that copies the selected text to the system clipboard.
/// </summary>
public class CopyAction : IAction
{
    /// <summary>
    /// Gets the display name of the action.
    /// </summary>
    public string Name => "Copy";

    /// <summary>
    /// Gets a brief description of what the action does.
    /// </summary>
    public string Description => "Copy selected text to clipboard";

    /// <summary>
    /// Gets the name of the icon to display for this action.
    /// </summary>
    public string IconName => "Copy";

    /// <summary>
    /// Determines whether the copy action is available.
    /// Copy action is always available.
    /// </summary>
    /// <returns>Always returns true.</returns>
    public bool IsAvailable()
    {
        return true;
    }

    /// <summary>
    /// Executes the copy action asynchronously on the selected text.
    /// </summary>
    /// <param name="selectedText">The text that was selected by the user.</param>
    /// <param name="context">Optional context information (not used by copy action).</param>
    /// <returns>A task that represents the asynchronous operation, containing the result of the action execution.</returns>
    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            // Validate input — reject null, empty, and whitespace-only strings
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                return Task.FromResult(ActionResult.FailureResult("No text selected to copy"));
            }

            // Clipboard.SetText requires an STA thread. If the current thread
            // is not STA, marshal the call to the WPF dispatcher.
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                var tcs = new TaskCompletionSource<ActionResult>();
                var dispatcher = Application.Current?.Dispatcher;

                if (dispatcher == null)
                {
                    return Task.FromResult(ActionResult.FailureResult(
                        "Cannot access clipboard: no UI dispatcher available"));
                }

                dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        ClipboardService.Set(selectedText);
                        tcs.SetResult(ActionResult.SuccessResult("Text copied to clipboard"));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetResult(ActionResult.FailureResult(
                            "Failed to copy text to clipboard", ex));
                    }
                });

                return tcs.Task;
            }

            // Already on STA thread — call directly
            ClipboardService.Set(selectedText);

            return Task.FromResult(ActionResult.SuccessResult("Text copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult(
                "Failed to copy text to clipboard",
                ex));
        }
    }
}
