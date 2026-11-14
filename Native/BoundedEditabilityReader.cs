namespace WinClip.Native;

internal sealed class BoundedEditabilityReader
{
    private int _inFlight;
    internal bool IsIdle => Volatile.Read(ref _inFlight) == 0;

    internal async Task<bool?> ReadAsync(Func<bool> read, TimeSpan budget, Func<bool>? isCurrent = null)
    {
        if (Interlocked.CompareExchange(ref _inFlight, 1, 0) != 0) return null;
        var completion = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            bool? result = null;
            try { result = read(); }
            catch { }
            finally
            {
                Volatile.Write(ref _inFlight, 0);
                completion.TrySetResult(result);
            }
        }) { IsBackground = true, Name = "WinClip editability read" };
        thread.SetApartmentState(ApartmentState.MTA);
        try { thread.Start(); }
        catch
        {
            Volatile.Write(ref _inFlight, 0);
            return null;
        }
        using var timeout = new CancellationTokenSource();
        var delay = Task.Delay(budget, timeout.Token);
        if (await Task.WhenAny(completion.Task, delay) != completion.Task) return null;
        await timeout.CancelAsync();
        var result = await completion.Task;
        return isCurrent == null || isCurrent() ? result : null;
    }
}
