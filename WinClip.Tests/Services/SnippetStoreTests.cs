using System;
using System.IO;
using WinClip.Actions;
using Xunit;

namespace WinClip.Tests.Services;

/// <summary>
/// Unit tests for the SnippetStore class.
/// </summary>
public class SnippetStoreTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly SnippetStore _store;

    public SnippetStoreTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"winclip_snippet_test_{Guid.NewGuid()}.json");
        _store = new SnippetStore(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public void Save_NewSnippet_Persists()
    {
        // Arrange
        var name = "greeting";
        var text = "Hello, world!";

        // Act
        _store.Add(name, text);

        // Assert
        var snippet = _store.Get(name);
        Assert.NotNull(snippet);
        Assert.Equal(name, snippet.Name);
        Assert.Equal(text, snippet.Text);
        Assert.Equal(1, _store.Count);
        Assert.True(File.Exists(_testFilePath));
    }

    [Fact]
    public void Save_DuplicateName_Overwrites()
    {
        // Arrange
        var name = "greeting";
        var originalText = "Hello, world!";
        var updatedText = "Hi there!";

        // Act
        _store.Add(name, originalText);
        _store.Add(name, updatedText);

        // Assert
        Assert.Equal(1, _store.Count);
        var snippet = _store.Get(name);
        Assert.NotNull(snippet);
        Assert.Equal(updatedText, snippet.Text);
    }

    [Fact]
    public void Load_EmptyFile_ReturnsEmptyList()
    {
        // Arrange — create an empty file before constructing the store
        File.WriteAllText(_testFilePath, string.Empty);

        // Act
        var store = new SnippetStore(_testFilePath);

        // Assert
        Assert.Equal(0, store.Count);
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Load_CorruptedFile_HandlesGracefully()
    {
        // Arrange — write garbage text to the file
        File.WriteAllText(_testFilePath, "this is not valid json {{[!@#$%");

        // Act — should not throw
        var store = new SnippetStore(_testFilePath);

        // Assert
        Assert.Equal(0, store.Count);
        Assert.Empty(store.GetAll());
    }

    [Fact]
    public void Delete_ExistingSnippet_Removes()
    {
        // Arrange
        _store.Add("to-delete", "some text");
        Assert.Equal(1, _store.Count);

        // Act
        var removed = _store.Remove("to-delete");

        // Assert
        Assert.True(removed);
        Assert.Equal(0, _store.Count);
        Assert.Null(_store.Get("to-delete"));
    }

    [Fact]
    public void Delete_NonExistent_DoesNotThrow()
    {
        // Act
        var removed = _store.Remove("does-not-exist");

        // Assert
        Assert.False(removed);
        Assert.Equal(0, _store.Count);
    }

    [Fact]
    public void GetAll_ReturnsSavedSnippets()
    {
        // Arrange
        _store.Add("first", "text one");
        _store.Add("second", "text two");
        _store.Add("third", "text three");

        // Act
        var all = _store.GetAll();

        // Assert
        Assert.Equal(3, all.Count);
        Assert.Contains(all, s => s.Name == "first" && s.Text == "text one");
        Assert.Contains(all, s => s.Name == "second" && s.Text == "text two");
        Assert.Contains(all, s => s.Name == "third" && s.Text == "text three");
    }

    [Fact]
    public void Persistence_SurvivesRestart()
    {
        // Arrange — add snippets via the first store instance
        _store.Add("persistent", "I survive restarts");
        _store.Add("also-persistent", "Me too");

        // Act — create a new store instance pointing to the same file
        var newStore = new SnippetStore(_testFilePath);

        // Assert
        Assert.Equal(2, newStore.Count);

        var snippet1 = newStore.Get("persistent");
        Assert.NotNull(snippet1);
        Assert.Equal("I survive restarts", snippet1.Text);

        var snippet2 = newStore.Get("also-persistent");
        Assert.NotNull(snippet2);
        Assert.Equal("Me too", snippet2.Text);
    }
}
