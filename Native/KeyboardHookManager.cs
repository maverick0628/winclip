using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinClip.Native;

/// <summary>
/// Managed wrapper for Windows low-level keyboard hooks.
/// Provides thread-safe hook lifecycle management and prevents delegate garbage collection.
/// </summary>
internal sealed class KeyboardHookManager : IDisposable
{
    private readonly object _lockObject = new();
    private IntPtr _hookHandle = IntPtr.Zero;
    private NativeMethods.HookProc? _hookProcDelegate;
    private bool _isInstalled;
    private bool _disposed;

    /// <summary>
    /// Occurs when a keyboard event is captured by the hook.
    /// </summary>
    public event EventHandler<KeyboardHookEventArgs>? KeyboardEvent;

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
    /// Installs the low-level keyboard hook.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the hook is already installed.</exception>
    /// <exception cref="Win32Exception">Thrown if hook installation fails.</exception>
    public void Install()
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KeyboardHookManager));
            }

            if (_isInstalled)
            {
                throw new InvalidOperationException("Hook is already installed.");
            }

            // Store the delegate as a field to prevent garbage collection
            _hookProcDelegate = HookCallback;

            // Install the hook
            _hookHandle = NativeMethods.SetWindowsHookEx(
                HookTypes.WH_KEYBOARD_LL,
                _hookProcDelegate,
                NativeMethods.GetModuleHandle(null),
                0);

            if (_hookHandle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                _hookProcDelegate = null;
                throw new Win32Exception(errorCode, $"Failed to install keyboard hook. Error code: {errorCode}");
            }

            _isInstalled = true;
        }
    }

    /// <summary>
    /// Uninstalls the low-level keyboard hook.
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
                System.Diagnostics.Debug.WriteLine($"Failed to unhook. Error code: {errorCode}");
            }

            _hookHandle = IntPtr.Zero;
        }

        _hookProcDelegate = null;
        _isInstalled = false;
    }

    /// <summary>
    /// Hook callback procedure that processes keyboard events.
    /// </summary>
    /// <param name="nCode">Hook code determining how to process the message.</param>
    /// <param name="wParam">Message identifier.</param>
    /// <param name="lParam">Pointer to KBDLLHOOKSTRUCT structure.</param>
    /// <returns>Result of CallNextHookEx.</returns>
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= HookTypes.HC_ACTION && lParam != IntPtr.Zero)
            {
                int message = wParam.ToInt32();
                var hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

                // Raise event for subscribers
                var eventArgs = new KeyboardHookEventArgs(message, hookStruct);
                KeyboardEvent?.Invoke(this, eventArgs);
            }
        }
        catch (Exception ex)
        {
            // Catch all exceptions to prevent them from propagating to Windows
            // This could destabilize the hook chain or cause the application to crash
            System.Diagnostics.Debug.WriteLine($"Exception in hook callback: {ex}");
        }

        // Always call the next hook in the chain
        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    /// <summary>
    /// Releases all resources used by the KeyboardHookManager.
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
    ~KeyboardHookManager()
    {
        Dispose();
    }
}

/// <summary>
/// Event arguments for keyboard hook events.
/// </summary>
internal sealed class KeyboardHookEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyboardHookEventArgs"/> class.
    /// </summary>
    /// <param name="message">The Windows message identifier.</param>
    /// <param name="hookStruct">The keyboard hook structure containing event details.</param>
    public KeyboardHookEventArgs(int message, KBDLLHOOKSTRUCT hookStruct)
    {
        Message = message;
        HookStruct = hookStruct;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the Windows message identifier (e.g., WM_KEYDOWN, WM_KEYUP).
    /// </summary>
    public int Message { get; }

    /// <summary>
    /// Gets the keyboard hook structure containing event details.
    /// </summary>
    public KBDLLHOOKSTRUCT HookStruct { get; }

    /// <summary>
    /// Gets the timestamp when the event was received.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the virtual key code of the key that triggered the event.
    /// </summary>
    public uint VirtualKeyCode => HookStruct.vkCode;

    /// <summary>
    /// Gets a value indicating whether the event is a key down event.
    /// </summary>
    public bool IsKeyDown => Message == HookTypes.WM_KEYDOWN || Message == HookTypes.WM_SYSKEYDOWN;

    /// <summary>
    /// Gets a value indicating whether the event is a key up event.
    /// </summary>
    public bool IsKeyUp => Message == HookTypes.WM_KEYUP || Message == HookTypes.WM_SYSKEYUP;
}
