// <copyright file="WaveOutAudioOutput.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

using System.Diagnostics.CodeAnalysis;

using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Windows/Cross-platform audio output using raw wave API.
/// </summary>
/// <remarks>
/// <para>
/// This implementation creates WAV data in memory and plays it using platform-specific APIs.
/// On Windows, it uses <see cref="System.Media.SoundPlayer"/>. On other platforms, audio
/// output is currently not supported and will be silent.
/// </para>
/// <para>
/// Audio playback occurs on a background thread to avoid blocking the emulation.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class WaveOutAudioOutput : IAudioOutput
{
    private readonly int sampleRate;
    private readonly int bitsPerSample;
    private readonly int channels;

    // Simple ring buffer for audio output
    private readonly Queue<byte[]> audioQueue = new();
    private readonly Thread playbackThread;
    private readonly ManualResetEventSlim audioAvailable = new(false);
    private readonly CancellationTokenSource cts = new();

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaveOutAudioOutput"/> class with the specified audio configuration.
    /// </summary>
    /// <param name="sampleRate">The sample rate of the audio output, in samples per second.</param>
    /// <param name="bitsPerSample">The number of bits per audio sample (e.g., 16 for 16-bit audio).</param>
    /// <param name="channels">The number of audio channels (e.g., 1 for mono, 2 for stereo).</param>
    /// <remarks>
    /// This constructor sets up the audio output configuration and starts a background thread
    /// for audio playback. The <see cref="WaveOutAudioOutput"/> class provides a cross-platform
    /// implementation for audio output using raw wave APIs.
    /// </remarks>
    public WaveOutAudioOutput(int sampleRate, int bitsPerSample, int channels)
    {
        this.sampleRate = sampleRate;
        this.bitsPerSample = bitsPerSample;
        this.channels = channels;

        // Start playback thread
        playbackThread = new(PlaybackLoop)
        {
            IsBackground = true,
            Name = "SpeakerController-Playback",
        };
        playbackThread.Start();
    }

    /// <inheritdoc />
    public void Play(short[] samples)
    {
        if (disposed || samples.Length == 0)
        {
            return;
        }

        // Convert samples to bytes
        var bytes = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);

        lock (audioQueue)
        {
            audioQueue.Enqueue(bytes);
            audioAvailable.Set();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        cts.Cancel();
        audioAvailable.Set(); // Wake up thread
        playbackThread.Join(1000);

        cts.Dispose();
        audioAvailable.Dispose();
    }

    private static void PlayWavStream(MemoryStream wavStream)
    {
        // Use System.Media.SoundPlayer on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var player = new System.Media.SoundPlayer(wavStream);
                player.PlaySync();
            }
            catch
            {
                // Fallback: ignore if SoundPlayer not available
            }
        }

        // On other platforms, we could use other audio APIs
        // For now, non-Windows platforms will be silent
    }

    private void PlaybackLoop()
    {
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                audioAvailable.Wait(cts.Token);

                byte[]? audioData = null;
                lock (audioQueue)
                {
                    if (audioQueue.Count > 0)
                    {
                        // Combine all queued audio
                        var allBytes = new List<byte>();
                        while (audioQueue.Count > 0)
                        {
                            allBytes.AddRange(audioQueue.Dequeue());
                        }

                        audioData = [.. allBytes];
                    }

                    audioAvailable.Reset();
                }

                if (audioData != null && audioData.Length > 0)
                {
                    PlayWavData(audioData);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }

    private void PlayWavData(byte[] pcmData)
    {
        // Create WAV file in memory and play using platform APIs
        try
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // WAV header
            int dataSize = pcmData.Length;
            int fileSize = 36 + dataSize;

            writer.Write("RIFF"u8.ToArray());
            writer.Write(fileSize);
            writer.Write("WAVE"u8.ToArray());
            writer.Write("fmt "u8.ToArray());
            writer.Write(16); // Subchunk1Size (16 for PCM)
            writer.Write((short)1); // AudioFormat (1 = PCM)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bitsPerSample / 8); // ByteRate
            writer.Write((short)(channels * bitsPerSample / 8)); // BlockAlign
            writer.Write((short)bitsPerSample);
            writer.Write("data"u8.ToArray());
            writer.Write(dataSize);
            writer.Write(pcmData);

            ms.Position = 0;

            // Play using platform-specific method
            PlayWavStream(ms);
        }
        catch
        {
            // Silently ignore playback errors
        }
    }
}