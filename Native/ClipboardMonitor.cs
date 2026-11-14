using System.ComponentModel;
using System.Runtime.InteropServices;

namespace WinClip.Native;

/// <summary>
/// Managed wrapper for Windows clipboard monitoring.
/// Provides thread-safe lifecycle management for clipboard format listeners.
/// </summary>
internal sealed class ClipboardMonitor : IDisposable
{
    private readonly object _lockObject = new();
    private IntPtr _windowHandle = IntPtr.Zero;
    private bool _isInstalled;
    private bool _disposed;

    /// <summary>
    /// Occurs when the clipboard contents have changed.
    /// </summary>
    public event EventHandler<ClipboardChangedEventArgs>? ClipboardChanged;

    /// <summary>
    /// Gets a value indicating whether the clipboard listener is currently installed.
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
    /// Installs the clipboard format listener for the specified window.
    /// </summary>
    /// <param name="windowHandle">Handle to the window that will receive clipboard notifications.</param>
    /// <exception cref="ArgumentException">Thrown if windowHandle is IntPtr.Zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the listener is already installed.</exception>
    /// <exception cref="Win32Exception">Thrown if listener installation fails.</exception>
    public void Install(IntPtr windowHandle)
    {
        lock (_lockObject)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ClipboardMonitor));
            }

            if (windowHandle == IntPtr.Zero)
            {
                throw new ArgumentException("Window handle cannot be IntPtr.Zero.", nameof(windowHandle));
            }

            if (_isInstalled)
            {
                throw new InvalidOperationException("Clipboard listener is already installed.");
            }

            // Register the window to receive clipboard notifications
            bool success = NativeMethods.AddClipboardFormatListener(windowHandle);

            if (!success)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Win32Exception(errorCode, $"Failed to install clipboard listener. Error code: {errorCode}");
            }

            _windowHandle = windowHandle;
            _isInstalled = true;
        }
    }

    /// <summary>
    /// Uninstalls the clipboard format listener.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the listener is not installed.</exception>
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
                throw new InvalidOperationException("Clipboard listener is not installed.");
            }

            UninstallInternal();
        }
    }

    /// <summary>
    /// Internal method to uninstall the listener without throwing exceptions.
    /// Used by Dispose to ensure cleanup even if listener is not in expected state.
    /// </summary>
    private void UninstallInternal()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            bool success = NativeMethods.RemoveClipboardFormatListener(_windowHandle);
            if (!success)
            {
                int errorCode = Marshal.GetLastWin32Error();
                // Log error but don't throw - we're cleaning up
                System.Diagnostics.Debug.WriteLine($"Failed to remove clipboard listener. Error code: {errorCode}");
            }

            _windowHandle = IntPtr.Zero;
        }

        _isInstalled = false;
    }

    /// <summary>
    /// Notifies subscribers that the clipboard contents have changed.
    /// This method should be called from the window's WndProc when WM_CLIPBOARDUPDATE is received.
    /// </summary>
    public void OnClipboardUpdate()
    {
        try
        {
            var eventArgs = new ClipboardChangedEventArgs();
            ClipboardChanged?.Invoke(this, eventArgs);
        }
        catch (Exception ex)
        {
            // Catch all exceptions to prevent them from propagating
            // This could destabilize the message loop or cause the application to crash
            System.Diagnostics.Debug.WriteLine($"Exception in clipboard changed handler: {ex}");
        }
    }

    /// <summary>
    /// Releases all resources used by the ClipboardMonitor.
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
    /// Finalizer to ensure listener is uninstalled if Dispose is not called.
    /// </summary>
    ~ClipboardMonitor()
    {
        Dispose();
    }
}

/// <summary>
/// Event arguments for clipboard change events.
/// </summary>
internal sealed class ClipboardChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClipboardChangedEventArgs"/> class.
    /// </summary>
    public ClipboardChangedEventArgs()
    {
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the timestamp when the clipboard change was detected.
    /// </summary>
    public DateTime Timestamp { get; }
}
