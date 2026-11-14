using System.Diagnostics;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace WinClip.Integrations.Blip;

/// <summary>
/// Communicates with Blip note-taking application via Named Pipes IPC.
/// Sends note creation requests and handles connection management.
/// </summary>
public class BlipCommunicator : IBlipCommunicator
{
    private const string PipeName = "blip-integration";
    private const int ConnectionTimeoutMs = 2000;
    private const int SendTimeoutMs = 2000;
    private const int LaunchWaitMs = 3000;
    private const int PipeCheckIntervalMs = 300;

    private readonly IBlipDetector _detector;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlipCommunicator"/> class.
    /// </summary>
    /// <param name="detector">The Blip detector for checking installation and runtime status.</param>
    public BlipCommunicator(IBlipDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    /// <summary>
    /// Sends a note creation request to Blip via Named Pipes.
    /// </summary>
    /// <param name="request">The note request containing text and optional metadata.</param>
    /// <returns>True if the note was successfully sent, false otherwise.</returns>
    public async Task<bool> SendNoteAsync(BlipNoteRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new ArgumentException("Note text cannot be empty.", nameof(request));
        }

        try
        {
            using var pipeClient = new NamedPipeClientStream(
                serverName: ".",
                pipeName: PipeName,
                direction: PipeDirection.Out);

            // Attempt to connect to the pipe
            await pipeClient.ConnectAsync(ConnectionTimeoutMs);

            // Serialize the request to JSON
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var data = Encoding.UTF8.GetBytes(json);

            // Send the data with timeout
            using var cts = new CancellationTokenSource(SendTimeoutMs);
            await pipeClient.WriteAsync(data, cts.Token);
            await pipeClient.FlushAsync(cts.Token);

            return true;
        }
        catch (TimeoutException)
        {
            // Blip not responding
            return false;
        }
        catch (IOException)
        {
            // Pipe connection failed
            return false;
        }
        catch (OperationCanceledException)
        {
            // Send operation timed out
            return false;
        }
        catch
        {
            // Other errors (serialization, etc.)
            return false;
        }
    }

    /// <summary>
    /// Checks if the Blip IPC pipe is available and accepting connections.
    /// </summary>
    /// <returns>True if the pipe is available, false otherwise.</returns>
    public bool IsPipeAvailable()
    {
        try
        {
            using var pipeClient = new NamedPipeClientStream(
                serverName: ".",
                pipeName: PipeName,
                direction: PipeDirection.Out);

            // Try to connect with a very short timeout (just checking availability)
            pipeClient.Connect(500);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Launches Blip application if it's not already running and waits for it to be ready.
    /// Waits up to 3 seconds for Blip to initialize and accept IPC connections.
    /// </summary>
    /// <returns>True if Blip was successfully launched and is ready, false otherwise.</returns>
    public async Task<bool> LaunchBlipAsync()
    {
        try
        {
            // If Blip process is already running, just wait for the pipe
            if (!_detector.IsBlipRunning())
            {
                var executablePath = _detector.GetBlipExecutablePath();
                if (string.IsNullOrEmpty(executablePath))
                    return false;

                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    CreateNoWindow = false
                });
            }

            // Poll for pipe availability using async connect
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < LaunchWaitMs)
            {
                await Task.Delay(PipeCheckIntervalMs);

                try
                {
                    using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    await pipe.ConnectAsync(300);
                    return true;
                }
                catch
                {
                    // Pipe not ready yet — keep waiting
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
