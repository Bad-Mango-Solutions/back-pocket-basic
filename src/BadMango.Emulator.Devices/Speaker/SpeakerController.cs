// <copyright file="SpeakerController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

using System.Runtime.InteropServices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Speaker controller handling $C030 toggle with audio output.
/// </summary>
/// <remarks>
/// <para>
/// The Apple II speaker is a simple 1-bit output toggled by accessing $C030.
/// Each access (read or write) toggles the speaker between high and low states.
/// Sound is produced by toggling the speaker at audio frequencies.
/// </para>
/// <para>
/// This controller records toggle events with cycle timestamps, allowing
/// audio synthesis systems to generate accurate waveforms from the toggle history.
/// It also generates audible click sounds using the host audio system.
/// </para>
/// </remarks>
[DeviceType("speaker")]
public sealed class SpeakerController : ISpeakerDevice, ISoftSwitchProvider, IDisposable
{
    private const byte SpeakerToggleOffset = 0x30;
    private const ushort SpeakerToggleAddress = 0xC030;

    // Audio configuration
    private const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private const int Channels = 1;
    private const ulong DefaultCyclesPerSecond = 1_020_484; // Apple IIe clock; used when the machine reports none.
    private const int SpeakerAmplitude = 8192;
    private const int MaxAudioBufferSamples = SampleRate * 2;
    private const int MaxPendingToggles = 16_384;

    private readonly Queue<(ulong Cycle, bool State)> pendingToggles = new();
    private readonly Lock syncLock = new();
    private readonly List<short> audioBuffer = [];
    private readonly System.Timers.Timer? flushTimer;
    private readonly IAudioOutput audioOutput;

    private bool speakerState;
    private bool hasLastRenderCycle;
    private ulong lastRenderCycle;
    private ulong cycleToSampleRemainder;
    private bool disposed;
    private IScheduler? scheduler;
    private IMachine? machine;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeakerController"/> class.
    /// </summary>
    public SpeakerController()
    {
        try
        {
            audioOutput = new WaveOutAudioOutput(SampleRate, BitsPerSample, Channels);
        }
        catch (Exception ex) when (ex is PlatformNotSupportedException
                                       or ThreadStateException
                                       or InvalidOperationException
                                       or OutOfMemoryException)
        {
            // WaveOutAudioOutput creates and starts a dedicated playback thread.
            // Thread.Start throws PlatformNotSupportedException where threads are
            // unavailable (e.g. single-threaded WASM), OutOfMemoryException when the
            // runtime cannot allocate the thread, ThreadStateException if the thread
            // is in an invalid state to start, and InvalidOperationException from the
            // Thread.Name setter when a name is rejected.
            // Fall back to silent output so the emulator still runs without audio.
            audioOutput = new NullAudioOutput();
        }

        flushTimer = new(50);
        flushTimer.Elapsed += (s, e) => Flush();
        flushTimer.AutoReset = true;
        flushTimer.Start();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeakerController"/> class with explicit dependencies.
    /// </summary>
    /// <param name="audioOutput">The audio output sink used to play generated PCM samples.</param>
    /// <param name="startFlushTimer">A value indicating whether periodic flushing should start automatically.</param>
    internal SpeakerController(IAudioOutput audioOutput, bool startFlushTimer)
    {
        ArgumentNullException.ThrowIfNull(audioOutput);
        this.audioOutput = audioOutput;
        flushTimer = null;

        if (startFlushTimer)
        {
            flushTimer = new(50);
            flushTimer.Elapsed += (s, e) => Flush();
            flushTimer.AutoReset = true;
            flushTimer.Start();
        }
    }

    /// <inheritdoc />
    public event Action<ulong, bool>? Toggled;

    /// <inheritdoc />
    public string Name => "Speaker Controller";

    /// <inheritdoc />
    public string DeviceType => "Speaker";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.Motherboard;

    /// <inheritdoc />
    public bool State => speakerState;

    /// <inheritdoc />
    public IReadOnlyList<(ulong Cycle, bool State)> PendingToggles
    {
        get
        {
            lock (syncLock)
            {
                return [.. pendingToggles];
            }
        }
    }

    /// <inheritdoc />
    public string ProviderName => "Speaker";

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        scheduler = context.Scheduler;

        // Devices are initialized with the machine itself as the event context,
        // which exposes the configured CPU clock rate used to derive pitch.
        machine = context as IMachine;
    }

    /// <inheritdoc />
    public void RegisterHandlers(IOPageDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        // $C030: Speaker toggle (read and write)
        dispatcher.Register(SpeakerToggleOffset, ToggleSpeakerRead, ToggleSpeakerWrite);
    }

    /// <inheritdoc />
    public void Reset()
    {
        lock (syncLock)
        {
            speakerState = false;
            pendingToggles.Clear();
            audioBuffer.Clear();
            hasLastRenderCycle = false;
            lastRenderCycle = 0;
            cycleToSampleRemainder = 0;
        }
    }

    /// <inheritdoc />
    public IList<(ulong Cycle, bool State)> DrainToggles()
    {
        lock (syncLock)
        {
            var result = pendingToggles.ToList();
            pendingToggles.Clear();
            return result;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        return
        [
            new("SPEAKER", SpeakerToggleAddress, speakerState, "Speaker output state (toggled on access)"),
        ];
    }

    /// <summary>
    /// Flushes accumulated click audio to the output device.
    /// </summary>
    public void Flush()
    {
        if (disposed)
        {
            return;
        }

        lock (syncLock)
        {
            FlushInternal();
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

        flushTimer?.Stop();
        flushTimer?.Dispose();

        lock (syncLock)
        {
            FlushInternal();
        }

        audioOutput.Dispose();
    }

    private void FlushInternal()
    {
        AppendCurrentStateUntilNow();

        if (audioBuffer.Count == 0)
        {
            return;
        }

        try
        {
            var samples = audioBuffer.ToArray();
            audioBuffer.Clear();
            audioOutput.Play(samples);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException)
        {
            // The output can be disposed concurrently with a timer-driven flush
            // (its wait handle then throws ObjectDisposedException), and audio
            // backends may reject playback in an invalid device state. Drop the
            // samples rather than crash the emulator.
            audioBuffer.Clear();
        }
    }

    private byte ToggleSpeakerRead(byte offset, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            Toggle(context.Cycle);
        }

        return 0xFF; // Floating bus
    }

    private void ToggleSpeakerWrite(byte offset, byte value, in BusAccess context)
    {
        if (!context.IsSideEffectFree)
        {
            Toggle(context.Cycle);
        }
    }

    private void Toggle(ulong cycle)
    {
        lock (syncLock)
        {
            AppendStateSegment(cycle);
            speakerState = !speakerState;
            pendingToggles.Enqueue((cycle, speakerState));
            while (pendingToggles.Count > MaxPendingToggles)
            {
                _ = pendingToggles.Dequeue();
            }
        }

        Toggled?.Invoke(cycle, speakerState);
    }

    private void AppendCurrentStateUntilNow()
    {
        if (scheduler is null || !hasLastRenderCycle)
        {
            return;
        }

        ulong currentCycle = scheduler.Now;
        AppendStateSegment(currentCycle);
    }

    private void AppendStateSegment(ulong cycle)
    {
        if (!hasLastRenderCycle)
        {
            hasLastRenderCycle = true;
            lastRenderCycle = cycle;
            return;
        }

        if (cycle <= lastRenderCycle)
        {
            lastRenderCycle = cycle;
            return;
        }

        ulong deltaCycles = cycle - lastRenderCycle;
        lastRenderCycle = cycle;

        ulong cyclesPerSecond = GetCyclesPerSecond();
        if (cycleToSampleRemainder >= cyclesPerSecond)
        {
            // The clock rate changed since the last segment; keep the carry in range.
            cycleToSampleRemainder %= cyclesPerSecond;
        }

        // Use 128-bit intermediates so very large cycle deltas (long pauses,
        // unthrottled runs) cannot overflow, then clamp to the buffer cap before
        // narrowing to int.
        UInt128 numerator = ((UInt128)deltaCycles * (ulong)SampleRate) + cycleToSampleRemainder;
        UInt128 wideSampleCount = numerator / cyclesPerSecond;
        cycleToSampleRemainder = (ulong)(numerator % cyclesPerSecond);
        ulong sampleCountUnclamped = wideSampleCount > ulong.MaxValue ? ulong.MaxValue : (ulong)wideSampleCount;
        if (sampleCountUnclamped == 0)
        {
            return;
        }

        int sampleCount = (int)Math.Min(sampleCountUnclamped, (ulong)MaxAudioBufferSamples);

        short sampleValue = (short)(speakerState ? SpeakerAmplitude : -SpeakerAmplitude);
        AppendSamples(sampleValue, sampleCount);
    }

    /// <summary>
    /// Gets the emulated clock rate used to convert cycle deltas to samples.
    /// </summary>
    /// <returns>
    /// The machine's <see cref="IMachine.ClockSpeedHz"/> when it is positive; otherwise
    /// <see cref="DefaultCyclesPerSecond"/>. A clock speed of zero means "unthrottled",
    /// but the emulated hardware still nominally runs at the default rate.
    /// </returns>
    private ulong GetCyclesPerSecond()
    {
        long clockSpeedHz = machine?.ClockSpeedHz ?? 0;
        return clockSpeedHz > 0 ? (ulong)clockSpeedHz : DefaultCyclesPerSecond;
    }

    private void AppendSamples(short sampleValue, int sampleCount)
    {
        if (sampleCount >= MaxAudioBufferSamples)
        {
            // The new segment alone fills the cap; keep only its newest samples.
            audioBuffer.Clear();
            sampleCount = MaxAudioBufferSamples;
        }
        else
        {
            int total = audioBuffer.Count + sampleCount;
            if (total > MaxAudioBufferSamples)
            {
                // Drop the oldest samples to bound latency and memory.
                audioBuffer.RemoveRange(0, total - MaxAudioBufferSamples);
            }
        }

        int start = audioBuffer.Count;
        CollectionsMarshal.SetCount(audioBuffer, start + sampleCount);
        CollectionsMarshal.AsSpan(audioBuffer).Slice(start, sampleCount).Fill(sampleValue);
    }
}