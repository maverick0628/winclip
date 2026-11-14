using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WinClip.Models;
using WinClip.Services;
using Xunit;

namespace WinClip.Tests.Services;

public class SelectionMonitorServiceTests
{
    private static SelectionMonitorService CreateService()
    {
        var logger = NullLogger<SelectionMonitorService>.Instance;
        return new SelectionMonitorService(logger);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        var service = CreateService();

        Assert.False(service.IsRunning);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SelectionMonitorService(null!));
    }

    [Fact]
    public void Stop_WhenNotRunning_ThrowsInvalidOperation()
    {
        var service = CreateService();

        Assert.Throws<InvalidOperationException>(() => service.Stop());
    }

    [Fact]
    public void Dispose_WhenNotRunning_DoesNotThrow()
    {
        var service = CreateService();

        service.Dispose();

        // Double dispose should also be safe
        service.Dispose();
    }

    [Fact]
    public void Start_WhenDisposed_ThrowsObjectDisposed()
    {
        var service = CreateService();
        service.Dispose();

        Assert.Throws<ObjectDisposedException>(() => service.Start());
    }

    [Fact]
    public void SelectionDetected_CanSubscribeAndUnsubscribe()
    {
        var service = CreateService();
        int callCount = 0;
        EventHandler<SelectionEvent> handler = (_, _) => callCount++;

        service.SelectionDetected += handler;
        service.SelectionDetected -= handler;

        // No exception means the event wiring works
        Assert.Equal(0, callCount);
    }
}
