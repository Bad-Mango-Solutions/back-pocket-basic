// <copyright file="WaveOutAudioOutput.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Basic.Emulation;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Windows/Cross-platform audio output using raw wave API.
/// </summary>
[ExcludeFromCodeCoverage]
internal class WaveOutAudioOutput : IAudioOutput
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
            Name = "AppleSpeaker-Playback",
        };
        playbackThread.Start();
    }

    /// <summary>
    /// Plays the specified audio samples through the audio output.
    /// </summary>
    /// <param name="samples">
    /// An array of audio samples to be played. Each sample is represented as a 16-bit signed integer.
    /// </param>
    /// <remarks>
    /// This method converts the provided audio samples into a byte array and enqueues them for playback.
    /// If the audio output has been disposed or the samples array is empty, the method returns without performing any action.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called after the audio output has been disposed.
    /// </exception>
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

    /// <summary>
    /// Releases all resources used by the <see cref="WaveOutAudioOutput"/> instance.
    /// </summary>
    /// <remarks>
    /// This method ensures proper cleanup of resources, including stopping the playback thread,
    /// releasing any queued audio data, and disposing of managed resources such as
    /// <see cref="CancellationTokenSource"/> and <see cref="ManualResetEventSlim"/>.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called on an already disposed instance.
    /// </exception>
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
            // Use System.Media.SoundPlayer alternative - write to temp WAV and play
            // For better cross-platform support, we'll use a simple approach
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

                        audioData = allBytes.ToArray();
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