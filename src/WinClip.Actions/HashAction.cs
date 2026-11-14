using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace WinClip.Actions;

/// <summary>
/// Action that generates a cryptographic hash of the selected text.
/// Supports MD5, SHA1, and SHA256 via the <see cref="HashAlgorithmKind"/> enum.
/// </summary>
public class HashAction : IAction
{
    private readonly HashAlgorithmKind _algorithm;

    public HashAction(HashAlgorithmKind algorithm)
    {
        _algorithm = algorithm;
    }

    public string Name => _algorithm switch
    {
        HashAlgorithmKind.MD5 => "Hash MD5",
        HashAlgorithmKind.SHA1 => "Hash SHA1",
        HashAlgorithmKind.SHA256 => "Hash SHA256",
        _ => "Hash"
    };

    public string Description => $"Generate {_algorithm} hash of selected text";
    public string IconName => "Hash";

    public bool IsAvailable() => true;

    public Task<ActionResult> ExecuteAsync(string selectedText, ActionContext? context = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(selectedText))
                return Task.FromResult(ActionResult.FailureResult("No text selected to hash"));

            var bytes = Encoding.UTF8.GetBytes(selectedText);

            byte[] hashBytes = _algorithm switch
            {
                HashAlgorithmKind.MD5 => MD5.HashData(bytes),
                HashAlgorithmKind.SHA1 => SHA1.HashData(bytes),
                HashAlgorithmKind.SHA256 => SHA256.HashData(bytes),
                _ => SHA256.HashData(bytes)
            };

            var hex = Convert.ToHexString(hashBytes).ToLowerInvariant();

            ClipboardService.Set(hex);

            return Task.FromResult(ActionResult.SuccessResult($"{_algorithm} hash copied to clipboard"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ActionResult.FailureResult($"Failed to compute {_algorithm} hash", ex));
        }
    }
}

public enum HashAlgorithmKind
{
    MD5,
    SHA1,
    SHA256
}
