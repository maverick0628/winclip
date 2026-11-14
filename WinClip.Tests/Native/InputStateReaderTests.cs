using WinClip.Models;
using WinClip.Native;

namespace WinClip.Tests.Native;

public class InputStateReaderTests
{
    private static readonly IReadOnlyDictionary<IntPtr, CursorKind> Known = new Dictionary<IntPtr, CursorKind>
    {
        [new IntPtr(1)] = CursorKind.IBeam,
        [new IntPtr(2)] = CursorKind.Arrow,
        [new IntPtr(3)] = CursorKind.NonText,
    };

    [Fact]
    public void Classify_KnownSystemHandle_ReturnsItsKind()
    {
        Assert.Equal(CursorKind.IBeam, CursorClassifier.Classify(new IntPtr(1), Known));
        Assert.Equal(CursorKind.NonText, CursorClassifier.Classify(new IntPtr(3), Known));
    }

    [Fact]
    public void Classify_AppSpecificHandle_IsUnknown()
    {
        Assert.Equal(CursorKind.Unknown, CursorClassifier.Classify(new IntPtr(99), Known));
    }

    [Fact]
    public void Classify_NullHandle_IsUnknown()
    {
        Assert.Equal(CursorKind.Unknown, CursorClassifier.Classify(IntPtr.Zero, Known));
    }

    [Fact]
    public void LiveReads_DoNotThrow()
    {
        // Real Win32 calls: in CI there may be no visible cursor, so any value is acceptable.
        _ = InputStateReader.ReadModifiers();
        _ = InputStateReader.ReadCursor();
    }
}
