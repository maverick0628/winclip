using WinClip.Actions;

namespace WinClip.Core.Services;

/// <summary>
/// Defines the contract for managing and accessing WinClip actions.
/// </summary>
public interface IActionRegistry
{
    /// <summary>
    /// Registers an action with the registry.
    /// </summary>
    /// <param name="action">The action to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an action with the same name is already registered.</exception>
    void RegisterAction(IAction action);

    /// <summary>
    /// Unregisters an action from the registry by name.
    /// </summary>
    /// <param name="actionName">The name of the action to unregister.</param>
    /// <returns>True if the action was found and unregistered; otherwise, false.</returns>
    bool UnregisterAction(string actionName);

    /// <summary>
    /// Gets an action by name.
    /// </summary>
    /// <param name="actionName">The name of the action to retrieve.</param>
    /// <returns>The action if found; otherwise, null.</returns>
    IAction? GetAction(string actionName);

    /// <summary>
    /// Gets all registered actions.
    /// </summary>
    /// <returns>A read-only collection of all registered actions.</returns>
    IReadOnlyCollection<IAction> GetAllActions();
}
