using Moq;
using WinClip.Actions;
using WinClip.Core.Services;
using Xunit;

namespace WinClip.Tests.Services;

public class ActionRegistryTests
{
    private readonly ActionRegistry _registry = new();

    private static IAction CreateMockAction(string name)
    {
        var mock = new Mock<IAction>();
        mock.Setup(a => a.Name).Returns(name);
        mock.Setup(a => a.Description).Returns($"{name} description");
        mock.Setup(a => a.IconName).Returns(name);
        return mock.Object;
    }

    // --- RegisterAction ---

    [Fact]
    public void RegisterAction_ValidAction_Succeeds()
    {
        var action = CreateMockAction("Copy");

        _registry.RegisterAction(action);

        Assert.Same(action, _registry.GetAction("Copy"));
    }

    [Fact]
    public void RegisterAction_NullAction_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => _registry.RegisterAction(null!));
    }

    [Fact]
    public void RegisterAction_EmptyName_ThrowsArgumentException()
    {
        var action = CreateMockAction("");

        Assert.Throws<ArgumentException>(() => _registry.RegisterAction(action));
    }

    [Fact]
    public void RegisterAction_WhitespaceName_ThrowsArgumentException()
    {
        var action = CreateMockAction("   ");

        Assert.Throws<ArgumentException>(() => _registry.RegisterAction(action));
    }

    [Fact]
    public void RegisterAction_DuplicateName_ThrowsInvalidOperation()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));

        Assert.Throws<InvalidOperationException>(() =>
            _registry.RegisterAction(CreateMockAction("Copy")));
    }

    // --- GetAction ---

    [Fact]
    public void GetAction_ExistingName_ReturnsAction()
    {
        var action = CreateMockAction("Search");
        _registry.RegisterAction(action);

        Assert.Same(action, _registry.GetAction("Search"));
    }

    [Fact]
    public void GetAction_UnknownName_ReturnsNull()
    {
        Assert.Null(_registry.GetAction("NonExistent"));
    }

    [Fact]
    public void GetAction_NullName_ReturnsNull()
    {
        Assert.Null(_registry.GetAction(null!));
    }

    [Fact]
    public void GetAction_EmptyName_ReturnsNull()
    {
        Assert.Null(_registry.GetAction(""));
    }

    [Fact]
    public void GetAction_WhitespaceName_ReturnsNull()
    {
        Assert.Null(_registry.GetAction("   "));
    }

    // --- GetAllActions ---

    [Fact]
    public void GetAllActions_Empty_ReturnsEmptyCollection()
    {
        var all = _registry.GetAllActions();

        Assert.Empty(all);
    }

    [Fact]
    public void GetAllActions_MultipleRegistered_ReturnsAll()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));
        _registry.RegisterAction(CreateMockAction("Search"));
        _registry.RegisterAction(CreateMockAction("Translate"));

        var all = _registry.GetAllActions();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void GetAllActions_CachedUntilModified()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));

        var first = _registry.GetAllActions();
        var second = _registry.GetAllActions();

        // Same reference — cached
        Assert.Same(first, second);
    }

    [Fact]
    public void GetAllActions_CacheInvalidatedOnRegister()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));
        var before = _registry.GetAllActions();

        _registry.RegisterAction(CreateMockAction("Search"));
        var after = _registry.GetAllActions();

        Assert.NotSame(before, after);
        Assert.Equal(2, after.Count);
    }

    // --- UnregisterAction ---

    [Fact]
    public void UnregisterAction_ExistingAction_ReturnsTrue()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));

        Assert.True(_registry.UnregisterAction("Copy"));
        Assert.Null(_registry.GetAction("Copy"));
    }

    [Fact]
    public void UnregisterAction_UnknownAction_ReturnsFalse()
    {
        Assert.False(_registry.UnregisterAction("NonExistent"));
    }

    [Fact]
    public void UnregisterAction_NullName_ReturnsFalse()
    {
        Assert.False(_registry.UnregisterAction(null!));
    }

    [Fact]
    public void UnregisterAction_EmptyName_ReturnsFalse()
    {
        Assert.False(_registry.UnregisterAction(""));
    }

    [Fact]
    public void UnregisterAction_CacheInvalidated()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));
        _registry.RegisterAction(CreateMockAction("Search"));
        var before = _registry.GetAllActions();

        _registry.UnregisterAction("Copy");
        var after = _registry.GetAllActions();

        Assert.NotSame(before, after);
        Assert.Single(after);
    }

    // --- Thread safety ---

    [Fact]
    public void RegisterAction_DuringCacheRebuild_IsNotLostFromCache()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));
        var search = CreateMockAction("Search");

        var rebuilt = GetAllActionsWhileWriting(
            () => _registry.RegisterAction(search),
            () => _registry.GetAction("Search") != null);

        Assert.Single(rebuilt);
        Assert.Equal(2, _registry.GetAllActions().Count);
    }

    [Fact]
    public void UnregisterAction_DuringCacheRebuild_IsNotLostFromCache()
    {
        _registry.RegisterAction(CreateMockAction("Copy"));
        _registry.RegisterAction(CreateMockAction("Search"));

        var rebuilt = GetAllActionsWhileWriting(
            () => _registry.UnregisterAction("Search"),
            () => _registry.GetAction("Search") == null);

        Assert.Equal(2, rebuilt.Count);
        Assert.Single(_registry.GetAllActions());
    }

    /// <summary>
    /// Rebuilds the cache and runs <paramref name="write"/> on another thread after the rebuild has
    /// read the actions but before it publishes them. The rebuild waits until the write has landed
    /// and the writer has either finished or blocked on the cache lock.
    /// </summary>
    private IReadOnlyCollection<IAction> GetAllActionsWhileWriting(Action write, Func<bool> writeLanded)
    {
        Exception? writeError = null;
        var writer = new Thread(() =>
        {
            try { write(); }
            catch (Exception ex) { writeError = ex; }
        })
        { IsBackground = true };

        _registry.AfterSnapshotForTests = () =>
        {
            _registry.AfterSnapshotForTests = null;
            writer.Start();
            var settled = SpinWait.SpinUntil(
                () => writeLanded()
                    && (writer.ThreadState & (ThreadState.WaitSleepJoin | ThreadState.Stopped)) != 0,
                TimeSpan.FromSeconds(10));
            Assert.True(settled, "The write did not land, or the writer neither finished nor blocked on the cache lock.");
        };

        var rebuilt = _registry.GetAllActions();

        Assert.True(writer.Join(TimeSpan.FromSeconds(10)), "The writer did not finish after the rebuild released the cache lock.");
        Assert.Null(writeError);
        return rebuilt;
    }

    [Fact]
    public async Task ConcurrentRegistrations_DoNotCorrupt()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
            Task.Run(() => _registry.RegisterAction(CreateMockAction($"Action{i}")))
        ).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(100, _registry.GetAllActions().Count);
    }

    [Fact(Timeout = 30000)]
    public async Task ConcurrentGetAndRegister_DoNotThrow()
    {
        // Pre-populate
        for (int i = 0; i < 10; i++)
            _registry.RegisterAction(CreateMockAction($"Init{i}"));

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var all = _registry.GetAllActions();
                Assert.True(all.Count >= 10);
            }
        })).ToArray();

        var writers = Enumerable.Range(100, 50).Select(i => Task.Run(() =>
        {
            _registry.RegisterAction(CreateMockAction($"Action{i}"));
        })).ToArray();

        await Task.WhenAll(writers);
        cts.Cancel();
        await Task.WhenAll(readers);

        Assert.Equal(60, _registry.GetAllActions().Count);
    }
}
