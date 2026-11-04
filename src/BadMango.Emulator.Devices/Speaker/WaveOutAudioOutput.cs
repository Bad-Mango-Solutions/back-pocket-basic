// <copyright file="WaveOutAudioOutput.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

using System.Diagnostics.CodeAnalysis;

using BadMango.Emulator.Devices.Interfaces;
using OpenTK.Audio.OpenAL;

/// <summary>
/// Cross-platform streaming audio output backed by OpenAL.
/// </summary>
/// <remarks>
/// <para>
/// This implementation keeps a single long-lived OpenAL source and streams PCM data
/// into it from a bounded ring buffer.
/// </para>
/// <para>
/// Incoming samples are enqueued by <see cref="Play(short[])"/> and consumed on a dedicated
/// background thread. Buffer overruns drop the oldest samples to keep playback latency bounded.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class WaveOutAudioOutput : IAudioOutput
{
    private const int BufferCount = 3;
    private const int BufferSamples = 1024;
    private const int MaxBufferedMilliseconds = 200;

    private readonly int sampleRate;
    private readonly AudioSampleRingBuffer ringBuffer;
    private readonly short[] sampleChunk;
    private readonly Thread playbackThread;
    private readonly ManualResetEventSlim samplesAvailable = new(false);
    private readonly CancellationTokenSource cts = new();

    private ALDevice device;
    private ALContext context;
    private int source;
    private int[] buffers = [];

    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="WaveOutAudioOutput"/> class with the specified audio configuration.
    /// </summary>
    /// <param name="sampleRate">The sample rate of the audio output, in samples per second.</param>
    /// <param name="bitsPerSample">The number of bits per audio sample (e.g., 16 for 16-bit audio).</param>
    /// <param name="channels">The number of audio channels (e.g., 1 for mono, 2 for stereo).</param>
    /// <remarks>
    /// This constructor sets up OpenAL streaming playback and starts a background thread
    /// that continuously feeds queued PCM data.
    /// </remarks>
    public WaveOutAudioOutput(int sampleRate, int bitsPerSample, int channels)
    {
        if (bitsPerSample != 16)
        {
            throw new PlatformNotSupportedException("OpenAL output currently supports only 16-bit PCM samples.");
        }

        if (channels != 1)
        {
            throw new PlatformNotSupportedException("OpenAL output currently supports only mono output.");
        }

        this.sampleRate = sampleRate;
        ringBuffer = new(sampleRate * MaxBufferedMilliseconds / 1000);
        sampleChunk = new short[BufferSamples];

        OpenAlNativeLibrary.EnsureConfigured();

        try
        {
            device = ALC.OpenDevice(null);
            if (device == default)
            {
                throw new PlatformNotSupportedException("OpenAL device could not be opened.");
            }

            context = ALC.CreateContext(device, Array.Empty<int>());
            if (context == default)
            {
                throw new PlatformNotSupportedException("OpenAL context could not be created.");
            }

            _ = ALC.MakeContextCurrent(context);

            source = AL.GenSource();
            buffers = AL.GenBuffers(BufferCount);

            for (var i = 0; i < BufferCount; i++)
            {
                FillSampleChunk();
                AL.BufferData(buffers[i], ALFormat.Mono16, sampleChunk, sampleRate);
                AL.SourceQueueBuffer(source, buffers[i]);
            }

            AL.SourcePlay(source);
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException or PlatformNotSupportedException)
        {
            CleanupAudioResources();
            throw new PlatformNotSupportedException("OpenAL playback is unavailable on this host.", ex);
        }

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

        ringBuffer.Write(samples);
        samplesAvailable.Set();
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
        samplesAvailable.Set();
        playbackThread.Join(1000);

        CleanupAudioResources();

        cts.Dispose();
        samplesAvailable.Dispose();
    }

    private void PlaybackLoop()
    {
        try
        {
            _ = ALC.MakeContextCurrent(context);

            while (!cts.Token.IsCancellationRequested)
            {
                AL.GetSource(source, ALGetSourcei.BuffersProcessed, out var processed);
                while (processed-- > 0)
                {
                    int processedBuffer = AL.SourceUnqueueBuffer(source);
                    FillSampleChunk();
                    AL.BufferData(processedBuffer, ALFormat.Mono16, sampleChunk, sampleRate);
                    AL.SourceQueueBuffer(source, processedBuffer);
                }

                AL.GetSource(source, ALGetSourcei.SourceState, out var stateValue);
                if ((ALSourceState)stateValue != ALSourceState.Playing)
                {
                    AL.SourcePlay(source);
                }

                _ = samplesAvailable.Wait(5, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (InvalidOperationException)
        {
            // Backend failure: stop playback loop and allow silent operation.
        }
        finally
        {
            try
            {
                _ = ALC.MakeContextCurrent(default);
            }
            catch (InvalidOperationException)
            {
            }
        }
    }

    private void FillSampleChunk()
    {
        sampleChunk.AsSpan().Clear();
        int read = ringBuffer.Read(sampleChunk);
        if (read < sampleChunk.Length)
        {
            samplesAvailable.Reset();
        }
    }

    private void CleanupAudioResources()
    {
        try
        {
            if (source != 0)
            {
                AL.SourceStop(source);
                AL.DeleteSource(source);
            }
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            if (buffers.Length > 0)
            {
                AL.DeleteBuffers(buffers);
            }
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            if (context != default)
            {
                _ = ALC.MakeContextCurrent(default);
                ALC.DestroyContext(context);
            }
        }
        catch (InvalidOperationException)
        {
        }

        try
        {
            if (device != default)
            {
                ALC.CloseDevice(device);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}