using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;

namespace WinClip.Services;

/// <summary>
/// Registers a system-wide hotkey (default: Ctrl+Shift+C) using the Win32 RegisterHotKey API.
/// Receives WM_HOTKEY via a hidden message-only window on the UI thread.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID_TOGGLE = 1;
    private const uint MOD_NOREPEAT = 0x4000;

    private readonly ILogger<GlobalHotkeyService>? _logger;
    private HwndSource? _hwndSource;
    private bool _registered;
    private bool _disposed;

    /// <summary>
    /// Fires when the registered hotkey is pressed.
    /// </summary>
    public event Action? HotkeyPressed;

    public GlobalHotkeyService(ILogger<GlobalHotkeyService>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers a global hotkey with the specified modifiers and virtual key code.
    /// Must be called from the WPF UI thread.
    /// Returns true if registration succeeded, false if the key combination is taken.
    /// </summary>
    /// <param name="modifiers">Win32 modifier flags (MOD_CONTROL=0x2, MOD_ALT=0x1, MOD_SHIFT=0x4, MOD_WIN=0x8).</param>
    /// <param name="virtualKey">Win32 virtual key code.</param>
    public bool Register(uint modifiers, uint virtualKey)
    {
        // Unregister any previous hotkey first
        if (_registered)
            Unregister();

        // Create a message-only window to receive WM_HOTKEY
        if (_hwndSource == null)
        {
            var parameters = new HwndSourceParameters("WinClipHotkey")
            {
                Width = 0,
                Height = 0,
                WindowStyle = 0, // not visible
            };
            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(WndProc);
        }

        bool success = RegisterHotKey(_hwndSource.Handle, HOTKEY_ID_TOGGLE,
            modifiers | MOD_NOREPEAT, virtualKey);

        if (success)
        {
            _registered = true;
            _logger?.LogInformation("Global hotkey registered (modifiers=0x{Modifiers:X}, vk=0x{VK:X}).",
                modifiers, virtualKey);
            return true;
        }
        else
        {
            int error = Marshal.GetLastWin32Error();
            _logger?.LogWarning("Failed to register hotkey (modifiers=0x{Modifiers:X}, vk=0x{VK:X}, error {Error}). " +
                "Another application may have claimed this key combination.", modifiers, virtualKey, error);
            return false;
        }
    }

    /// <summary>
    /// Unregisters the global hotkey.
    /// </summary>
    public void Unregister()
    {
        if (!_registered || _hwndSource == null) return;

        UnregisterHotKey(_hwndSource.Handle, HOTKEY_ID_TOGGLE);
        _registered = false;
        _logger?.LogInformation("Global hotkey unregistered.");
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID_TOGGLE)
        {
            _logger?.LogDebug("Global hotkey pressed.");
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Unregister();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
