using System.Reflection;
using System.Runtime.Loader;

namespace WinClip.Plugins.Security;

/// <summary>
/// Provides a sandbox environment for executing plugin code with isolation and security constraints.
/// Uses AssemblyLoadContext for assembly isolation in .NET 8.0+ (modern replacement for AppDomain).
/// </summary>
public sealed class PluginSandbox : IDisposable
{
    private readonly PluginAssemblyLoadContext _loadContext;
    private readonly PermissionType _grantedPermissions;
    private readonly TimeSpan _executionTimeout;
    private bool _disposed;

    /// <summary>
    /// Gets the granted permissions for this sandbox.
    /// </summary>
    public PermissionType GrantedPermissions => _grantedPermissions;

    /// <summary>
    /// Gets the execution timeout for operations in this sandbox.
    /// </summary>
    public TimeSpan ExecutionTimeout => _executionTimeout;

    /// <summary>
    /// Gets whether this sandbox has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginSandbox"/> class.
    /// </summary>
    /// <param name="grantedPermissions">The permissions granted to code running in this sandbox.</param>
    /// <param name="executionTimeout">The maximum execution time for operations. Defaults to 30 seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when execution timeout is less than or equal to zero.</exception>
    public PluginSandbox(PermissionType grantedPermissions, TimeSpan? executionTimeout = null)
    {
        _grantedPermissions = grantedPermissions;
        _executionTimeout = executionTimeout ?? TimeSpan.FromSeconds(30);

        if (_executionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(executionTimeout), "Execution timeout must be greater than zero.");
        }

        _loadContext = new PluginAssemblyLoadContext();
    }

    /// <summary>
    /// Loads an assembly into the sandbox from a file path.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly file.</param>
    /// <returns>The loaded assembly.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assemblyPath is null or whitespace.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file does not exist.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the sandbox has been disposed.</exception>
    public Assembly LoadAssembly(string assemblyPath)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new ArgumentNullException(nameof(assemblyPath));
        }

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly file not found: {assemblyPath}", assemblyPath);
        }

        return _loadContext.LoadFromAssemblyPath(assemblyPath);
    }

    /// <summary>
    /// Creates an instance of a type from a loaded assembly.
    /// </summary>
    /// <typeparam name="T">The type to instantiate (typically IPlugin).</typeparam>
    /// <param name="assembly">The assembly containing the type.</param>
    /// <param name="typeName">The full name of the type to instantiate.</param>
    /// <returns>An instance of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when assembly or typeName is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the type cannot be found or instantiated.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the sandbox has been disposed.</exception>
    public T CreateInstance<T>(Assembly assembly, string typeName) where T : class
    {
        ThrowIfDisposed();

        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        var type = assembly.GetType(typeName);
        if (type == null)
        {
            throw new InvalidOperationException($"Type '{typeName}' not found in assembly '{assembly.FullName}'.");
        }

        if (!typeof(T).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"Type '{typeName}' does not implement '{typeof(T).Name}'.");
        }

        var instance = Activator.CreateInstance(type) as T;
        if (instance == null)
        {
            throw new InvalidOperationException($"Failed to create instance of type '{typeName}'.");
        }

        return instance;
    }

    /// <summary>
    /// Executes an action within the sandbox with timeout enforcement.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the action exceeds the execution timeout.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the sandbox has been disposed.</exception>
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        using var timeoutCts = new CancellationTokenSource(_executionTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await action().WaitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation exceeded the execution timeout of {_executionTimeout.TotalSeconds} seconds.");
        }
    }

    /// <summary>
    /// Executes a function within the sandbox with timeout enforcement.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with a result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when func is null.</exception>
    /// <exception cref="TimeoutException">Thrown when the function exceeds the execution timeout.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the sandbox has been disposed.</exception>
    public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (func == null)
        {
            throw new ArgumentNullException(nameof(func));
        }

        using var timeoutCts = new CancellationTokenSource(_executionTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await func().WaitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation exceeded the execution timeout of {_executionTimeout.TotalSeconds} seconds.");
        }
    }

    /// <summary>
    /// Checks if a specific permission has been granted to this sandbox.
    /// </summary>
    /// <param name="permission">The permission to check.</param>
    /// <returns>True if the permission is granted; false otherwise.</returns>
    public bool HasPermission(PermissionType permission)
    {
        return _grantedPermissions.HasFlag(permission);
    }

    /// <summary>
    /// Validates that a required permission has been granted.
    /// </summary>
    /// <param name="permission">The required permission.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when the required permission has not been granted.</exception>
    public void RequirePermission(PermissionType permission)
    {
        if (!HasPermission(permission))
        {
            var permissionName = PluginPermission.GetDisplayName(permission);
            throw new UnauthorizedAccessException($"The plugin does not have the required permission: {permissionName}");
        }
    }

    /// <summary>
    /// Unloads the plugin sandbox and releases all associated resources.
    /// </summary>
    public void Unload()
    {
        if (!_disposed)
        {
            _loadContext.Unload();
            _disposed = true;
        }
    }

    /// <summary>
    /// Disposes the sandbox and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Unload();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(PluginSandbox));
        }
    }
}

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation.
/// Allows plugins to be loaded and unloaded independently.
/// </summary>
internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    public PluginAssemblyLoadContext() : base(isCollectible: true)
    {
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Return null to use default load behavior for dependencies
        return null;
    }
}
