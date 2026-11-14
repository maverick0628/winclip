using WinClip.Services;

namespace WinClip.Tests.Services;

public class ClipboardPreserverTests
{
    [Theory]
    [InlineData("ExcludeClipboardContentFromMonitorProcessing")]
    [InlineData("Clipboard Viewer Ignore")]
    [InlineData("clipboard viewer ignore")]
    public void ConcealedMarker_IsDetected(string marker)
    {
        Assert.True(ClipboardPreserver.IsConcealed(["UnicodeText", marker]));
    }

    [Fact]
    public void OrdinaryFormats_AreNotConcealed()
    {
        Assert.False(ClipboardPreserver.IsConcealed(["UnicodeText", "HTML Format", "Rich Text Format"]));
    }

    [Fact]
    public void Restore_WhenCopyLandedAndNothingChangedSince()
    {
        Assert.True(ClipboardPreserver.ShouldRestore(sequenceBefore: 10, sequenceAtRead: 12, sequenceNow: 12));
    }

    [Fact]
    public void NoRestore_WhenTheCopyNeverChangedTheClipboard()
    {
        Assert.False(ClipboardPreserver.ShouldRestore(sequenceBefore: 10, sequenceAtRead: 10, sequenceNow: 10));
    }

    [Fact]
    public void NoRestore_WhenSomethingCopiedAfterTheRead()
    {
        // The user or the app wrote again; their content wins over the old snapshot.
        Assert.False(ClipboardPreserver.ShouldRestore(sequenceBefore: 10, sequenceAtRead: 12, sequenceNow: 13));
    }

    [Theory]
    [InlineData(2u, false, true)]      // CF_BITMAP: a GDI handle, synthesized from CF_DIB
    [InlineData(3u, false, true)]      // CF_METAFILEPICT: holds a metafile handle
    [InlineData(1u, true, true)]       // CF_TEXT is synthesized from Unicode text
    [InlineData(1u, false, false)]     // ...unless it is the only text
    [InlineData(8u, false, false)]     // CF_DIB is plain bytes
    [InlineData(14u, false, false)]    // CF_ENHMETAFILE is copied through its bits
    [InlineData(0x300u, false, true)]  // private GDI object range
    [InlineData(0xC123u, false, false)] // registered formats are HGLOBAL bytes
    public void HandleAndSynthesizedFormats_AreSkipped(uint format, bool hasUnicodeText, bool skipped)
    {
        Assert.Equal(skipped, ClipboardPreserver.IsHandleOrSynthesized(format, hasUnicodeText));
    }

    [Theory]
    [InlineData("DataObject")]
    [InlineData("Ole Private Data")]
    [InlineData("OleClipboardPersistOnFlush")]
    public void OleBookkeepingFormats_AreListed(string name)
    {
        Assert.Contains(name, ClipboardPreserver.OleBookkeepingFormats);
    }

    [Fact]
    public void SaveAndRestore_RoundTripsTextAndHtml()
    {
        // The clipboard is shared with other test assemblies running in parallel, so a
        // round trip that another writer interrupted is retried.
        Exception? failure = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            failure = RunOnSta(RoundTrip);
            if (failure == null) return;
            Thread.Sleep(200);
        }
        System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure!).Throw();
    }

    private static Exception? RunOnSta(Action body)
    {
        // WPF's clipboard calls need an STA thread; xUnit's runner threads are MTA.
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return failure;
    }

    private static void RoundTrip()
    {
        var original = new System.Windows.DataObject();
        original.SetData(System.Windows.DataFormats.UnicodeText, "kept text");
        original.SetData(System.Windows.DataFormats.Html, "<b>kept</b>");
        System.Windows.Clipboard.SetDataObject(original, copy: true);

        var snapshot = ClipboardPreserver.Save();
        Assert.NotNull(snapshot);
        Assert.Contains(snapshot!.Items, i => i.Name == "HTML Format");
        // WPF copies through OLE; its bookkeeping must not be restored
        Assert.DoesNotContain(snapshot.Items, i => ClipboardPreserver.OleBookkeepingFormats.Contains(i.Name));

        System.Windows.Clipboard.SetText("transient");
        Assert.True(ClipboardPreserver.Restore(snapshot));

        Assert.Equal("kept text", System.Windows.Clipboard.GetText());
        var formats = System.Windows.Clipboard.GetDataObject()!.GetFormats(false);
        Assert.Contains(System.Windows.DataFormats.Html, formats);
        Assert.Contains("CanIncludeInClipboardHistory", formats);

        // A restored clipboard must not look concealed, or the next action couldn't save it.
        Assert.NotNull(ClipboardPreserver.Save());
    }
}
