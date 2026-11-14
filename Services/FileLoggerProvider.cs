using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace WinClip.Services;

/// <summary>
/// Minimal file-based logger so diagnostics are visible when running as a tray app (no console).
/// Logs are written to %APPDATA%\WinClip\winclip.log, rotating when the file exceeds 2 MB.
///
/// If the log file becomes unwritable (disk full, locked by antivirus, permission denied),
/// the logger falls back to the Windows Event Log so critical diagnostics are still captured
/// instead of vanishing into <see cref="Debug.WriteLine"/> (which no tray user ever reads).
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB
    private int _writeCount;
    private const int RotationCheckInterval = 100;

    // Event log fallback — activated when file I/O fails repeatedly so we don't
    // spam the event log on transient errors.
    private int _consecutiveFileFailures;
    private const int FailureThresholdBeforeEventLog = 3;
    private static readonly object EventLogLock = new();
    private static bool _eventSourceChecked;
    private const string EventLogSource = "WinClip";
    private const string EventLogName = "Application";

    public FileLoggerProvider()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WinClip");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, "winclip.log");
        RotateIfNeeded();
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, this);

    public void Dispose() { }

    internal void WriteLog(string message)
    {
        lock (_lock)
        {
            try
            {
                if (++_writeCount % RotationCheckInterval == 0)
                    RotateIfNeeded();
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
                _consecutiveFileFailures = 0;
            }
            catch (Exception ex)
            {
                _consecutiveFileFailures++;
                Debug.WriteLine($"WinClip file logger failed: {ex.Message}");

                // After repeated failures, fall back to the Windows Event Log so
                // diagnostics are still reachable. This is a last-resort channel —
                // we only emit a summary, not every dropped line.
                if (_consecutiveFileFailures == FailureThresholdBeforeEventLog)
                {
                    TryWriteEventLog(
                        $"WinClip file logger has failed {FailureThresholdBeforeEventLog} consecutive writes. "
                        + $"Last error: {ex.Message}. Log path: {_logFilePath}. "
                        + "Subsequent file-logger errors will be suppressed until writes succeed again.",
                        EventLogEntryType.Warning);
                }
            }
        }
    }

    private static void TryWriteEventLog(string message, EventLogEntryType type)
    {
        // Event source creation requires admin on first write; we probe once and
        // suppress further attempts if the source can't be created. Writing to an
        // existing source never requires admin.
        lock (EventLogLock)
        {
            try
            {
                if (!_eventSourceChecked)
                {
                    _eventSourceChecked = true;
                    if (!EventLog.SourceExists(EventLogSource))
                    {
                        // Source creation needs admin; skip silently and fall through
                        // to writing under the generic Application source.
                    }
                }

                var sourceToUse = EventLog.SourceExists(EventLogSource) ? EventLogSource : EventLogName;
                EventLog.WriteEntry(sourceToUse, message, type);
            }
            catch
            {
                // Event log also unavailable — genuine last resort; nothing we can do.
                Debug.WriteLine("WinClip logger: event log fallback also failed.");
            }
        }
    }

    private void RotateIfNeeded()
    {
        try
        {
            if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > MaxFileSizeBytes)
            {
                var backup = _logFilePath + ".old";
                if (File.Exists(backup)) File.Delete(backup);
                File.Move(_logFilePath, backup);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"WinClip file logger rotation failed: {ex.Message}");
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly string _category;
        private readonly FileLoggerProvider _provider;

        public FileLogger(string category, FileLoggerProvider provider)
        {
            // Shorten "WinClip.Services.SelectionMonitorService" → "SelectionMonitorService"
            var dot = category.LastIndexOf('.');
            _category = dot >= 0 ? category[(dot + 1)..] : category;
            _provider = provider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var level = logLevel switch
            {
                LogLevel.Trace => "TRC",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "CRT",
                _ => "???"
            };

            var line = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {_category}: {formatter(state, exception)}";
            if (exception != null)
                line += Environment.NewLine + exception;

            _provider.WriteLog(line);
        }
    }
}
