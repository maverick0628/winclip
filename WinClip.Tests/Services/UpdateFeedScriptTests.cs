using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using WinClip.Services;

namespace WinClip.Tests.Services;

/// <summary>
/// Runs scripts/New-UpdateFeed.ps1, the script release.yml uses to write latest.json, and
/// reads its output with the app's own parser. A feed the app cannot read is a release every
/// installed copy silently ignores, and nothing else would catch it before a tag.
/// </summary>
public class UpdateFeedScriptTests
{
    [Fact]
    public void ScriptOutput_IsReadableByTheUpdateFeedParser()
    {
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, "scripts", "New-UpdateFeed.ps1");
        Assert.True(File.Exists(script), $"missing {script}");

        var workDir = Path.Combine(Path.GetTempPath(), "winclip-feed-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var x64 = Path.Combine(workDir, "WinClip-9.8.7-x64-Setup.exe");
            var arm64 = Path.Combine(workDir, "WinClip-9.8.7-arm64-Setup.exe");
            File.WriteAllBytes(x64, new byte[] { 1, 2, 3, 4, 5 });
            File.WriteAllBytes(arm64, new byte[] { 6, 7, 8 });
            var feedPath = Path.Combine(workDir, "latest.json");

            var run = RunPwsh(script,
                "-Version", "9.8.7",
                "-BaseUrl", "https://feed.example.test/",
                "-X64Installer", x64,
                "-Arm64Installer", arm64,
                "-OutFile", feedPath);
            if (run is null)
            {
                Assert.False(IsCi, "pwsh must be on PATH in CI so the release feed script is exercised");
                return;
            }

            Assert.True(run.Value.ExitCode == 0, $"New-UpdateFeed.ps1 failed:\n{run.Value.Output}");

            var bytes = File.ReadAllBytes(feedPath);
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                "feed must be written without a BOM");
            var json = File.ReadAllText(feedPath);

            var current = new Version(2, 1, 1);
            AssertInstaller(UpdateFeed.Evaluate(json, current, "x64"), "x64", x64);
            AssertInstaller(UpdateFeed.Evaluate(json, current, "arm64"), "arm64", arm64);

            Assert.Equal(UpdateFeedStatus.UpToDate, UpdateFeed.Evaluate(json, new Version(9, 8, 7), "x64").Status);
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void Script_RefusesPrereleaseVersions()
    {
        var script = Path.Combine(FindRepoRoot(), "scripts", "New-UpdateFeed.ps1");
        var workDir = Path.Combine(Path.GetTempPath(), "winclip-feed-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var x64 = Path.Combine(workDir, "WinClip-9.8.7-rc.1-x64-Setup.exe");
            var arm64 = Path.Combine(workDir, "WinClip-9.8.7-rc.1-arm64-Setup.exe");
            File.WriteAllBytes(x64, new byte[] { 1 });
            File.WriteAllBytes(arm64, new byte[] { 1 });
            var feedPath = Path.Combine(workDir, "latest.json");

            var run = RunPwsh(script,
                "-Version", "9.8.7-rc.1",
                "-BaseUrl", "https://feed.example.test",
                "-X64Installer", x64,
                "-Arm64Installer", arm64,
                "-OutFile", feedPath);
            if (run is null)
            {
                Assert.False(IsCi, "pwsh must be on PATH in CI so the release feed script is exercised");
                return;
            }

            Assert.NotEqual(0, run.Value.ExitCode);
            Assert.False(File.Exists(feedPath));
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch (IOException) { }
        }
    }

    private static void AssertInstaller(UpdateFeedResult result, string arch, string installerPath)
    {
        Assert.True(result.Status == UpdateFeedStatus.UpdateAvailable, $"{arch}: {result.Status} {result.Detail}");
        var update = result.Update!;
        var name = Path.GetFileName(installerPath);
        Assert.Equal("9.8.7", update.Version);
        Assert.Equal($"https://feed.example.test/{name}", update.DownloadUrl);
        Assert.Equal(new FileInfo(installerPath).Length, update.SizeBytes);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(installerPath))), update.Sha256);
        Assert.True(UpdateService.FileMatchesSha256(installerPath, update.Sha256!));
    }

    private static bool IsCi =>
        string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    private static (int ExitCode, string Output)? RunPwsh(string script, params string[] args)
    {
        var psi = new ProcessStartInfo("pwsh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var a in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", script }.Concat(args))
            psi.ArgumentList.Add(a);

        Process process;
        try
        {
            process = Process.Start(psi)!;
        }
        catch (Win32Exception)
        {
            return null;
        }

        using (process)
        {
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(120_000))
            {
                process.Kill(entireProcessTree: true);
                return (-1, "pwsh timed out after 120s");
            }
            process.WaitForExit();
            return (process.ExitCode, stdout.Result + stderr.Result);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "WinClip.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("WinClip.sln not found above " + AppContext.BaseDirectory);
    }
}
