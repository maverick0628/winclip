using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WinClip.SDK
{
    /// <summary>
    /// Base class for WinClip plugins that provides automatic action discovery via attributes.
    /// Plugin developers can inherit from this class and use the <see cref="PluginAttribute"/>
    /// and <see cref="ActionAttribute"/> to define their plugins declaratively.
    /// </summary>
    public abstract class PluginBase : IPlugin
    {
        private PluginAttribute? _pluginMetadata;
        private Dictionary<string, MethodInfo>? _actionMethods;

        /// <summary>
        /// Gets the unique identifier for this plugin from the <see cref="PluginAttribute"/>.
        /// </summary>
        public virtual string Id
        {
            get
            {
                EnsureMetadataLoaded();
                return _pluginMetadata!.Id;
            }
        }

        /// <summary>
        /// Gets the display name of the plugin from the <see cref="PluginAttribute"/>.
        /// </summary>
        public virtual string Name
        {
            get
            {
                EnsureMetadataLoaded();
                return _pluginMetadata!.Name;
            }
        }

        /// <summary>
        /// Gets the version of the plugin from the <see cref="PluginAttribute"/>.
        /// </summary>
        public virtual string Version
        {
            get
            {
                EnsureMetadataLoaded();
                return _pluginMetadata!.Version;
            }
        }

        /// <summary>
        /// Gets the description of what this plugin does from the <see cref="PluginAttribute"/>.
        /// </summary>
        public virtual string Description
        {
            get
            {
                EnsureMetadataLoaded();
                return _pluginMetadata!.Description;
            }
        }

        /// <summary>
        /// Initializes the plugin. Called once when the plugin is loaded.
        /// Override this method to perform custom initialization logic.
        /// </summary>
        /// <param name="context">The clipboard context providing access to clipboard data.</param>
        public virtual void Initialize(IClipboardContext context)
        {
            // Default implementation does nothing
            // Derived classes can override to add initialization logic
        }

        /// <summary>
        /// Determines whether this plugin should be enabled for the current clipboard content.
        /// Override this method to provide custom enablement logic.
        /// The default implementation returns true (always enabled).
        /// </summary>
        /// <param name="context">The clipboard context containing the current clipboard data.</param>
        /// <returns>True if the plugin should be enabled; otherwise, false.</returns>
        public virtual bool ShouldEnable(IClipboardContext context)
        {
            // Default implementation: always enabled
            return true;
        }

        /// <summary>
        /// Gets the list of actions this plugin provides for the current clipboard content.
        /// This method automatically discovers methods marked with <see cref="ActionAttribute"/>.
        /// Override this method if you need custom action discovery logic.
        /// </summary>
        /// <param name="context">The clipboard context containing the current clipboard data.</param>
        /// <returns>A collection of action identifiers and their display names.</returns>
        public virtual IEnumerable<PluginAction> GetActions(IClipboardContext context)
        {
            EnsureActionsLoaded();

            var actions = new List<PluginAction>();

            foreach (var kvp in _actionMethods!)
            {
                var method = kvp.Value;
                var actionAttr = method.GetCustomAttribute<ActionAttribute>();

                if (actionAttr != null && actionAttr.IsEnabled)
                {
                    actions.Add(new PluginAction
                    {
                        Id = actionAttr.Id,
                        DisplayName = actionAttr.DisplayName,
                        Icon = actionAttr.Icon,
                        Shortcut = actionAttr.Shortcut
                    });
                }
            }

            // Sort by Order property
            return actions.OrderBy(a =>
            {
                var method = _actionMethods![a.Id];
                var attr = method.GetCustomAttribute<ActionAttribute>();
                return attr?.Order ?? 0;
            });
        }

        /// <summary>
        /// Executes the specified action by invoking the corresponding method marked with <see cref="ActionAttribute"/>.
        /// </summary>
        /// <param name="actionId">The identifier of the action to execute.</param>
        /// <param name="context">The action context providing access to clipboard and system operations.</param>
        public virtual void ExecuteAction(string actionId, IActionContext context)
        {
            EnsureActionsLoaded();

            if (_actionMethods!.TryGetValue(actionId, out var method))
            {
                try
                {
                    // Invoke the action method with the context parameter
                    method.Invoke(this, new object[] { context });
                }
                catch (TargetInvocationException ex)
                {
                    // Unwrap the inner exception for clearer error reporting
                    if (ex.InnerException != null)
                        throw ex.InnerException;
                    throw;
                }
            }
            else
            {
                throw new InvalidOperationException($"Action '{actionId}' not found in plugin '{Id}'.");
            }
        }

        /// <summary>
        /// Ensures that plugin metadata has been loaded from the <see cref="PluginAttribute"/>.
        /// </summary>
        private void EnsureMetadataLoaded()
        {
            if (_pluginMetadata == null)
            {
                _pluginMetadata = GetType().GetCustomAttribute<PluginAttribute>();
                if (_pluginMetadata == null)
                {
                    throw new InvalidOperationException(
                        $"Plugin class '{GetType().Name}' must be decorated with the [Plugin] attribute.");
                }
            }
        }

        /// <summary>
        /// Ensures that action methods have been discovered via reflection.
        /// </summary>
        private void EnsureActionsLoaded()
        {
            if (_actionMethods == null)
            {
                _actionMethods = new Dictionary<string, MethodInfo>();

                var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    var actionAttr = method.GetCustomAttribute<ActionAttribute>();
                    if (actionAttr != null)
                    {
                        // Validate method signature: void MethodName(IActionContext context)
                        var parameters = method.GetParameters();
                        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(IActionContext))
                        {
                            throw new InvalidOperationException(
                                $"Action method '{method.Name}' in plugin '{GetType().Name}' must have exactly one parameter of type IActionContext.");
                        }

                        if (method.ReturnType != typeof(void))
                        {
                            throw new InvalidOperationException(
                                $"Action method '{method.Name}' in plugin '{GetType().Name}' must have a void return type.");
                        }

                        if (_actionMethods.ContainsKey(actionAttr.Id))
                        {
                            throw new InvalidOperationException(
                                $"Duplicate action ID '{actionAttr.Id}' found in plugin '{GetType().Name}'.");
                        }

                        _actionMethods[actionAttr.Id] = method;
                    }
                }
            }
        }
    }
}
