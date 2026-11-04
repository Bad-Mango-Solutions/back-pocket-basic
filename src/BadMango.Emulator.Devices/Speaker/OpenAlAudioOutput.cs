// <copyright file="OpenAlAudioOutput.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

using System.Diagnostics.CodeAnalysis;

using BadMango.Emulator.Devices.Interfaces;
using OpenTK.Audio.OpenAL;

using Serilog;

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
/// <para>
/// OpenAL reports failures through <c>alGetError</c>/<c>alcGetError</c> rather than exceptions,
/// so every stage checks the error state explicitly. Setup failures surface as
/// <see cref="PlatformNotSupportedException"/> so callers can fall back to silent output; failures
/// while streaming or tearing down are logged, and persistent streaming errors stop playback.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class OpenAlAudioOutput : IAudioOutput
{
    private const int BufferCount = 3;
    private const int BufferSamples = 1024;
    private const int MaxBufferedMilliseconds = 200;
    private const int MaxConsecutiveStreamErrors = 3;
    private const int DisposeJoinTimeoutMilliseconds = 2000;

    private readonly int sampleRate;
    private readonly ILogger logger;
    private readonly AudioSampleRingBuffer ringBuffer;
    private readonly short[] sampleChunk;
    private readonly Thread playbackThread;
    private readonly ManualResetEventSlim samplesAvailable = new(false);
    private readonly CancellationTokenSource cts = new();

    private ALDevice device;
    private ALContext context;
    private int source;
    private int[] buffers = [];

    private int disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAlAudioOutput"/> class with the specified audio configuration.
    /// </summary>
    /// <param name="sampleRate">The sample rate of the audio output, in samples per second.</param>
    /// <param name="bitsPerSample">The number of bits per audio sample (e.g., 16 for 16-bit audio).</param>
    /// <param name="channels">The number of audio channels (e.g., 1 for mono, 2 for stereo).</param>
    /// <param name="logger">The logger for OpenAL errors; defaults to the global Serilog logger.</param>
    /// <exception cref="PlatformNotSupportedException">
    /// The audio format is unsupported, the OpenAL native library cannot be loaded, or OpenAL
    /// reports an error while opening the device or preparing the stream.
    /// </exception>
    /// <remarks>
    /// This constructor sets up OpenAL streaming playback and starts a background thread
    /// that continuously feeds queued PCM data.
    /// </remarks>
    public OpenAlAudioOutput(int sampleRate, int bitsPerSample, int channels, ILogger? logger = null)
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
        this.logger = logger ?? Log.ForContext<OpenAlAudioOutput>();
        ringBuffer = new(sampleRate * MaxBufferedMilliseconds / 1000);
        sampleChunk = new short[BufferSamples];

        OpenAlNativeLibrary.EnsureConfigured();

        try
        {
            InitializeAudio();
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            // OpenTK resolves the native library on the first AL/ALC call.
            CleanupAudioResources();
            throw new PlatformNotSupportedException("The OpenAL native library could not be loaded.", ex);
        }
        catch (PlatformNotSupportedException)
        {
            CleanupAudioResources();
            throw;
        }

        playbackThread = new(PlaybackLoop)
        {
            IsBackground = true,
            Name = "SpeakerController-Playback",
        };

        try
        {
            playbackThread.Start();
        }
        catch (Exception ex) when (ex is ThreadStateException or OutOfMemoryException or PlatformNotSupportedException)
        {
            // The playback thread owns cleanup once it runs; if it never starts, release here.
            CleanupAudioResources();
            throw;
        }
    }

    /// <inheritdoc />
    public void Play(short[] samples)
    {
        if (Volatile.Read(ref disposed) != 0 || samples.Length == 0)
        {
            return;
        }

        ringBuffer.Write(samples);
        samplesAvailable.Set();
    }

    /// <inheritdoc />
    /// <remarks>
    /// The playback thread releases the OpenAL resources itself as it exits, so they are never
    /// freed while it may still be using them. If the thread does not exit in time, it still
    /// cleans up when it does, and the synchronization primitives are left for it.
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        cts.Cancel();
        samplesAvailable.Set();

        if (!playbackThread.Join(DisposeJoinTimeoutMilliseconds))
        {
            logger.Warning(
                "Speaker playback thread did not exit within {Timeout} ms; it will release OpenAL resources when it exits",
                DisposeJoinTimeoutMilliseconds);
            return;
        }

        cts.Dispose();
        samplesAvailable.Dispose();
    }

    /// <summary>
    /// Throws if OpenAL has recorded an error; used during setup so callers fall back to silent output.
    /// </summary>
    /// <param name="operation">A description of the operation that was attempted.</param>
    private static void ThrowOnAlError(string operation)
    {
        ALError error = AL.GetError();
        if (error != ALError.NoError)
        {
            throw new PlatformNotSupportedException($"OpenAL failed to {operation}: {error}.");
        }
    }

    private void InitializeAudio()
    {
        device = ALC.OpenDevice(null);
        if (device == default)
        {
            throw new PlatformNotSupportedException("OpenAL device could not be opened.");
        }

        context = ALC.CreateContext(device, (int[]?)null);
        if (context == default)
        {
            throw new PlatformNotSupportedException($"OpenAL context could not be created: {ALC.GetError(device)}.");
        }

        if (!ALC.MakeContextCurrent(context))
        {
            throw new PlatformNotSupportedException($"OpenAL context could not be made current: {ALC.GetError(device)}.");
        }

        // Discard any error left over from before this instance owned the context.
        _ = AL.GetError();

        source = AL.GenSource();
        ThrowOnAlError("generate a source");

        buffers = AL.GenBuffers(BufferCount);
        ThrowOnAlError("generate buffers");

        foreach (int buffer in buffers)
        {
            FillSampleChunk();
            AL.BufferData(buffer, ALFormat.Mono16, sampleChunk, sampleRate);
            AL.SourceQueueBuffer(source, buffer);
        }

        ThrowOnAlError("queue the initial buffers");

        AL.SourcePlay(source);
        ThrowOnAlError("start playback");
    }

    private void PlaybackLoop()
    {
        CancellationToken token = cts.Token;
        int consecutiveErrors = 0;

        try
        {
            if (!ALC.MakeContextCurrent(context))
            {
                logger.Error("OpenAL context could not be made current on the playback thread ({Error}); speaker playback stopped", ALC.GetError(device));
                return;
            }

            while (!token.IsCancellationRequested)
            {
                RefillProcessedBuffers();

                AL.GetSource(source, ALGetSourcei.SourceState, out var stateValue);
                if ((ALSourceState)stateValue != ALSourceState.Playing)
                {
                    // Starved or underrun: restart once the queue is refilled.
                    AL.SourcePlay(source);
                }

                if (LogAlError("stream audio"))
                {
                    if (++consecutiveErrors >= MaxConsecutiveStreamErrors)
                    {
                        logger.Error("OpenAL reported {Count} consecutive streaming errors; speaker playback stopped", consecutiveErrors);
                        return;
                    }
                }
                else
                {
                    consecutiveErrors = 0;
                }

                _ = samplesAvailable.Wait(5, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Dispose cancelled the wait; normal shutdown.
        }
        finally
        {
            // The playback thread is the sole owner of the OpenAL resources once started, so it
            // releases them here; Dispose never frees them while this loop may still run.
            CleanupAudioResources();
        }
    }

    private void RefillProcessedBuffers()
    {
        AL.GetSource(source, ALGetSourcei.BuffersProcessed, out var processed);
        while (processed-- > 0)
        {
            int processedBuffer = AL.SourceUnqueueBuffer(source);
            FillSampleChunk();
            AL.BufferData(processedBuffer, ALFormat.Mono16, sampleChunk, sampleRate);
            AL.SourceQueueBuffer(source, processedBuffer);
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

    /// <summary>
    /// Logs and clears any recorded OpenAL error.
    /// </summary>
    /// <param name="operation">A description of the operation that was attempted.</param>
    /// <returns><see langword="true"/> if an error was recorded; otherwise, <see langword="false"/>.</returns>
    private bool LogAlError(string operation)
    {
        ALError error = AL.GetError();
        if (error == ALError.NoError)
        {
            return false;
        }

        logger.Warning("OpenAL error {Error} while trying to {Operation}", error, operation);
        return true;
    }

    /// <summary>
    /// Releases OpenAL resources on a best-effort basis, logging any errors reported.
    /// </summary>
    private void CleanupAudioResources()
    {
        if (context != default)
        {
            // Source and buffer names belong to the context, which must be current to delete them.
            if (ALC.MakeContextCurrent(context))
            {
                if (source != 0)
                {
                    AL.SourceStop(source);
                    AL.DeleteSource(source);
                }

                if (buffers.Length > 0)
                {
                    AL.DeleteBuffers(buffers);
                }

                _ = LogAlError("release the source and buffers");
            }
            else
            {
                logger.Warning("OpenAL context could not be made current for cleanup ({Error})", ALC.GetError(device));
            }

            _ = ALC.MakeContextCurrent(ALContext.Null);
            ALC.DestroyContext(context);

            AlcError contextError = ALC.GetError(device);
            if (contextError != AlcError.NoError)
            {
                logger.Warning("OpenAL error {Error} while destroying the context", contextError);
            }
        }

        if (device != default && !ALC.CloseDevice(device))
        {
            logger.Warning("OpenAL device could not be closed");
        }

        source = 0;
        buffers = [];
        context = default;
        device = default;
    }
}