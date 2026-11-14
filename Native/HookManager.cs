using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WinClip.Native;

/// <summary>
/// Managed wrapper for Windows low-level mouse hooks.
/// Provides thread-safe hook lifecycle management and prevents delegate garbage collection.
/// </summary>
internal sealed class HookManager : IDisposable
{
    private readonly object _lockObject = new();
    private readonly ILogger<HookManager> _logger;
    private IntPtr _hookHandle = IntPtr.Zero;
    private NativeMethods.HookProc? _hookProcDelegate;
    private bool _isInstalled;
    private bool _disposed;
    private long _lastCallbackTicks;

    /// <summary>
    /// Initializes a new instance of the <see cref="HookManager"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public HookManager(ILogger<HookManager>? logger = null)
    {
        _logger = logger ?? NullLogger<HookManager>.Instance;
    }

    /// <summary>
    /// Occurs when a mouse event is captured by the hook.
    /// </summary>
    public event EventHandler<MouseHookEventArgs>? MouseEvent;

    /// <summary>
    /// Gets a value indicating whether the hook is currently installed.
    /// </summary>
    public bool IsInstalled
    {
        get
        {
            lock (_lockObject)
            {
                return _isInstalled;
            }
        }
    }

    /// <summary>
    /// Installs the low-level mouse hook.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the hook is already installed.</exception>
    /// <exception cref="Win32Exception">Thrown if hook installation fails.</exception>
    public void Install()
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HookManager));
            }

            if (_isInstalled)
            {
                throw new InvalidOperationException("Hook is already installed.");
            }

            // Store the delegate as a field to prevent garbage collection
            _hookProcDelegate = HookCallback;

            // Use LoadLibrary("user32.dll") for a reliable module handle.
            // In .NET 8, GetModuleHandle(null) can return the coreclr handle
            // instead of the host executable, causing low-level hooks to
            // install successfully but never fire callbacks.
            var moduleHandle = NativeMethods.LoadLibrary("user32.dll");

            _hookHandle = NativeMethods.SetWindowsHookEx(
                HookTypes.WH_MOUSE_LL,
                _hookProcDelegate,
                moduleHandle,
                0);

            if (_hookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                _hookProcDelegate = null;
                throw new Win32Exception(errorCode, $"Failed to install mouse hook. Error code: {errorCode}");
            }

            _isInstalled = true;
            _logger.LogInformation("Mouse hook installed successfully (handle: {Handle})", _hookHandle);
        }
    }

    /// <summary>
    /// Uninstalls the low-level mouse hook.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the hook is not installed.</exception>
    public void Uninstall()
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                return; // Already disposed, nothing to do
            }

            if (!_isInstalled)
            {
                throw new InvalidOperationException("Hook is not installed.");
            }

            UninstallInternal();
        }
    }

    /// <summary>
    /// Internal method to uninstall the hook without throwing exceptions.
    /// Used by Dispose to ensure cleanup even if hook is not in expected state.
    /// </summary>
    private void UninstallInternal()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            bool success = NativeMethods.UnhookWindowsHookEx(_hookHandle);
            if (!success)
            {
                int errorCode = Marshal.GetLastWin32Error();
                // Log error but don't throw - we're cleaning up
                _logger.LogWarning("Failed to unhook. Error code: {ErrorCode}", errorCode);
            }

            _hookHandle = IntPtr.Zero;
        }

        _hookProcDelegate = null;
        _isInstalled = false;
    }

    /// <summary>
    /// Gets the DateTime (UTC) of the last callback received from the hook, or null if none received yet.
    /// Used by the watchdog to detect if the hook has gone silent.
    /// </summary>
    public DateTime? LastCallbackTime
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastCallbackTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    /// <summary>
    /// Reinstalls the hook. Used by the watchdog when the hook appears to have gone silent.
    /// </summary>
    public void Reinstall()
    {
        lock (_lockObject)
        {
            if (_disposed) return;
            _logger.LogWarning("Reinstalling mouse hook...");
            if (_isInstalled) UninstallInternal();
            _callbackCount = 0; // reset diagnostic counter
        }
        // Install outside inner lock scope (it takes its own lock)
        Install();
    }

    /// <summary>
    /// Hook callback procedure that processes mouse events.
    /// </summary>
    private int _callbackCount;

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Update heartbeat on EVERY callback (including WM_MOUSEMOVE) so
        // the watchdog knows the hook is alive even when the user isn't clicking.
        Interlocked.Exchange(ref _lastCallbackTicks, DateTime.UtcNow.Ticks);

        try
        {
            if (nCode >= HookTypes.HC_ACTION && lParam != IntPtr.Zero)
            {
                int message = wParam.ToInt32();

                // ── Performance: only process button events, skip WM_MOUSEMOVE ──
                // Mouse-move events fire hundreds of times per second and are never
                // needed by the selection detector. Filtering here keeps the hook
                // callback well under the ~200ms Windows timeout.
                if (message == HookTypes.WM_LBUTTONDOWN ||
                    message == HookTypes.WM_LBUTTONUP)
                {
                    var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

                    // Log first few callbacks to confirm hook is working
                    if (_callbackCount < 3 && message == HookTypes.WM_LBUTTONUP)
                    {
                        _callbackCount++;
                        _logger.LogInformation("Hook callback #{Count}: LButtonUp at ({X},{Y})",
                            _callbackCount, hookStruct.pt.x, hookStruct.pt.y);
                    }

                    // Raise event for subscribers
                    var eventArgs = new MouseHookEventArgs(message, hookStruct);
                    MouseEvent?.Invoke(this, eventArgs);
                }
            }
        }
        catch (Exception ex)
        {
            // Catch all exceptions to prevent them from propagating to Windows
            // This could destabilize the hook chain or cause the application to crash
            _logger.LogError(ex, "Exception in hook callback");
        }

        // Always call the next hook in the chain
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    /// <summary>
    /// Releases all resources used by the HookManager.
    /// </summary>
    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                return;
            }

            if (_isInstalled)
            {
                UninstallInternal();
            }

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to ensure hook is uninstalled if Dispose is not called.
    /// </summary>
    ~HookManager()
    {
        Dispose();
    }
}

/// <summary>
/// Event arguments for mouse hook events.
/// </summary>
internal sealed class MouseHookEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MouseHookEventArgs"/> class.
    /// </summary>
    /// <param name="message">The Windows message identifier.</param>
    /// <param name="hookStruct">The mouse hook structure containing event details.</param>
    public MouseHookEventArgs(int message, MSLLHOOKSTRUCT hookStruct)
    {
        Message = message;
        HookStruct = hookStruct;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the Windows message identifier (e.g., WM_LBUTTONUP).
    /// </summary>
    public int Message { get; }

    /// <summary>
    /// Gets the mouse hook structure containing event details.
    /// </summary>
    public MSLLHOOKSTRUCT HookStruct { get; }

    /// <summary>
    /// Gets the timestamp when the event was captured.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets a value indicating whether this is a left button up event.
    /// </summary>
    public bool IsLeftButtonUp => Message == HookTypes.WM_LBUTTONUP;

    /// <summary>
    /// Gets a value indicating whether this is a left button down event.
    /// </summary>
    public bool IsLeftButtonDown => Message == HookTypes.WM_LBUTTONDOWN;

    /// <summary>
    /// Gets a value indicating whether this is a right button up event.
    /// </summary>
    public bool IsRightButtonUp => Message == HookTypes.WM_RBUTTONUP;

    /// <summary>
    /// Gets a value indicating whether this is a right button down event.
    /// </summary>
    public bool IsRightButtonDown => Message == HookTypes.WM_RBUTTONDOWN;

    /// <summary>
    /// Gets a value indicating whether this is a mouse move event.
    /// </summary>
    public bool IsMouseMove => Message == HookTypes.WM_MOUSEMOVE;
}
