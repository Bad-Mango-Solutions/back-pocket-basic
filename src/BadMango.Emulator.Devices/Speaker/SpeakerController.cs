// <copyright file="SpeakerController.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

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

    // Click timing - Apple II CPU runs at ~1.023 MHz
    // A typical click produces a brief pulse
    private const int ClickSamples = 64; // ~1.5ms pulse

    private readonly List<(ulong Cycle, bool State)> pendingToggles = [];
    private readonly Lock syncLock = new();

    // Audio buffer for accumulating clicks
    private readonly List<short> audioBuffer = [];
    private readonly System.Timers.Timer flushTimer;

    private bool speakerState;
    private DateTime lastClickTime = DateTime.MinValue;
    private bool disposed;
    private IScheduler? scheduler;

    // Platform audio output
    private IAudioOutput? audioOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpeakerController"/> class.
    /// </summary>
    public SpeakerController()
    {
        // Set up a timer to flush audio buffer periodically
        flushTimer = new(50); // 50ms intervals
        flushTimer.Elapsed += (s, e) => Flush();
        flushTimer.AutoReset = true;
        flushTimer.Start();

        // Initialize audio output
        InitializeAudioOutput();
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
    public IReadOnlyList<(ulong Cycle, bool State)> PendingToggles => pendingToggles;

    /// <inheritdoc />
    public string ProviderName => "Speaker";

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        scheduler = context.Scheduler;
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
        }
    }

    /// <inheritdoc />
    public IList<(ulong Cycle, bool State)> DrainToggles()
    {
        lock (syncLock)
        {
            var result = new List<(ulong, bool)>(pendingToggles);
            pendingToggles.Clear();
            return result;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SoftSwitchState> GetSoftSwitchStates()
    {
        return
        [
            new SoftSwitchState("SPEAKER", SpeakerToggleAddress, speakerState, "Speaker output state (toggled on access)"),
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

        flushTimer.Stop();
        flushTimer.Dispose();

        lock (syncLock)
        {
            FlushInternal();
        }

        audioOutput?.Dispose();
    }

    private void InitializeAudioOutput()
    {
        try
        {
            audioOutput = new WaveOutAudioOutput(SampleRate, BitsPerSample, Channels);
        }
        catch
        {
            // Fall back to null output if audio initialization fails
            audioOutput = new NullAudioOutput();
        }
    }

    private void FlushInternal()
    {
        if (audioBuffer.Count == 0)
        {
            return;
        }

        try
        {
            var samples = audioBuffer.ToArray();
            audioBuffer.Clear();
            audioOutput?.Play(samples);
        }
        catch
        {
            // Silently ignore playback errors
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
            // Toggle speaker state
            speakerState = !speakerState;

            // Record the toggle for cycle-accurate synthesis
            pendingToggles.Add((cycle, speakerState));

            // Generate click audio
            GenerateClickAudio();
        }

        // Raise event outside the lock
        Toggled?.Invoke(cycle, speakerState);
    }

    private void GenerateClickAudio()
    {
        // Calculate time since last click to determine sample gap
        var now = DateTime.UtcNow;
        var timeSinceLastClick = now - lastClickTime;
        lastClickTime = now;

        // Add silence between clicks if there was a gap
        if (timeSinceLastClick.TotalMilliseconds > 1 && timeSinceLastClick.TotalMilliseconds < 100)
        {
            int silenceSamples = (int)(timeSinceLastClick.TotalSeconds * SampleRate);
            silenceSamples = Math.Min(silenceSamples, SampleRate / 10); // Cap at 100ms of silence

            for (int i = 0; i < silenceSamples; i++)
            {
                audioBuffer.Add(0);
            }
        }

        // Generate click waveform (sharp transition)
        short amplitude = speakerState ? short.MaxValue : short.MinValue;

        // Create a brief pulse with attack/decay to reduce harshness
        for (int i = 0; i < ClickSamples; i++)
        {
            // Apply simple envelope to smooth the click
            double envelope = 1.0;
            if (i < 8)
            {
                envelope = i / 8.0; // Attack
            }
            else if (i > ClickSamples - 8)
            {
                envelope = (ClickSamples - i) / 8.0; // Decay
            }

            audioBuffer.Add((short)(amplitude * envelope));
        }
    }
}