using System.IO;
using System.Media;
using System.Reflection;

namespace WinClip.UI;

/// <summary>
/// Plays the embedded pop.wav sound effect.
/// Falls back to a programmatically generated pop if the embedded resource is missing.
/// </summary>
internal static class PopSound
{
    private static readonly Lazy<SoundPlayer> _player = new(CreatePlayer);

    public static void Play()
    {
        try { _player.Value.Play(); }
        catch { /* Don't crash the app if sound fails */ }
    }

    private static SoundPlayer CreatePlayer()
    {
        // Try to load the embedded pop.wav first
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream("WinClip.Resources.pop.wav");

        if (stream == null)
        {
            // Fallback: generate a simple pop in memory
            stream = new MemoryStream();
            WriteFallbackWav(stream);
            stream.Position = 0;
        }

        var player = new SoundPlayer(stream);
        player.Load();
        return player;
    }

    /// <summary>
    /// Generates a minimal 55ms pop sound in memory as a fallback.
    /// </summary>
    private static void WriteFallbackWav(Stream stream)
    {
        const int sampleRate = 44100;
        const int durationMs = 55;
        int sampleCount = sampleRate * durationMs / 1000;
        int dataSize = sampleCount * 2;

        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 2);
        writer.Write((short)2);
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);

        for (int i = 0; i < sampleCount; i++)
        {
            double t = (double)i / sampleRate;
            double click = i < 22 ? 0.5 * (1.0 - (double)i / 22) : 0.0;
            double low = 0.40 * Math.Exp(-t * 50.0) * Math.Sin(2 * Math.PI * 180.0 * t);
            double mid = 0.35 * Math.Exp(-t * 90.0) * Math.Sin(2 * Math.PI * 700.0 * t);
            double high = 0.25 * Math.Exp(-t * 160.0) * Math.Sin(2 * Math.PI * 2200.0 * t);
            double sample = Math.Clamp(click + low + mid + high, -1.0, 1.0);
            writer.Write((short)(sample * short.MaxValue));
        }
    }
}
