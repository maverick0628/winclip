using System.Collections.Concurrent;
using WinClip.Actions;

namespace WinClip.Core.Services;

/// <summary>
/// Manages the collection of available WinClip actions.
/// Thread-safe implementation using concurrent dictionary.
/// </summary>
public class ActionRegistry : IActionRegistry
{
    private readonly ConcurrentDictionary<string, IAction> _actions = new();
    private volatile IReadOnlyCollection<IAction>? _cachedAll;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Runs inside the cache lock after a rebuild has read the actions and before it publishes them.
    /// Tests use it to land a register or unregister in that window.
    /// </summary>
    internal Action? AfterSnapshotForTests { get; set; }

    /// <summary>
    /// Registers an action with the registry.
    /// </summary>
    /// <param name="action">The action to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an action with the same name is already registered.</exception>
    public void RegisterAction(IAction action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (string.IsNullOrWhiteSpace(action.Name))
        {
            throw new ArgumentException("Action name cannot be null or whitespace.", nameof(action));
        }

        if (!_actions.TryAdd(action.Name, action))
        {
            throw new InvalidOperationException($"An action with the name '{action.Name}' is already registered.");
        }

        InvalidateCache();
    }

    /// <summary>
    /// Unregisters an action from the registry by name.
    /// </summary>
    /// <param name="actionName">The name of the action to unregister.</param>
    /// <returns>True if the action was found and unregistered; otherwise, false.</returns>
    public bool UnregisterAction(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return false;
        }

        var removed = _actions.TryRemove(actionName, out _);
        if (removed)
        {
            InvalidateCache();
        }

        return removed;
    }

    /// <summary>
    /// Gets an action by name.
    /// </summary>
    /// <param name="actionName">The name of the action to retrieve.</param>
    /// <returns>The action if found; otherwise, null.</returns>
    public IAction? GetAction(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName))
        {
            return null;
        }

        _actions.TryGetValue(actionName, out var action);
        return action;
    }

    /// <summary>
    /// Gets all registered actions.
    /// Returns a cached read-only collection, rebuilt only when the registry changes.
    /// </summary>
    /// <returns>A read-only collection of all registered actions.</returns>
    public IReadOnlyCollection<IAction> GetAllActions()
    {
        var cached = _cachedAll;
        if (cached != null)
            return cached;

        lock (_cacheLock)
        {
            cached = _cachedAll;
            if (cached != null)
                return cached;
            var snapshot = _actions.Values.ToList().AsReadOnly();
            AfterSnapshotForTests?.Invoke();
            _cachedAll = snapshot;
            return snapshot;
        }
    }

    /// <summary>
    /// Clears the cached list under the cache lock.
    /// A rebuild that read the actions before this change publishes first, so the clear always lands after it.
    /// Clearing without the lock let a rebuild overwrite the clear with its stale list.
    /// </summary>
    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedAll = null;
        }
    }
}
