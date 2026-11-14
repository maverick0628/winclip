using Microsoft.Extensions.Logging;
using WinClip.Models;
using WinClip.Native;

namespace WinClip.Services;

public sealed class SelectionMonitorService : ISelectionMonitorService, IDisposable
{
    private readonly ILogger<SelectionMonitorService> _logger;
    private readonly HookManager _hookManager;
    private readonly SelectionDetector _selectionDetector = new();
    private readonly object _lockObject = new();
    private HeldModifiers _modifiersAtDown;
    private CursorKind _cursorAtDown;
    private HashSet<IntPtr> _windowsAtDown = new();
    private IntPtr _rootAtDown;
    private System.Threading.Timer? _watchdogTimer;
    private bool _isRunning;
    private bool _disposed;

    /// <summary>
    /// How long the hook can go without receiving any callback before the watchdog reinstalls it.
    /// A desktop user generates mouse move events almost continuously, so 30 seconds of silence
    /// is a strong signal that the hook has been silently removed by Windows.
    /// </summary>
    private static readonly TimeSpan WatchdogTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(10);

    public event EventHandler<SelectionEvent>? SelectionDetected;

    public bool IsRunning
    {
        get { lock (_lockObject) { return _isRunning; } }
    }

    public SelectionMonitorService(
        ILogger<SelectionMonitorService> logger,
        ILoggerFactory? loggerFactory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hookManager = new HookManager(loggerFactory?.CreateLogger<HookManager>());
        _hookManager.MouseEvent += OnMouseEvent;
    }

    public void Start()
    {
        lock (_lockObject)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SelectionMonitorService));
            if (_isRunning) throw new InvalidOperationException("Already running.");

            _logger.LogInformation("Starting selection monitor service...");
            _hookManager.Install();
            _isRunning = true;
            StartWatchdog();
            _logger.LogInformation("Selection monitor started — listening for double-click and drag-select.");
        }
    }

    public void Stop()
    {
        lock (_lockObject)
        {
            if (_disposed) return;
            if (!_isRunning) throw new InvalidOperationException("Not running.");

            _logger.LogInformation("Stopping selection monitor service...");
            StopWatchdog();
            _hookManager.Uninstall();
            _isRunning = false;
            _logger.LogInformation("Selection monitor service stopped.");
        }
    }

    // ── Watchdog ────────────────────────────────────────────────────────

    private void StartWatchdog()
    {
        _watchdogTimer?.Dispose();
        _watchdogTimer = new System.Threading.Timer(
            WatchdogTick, null, WatchdogInterval, WatchdogInterval);
    }

    private void StopWatchdog()
    {
        _watchdogTimer?.Dispose();
        _watchdogTimer = null;
    }

    private void WatchdogTick(object? state)
    {
        try
        {
            lock (_lockObject)
            {
                if (!_isRunning || _disposed) return;
            }

            var lastCallback = _hookManager.LastCallbackTime;
            if (lastCallback == null)
            {
                // Hook was just installed, hasn't received a callback yet — give it more time
                return;
            }

            var silence = DateTime.UtcNow - lastCallback.Value;
            if (silence > WatchdogTimeout)
            {
                _logger.LogWarning(
                    "Hook watchdog: no callback for {Seconds:F0}s — reinstalling hook on UI thread.",
                    silence.TotalSeconds);

                // CRITICAL: Low-level hooks MUST be installed on a thread with a message pump.
                // System.Threading.Timer fires on a threadpool thread which has no message pump,
                // so hooks installed there will never receive callbacks.
                // Dispatch to the WPF Dispatcher (UI thread) to ensure the hook works.
                if (System.Windows.Application.Current?.Dispatcher is { } dispatcher)
                {
                    dispatcher.BeginInvoke(() =>
                    {
                        try { _hookManager.Reinstall(); }
                        catch (Exception ex) { _logger.LogError(ex, "Hook reinstall failed."); }
                    });
                }
                else
                {
                    // Fallback if no dispatcher available (shouldn't happen in normal operation)
                    _logger.LogError("No WPF Dispatcher available for hook reinstall.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hook watchdog error.");
        }
    }

    // ── Mouse event handler ─────────────────────────────────────────────

    private void OnMouseEvent(object? sender, MouseHookEventArgs e)
    {
        try
        {
            if (e.IsLeftButtonDown)
            {
                var now = DateTime.UtcNow;
                _modifiersAtDown = InputStateReader.ReadModifiers();
                _cursorAtDown = InputStateReader.ReadCursor();
                // A double-click is compared from its first mouse-down: a menu or dropdown the
                // first click opened must count as a window that appeared.
                if (!_selectionDetector.IsSecondClick(e.HookStruct.pt.x, e.HookStruct.pt.y, now))
                {
                    _windowsAtDown = WindowSnapshot.VisibleTopLevelWindows();
                    _rootAtDown = WindowSnapshot.RootWindowAt(e.HookStruct.pt.x, e.HookStruct.pt.y);
                }
                _selectionDetector.OnMouseDown(e.HookStruct.pt.x, e.HookStruct.pt.y, now);
                return;
            }

            if (!e.IsLeftButtonUp)
                return;

            var gesture = _selectionDetector.OnMouseUp(
                e.HookStruct.pt.x, e.HookStruct.pt.y, DateTime.UtcNow);

            if (gesture == SelectionGesture.None)
                return; // Single click — no action

            _logger.LogDebug("{Gesture} detected at ({X},{Y})",
                gesture, e.HookStruct.pt.x, e.HookStruct.pt.y);

            SelectionDetected?.Invoke(this, new SelectionEvent(
                e.HookStruct.pt.x, e.HookStruct.pt.y,
                gesture)
            {
                ModifiersAtDown = _modifiersAtDown,
                ModifiersAtUp = InputStateReader.ReadModifiers(),
                CursorAtDown = _cursorAtDown,
                CursorAtUp = InputStateReader.ReadCursor(),
                WindowChangeReason = ReadWindowChange(e.HookStruct.pt.x, e.HookStruct.pt.y),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mouse event.");
        }
    }

    private string? ReadWindowChange(int x, int y)
    {
        try
        {
            var rootAtUp = WindowSnapshot.RootWindowAt(x, y);
            return ClickWindowFilter.RejectReason(
                _windowsAtDown,
                _rootAtDown,
                rootAtUp,
                WindowSnapshot.IsVisible(_rootAtDown),
                WindowSnapshot.BelongsToProcess(rootAtUp, Environment.ProcessId));
        }
        catch
        {
            return null;
        }
    }

    // ── Disposal ────────────────────────────────────────────────────────

    public void Dispose()
    {
        lock (_lockObject)
        {
            if (_disposed) return;
            StopWatchdog();
            if (_isRunning && _hookManager != null)
            {
                try { _hookManager.Uninstall(); _isRunning = false; }
                catch (Exception ex) { _logger?.LogError(ex, "Error during disposal."); }
            }
            if (_hookManager != null)
            {
                _hookManager.MouseEvent -= OnMouseEvent;
                _hookManager.Dispose();
            }
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~SelectionMonitorService() { Dispose(); }
}
