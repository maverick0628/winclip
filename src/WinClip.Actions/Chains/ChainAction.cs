namespace WinClip.Actions.Chains;

/// <summary>
/// Runtime <see cref="IAction"/> wrapper around a user-defined <see cref="ActionChain"/>.
///
/// The chain is executed step by step. Each step's transformed output is read back
/// from the clipboard (where built-in actions write their results) and fed in as
/// the next step's <c>selectedText</c>. The first step receives the original
/// <c>selectedText</c>. The final clipboard content is the chain's output.
///
/// If any step returns <c>Success = false</c> the chain aborts with that step's error.
/// Missing actions (by name) abort the chain with a clear error message — chains
/// referencing renamed/removed plugins won't silently skip.
/// </summary>
public sealed class ChainAction : IAction
{
    private readonly ActionChain _chain;
    private readonly Func<string, IAction?> _resolver;

    public ChainAction(ActionChain chain, Func<string, IAction?> resolver)
    {
        _chain = chain ?? throw new ArgumentNullException(nameof(chain));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    }

    public string Name => _chain.Name;
    public string Description => _chain.Description ?? $"Chain: {string.Join(" → ", _chain.ActionIds)}";
    public string IconName => "Chain";

    public bool IsAvailable() => _chain.ActionIds.Count > 0;

    public async Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        if (_chain.ActionIds.Count == 0)
            return ActionResult.FailureResult($"Chain '{Name}' has no steps");

        var current = selectedText;
        for (var i = 0; i < _chain.ActionIds.Count; i++)
        {
            var stepName = _chain.ActionIds[i];
            var step = _resolver(stepName);
            if (step == null)
                return ActionResult.FailureResult(
                    $"Chain '{Name}' step {i + 1} references unknown action '{stepName}'");

            try
            {
                var stepResult = await step.ExecuteAsync(current, context);
                if (!stepResult.Success)
                    return ActionResult.FailureResult(
                        $"Chain '{Name}' failed at step {i + 1} ({stepName}): {stepResult.Message ?? "unspecified error"}",
                        stepResult.Error);

                // The step has written its output to the clipboard. Read it back
                // as the input for the next step (built-in actions follow this contract).
                if (i < _chain.ActionIds.Count - 1)
                {
                    try
                    {
                        var next = System.Windows.Clipboard.GetText();
                        if (!string.IsNullOrEmpty(next))
                            current = next;
                    }
                    catch
                    {
                        // Clipboard locked by another app — fall back to the prior buffer
                        // so the chain doesn't abort on a transient OLE error.
                    }
                }
            }
            catch (Exception ex)
            {
                return ActionResult.FailureResult(
                    $"Chain '{Name}' threw at step {i + 1} ({stepName}): {ex.Message}", ex);
            }
        }

        // The final clipboard content is the chain output; mirror the
        // built-in transform contract so the host pastes the result.
        return ActionResult.SuccessWithPaste($"Ran chain: {Name}");
    }
}
