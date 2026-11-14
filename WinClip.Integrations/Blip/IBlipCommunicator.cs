namespace WinClip.Integrations.Blip;

/// <summary>
/// Interface for communicating with the Blip note-taking application via IPC.
/// </summary>
public interface IBlipCommunicator
{
    /// <summary>
    /// Sends a note creation request to Blip via Named Pipes.
    /// </summary>
    /// <param name="request">The note request containing text and optional metadata.</param>
    /// <returns>True if the note was successfully sent, false otherwise.</returns>
    Task<bool> SendNoteAsync(BlipNoteRequest request);

    /// <summary>
    /// Checks if the Blip IPC pipe is available and accepting connections.
    /// </summary>
    /// <returns>True if the pipe is available, false otherwise.</returns>
    bool IsPipeAvailable();

    /// <summary>
    /// Launches Blip application if it's not already running and waits for it to be ready.
    /// </summary>
    /// <returns>True if Blip was successfully launched and is ready, false otherwise.</returns>
    Task<bool> LaunchBlipAsync();
}
