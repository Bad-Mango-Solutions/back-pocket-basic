// <copyright file="AppleSpeaker.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

using System.Buffers;

using Microsoft.Extensions.Logging;

/// <summary>
/// Apple II speaker emulation using raw PCM audio
/// The Apple II speaker works by toggling a single bit that moves the speaker cone
/// in or out. Rapid toggling creates sound waves.
/// </summary>
public class AppleSpeaker : IAppleSpeaker
{
    private readonly ILogger<AppleSpeaker> _logger;
    private readonly object _lock = new();

    // Audio configuration
    private const int SampleRate = 44100;
    private const int BitsPerSample = 16;
    private const int Channels = 1;
    private const int BytesPerSample = BitsPerSample / 8;

    // Click timing - Apple II CPU runs at ~1.023 MHz
    // A typical click produces a brief pulse
    private const int ClickSamples = 64; // ~1.5ms pulse

    // Beep configuration - authentic Apple II beep is ~1000Hz for ~0.1 seconds
    private const int BeepFrequency = 800;
    private const double BeepDuration = 0.15;

    // Audio buffer for accumulating clicks
    private readonly List<short> _audioBuffer = new();
    private readonly System.Timers.Timer _flushTimer;
    private bool _speakerState; // Current speaker cone position (in/out)
    private DateTime _lastClickTime = DateTime.MinValue;
    private bool _disposed;

    // Platform audio output
    private IAudioOutput? _audioOutput;

    public AppleSpeaker(ILogger<AppleSpeaker> logger)
    {
        _logger = logger;

        // Set up a timer to flush audio buffer periodically
        _flushTimer = new System.Timers.Timer(50); // 50ms intervals
        _flushTimer.Elapsed += (s, e) => Flush();
        _flushTimer.AutoReset = true;
        _flushTimer.Start();

        // Initialize audio output
        InitializeAudioOutput();

        _logger.LogDebug("Apple II speaker emulation initialized (SampleRate={SampleRate}Hz)", SampleRate);
    }

    private void InitializeAudioOutput()
    {
        try
        {
            _audioOutput = new WaveOutAudioOutput(SampleRate, BitsPerSample, Channels);
            _logger.LogDebug("Audio output initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize audio output - speaker emulation will be silent");
            _audioOutput = new NullAudioOutput();
        }
    }

    /// <summary>
    /// Toggles the speaker cone, producing a click
    /// This emulates writing to memory location $C030 (49200).
    /// </summary>
    public void Click()
    {
        if (_disposed) return;

        lock (_lock)
        {
            // Toggle speaker state
            _speakerState = !_speakerState;

            // Calculate time since last click to determine sample gap
            var now = DateTime.UtcNow;
            var timeSinceLastClick = now - _lastClickTime;
            _lastClickTime = now;

            // Add silence between clicks if there was a gap
            if (timeSinceLastClick.TotalMilliseconds > 1 && timeSinceLastClick.TotalMilliseconds < 100)
            {
                int silenceSamples = (int)(timeSinceLastClick.TotalSeconds * SampleRate);
                silenceSamples = Math.Min(silenceSamples, SampleRate / 10); // Cap at 100ms of silence

                for (int i = 0; i < silenceSamples; i++)
                {
                    _audioBuffer.Add(0);
                }
            }

            // Generate click waveform (sharp transition)
            short amplitude = _speakerState ? short.MaxValue : short.MinValue;

            // Create a brief pulse with attack/decay to reduce harshness
            for (int i = 0; i < ClickSamples; i++)
            {
                // Apply simple envelope to smooth the click
                double envelope = 1.0;
                if (i < 8) envelope = i / 8.0; // Attack
                else if (i > ClickSamples - 8) envelope = (ClickSamples - i) / 8.0; // Decay

                _audioBuffer.Add((short)(amplitude * envelope));
            }
        }
    }

    /// <summary>
    /// Plays the authentic Apple II beep tone
    /// The Apple II beep is approximately 1000Hz for 0.1 seconds.
    /// </summary>
    public void Beep()
    {
        if (_disposed) return;

        lock (_lock)
        {
            // Flush any pending clicks first
            FlushInternal();

            // Generate beep waveform
            int totalSamples = (int)(SampleRate * BeepDuration);
            var beepBuffer = new short[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                // Square wave at BeepFrequency Hz (more authentic than sine wave)
                double t = (double)i / SampleRate;
                double cycle = t * BeepFrequency;
                bool high = (cycle % 1.0) < 0.5;

                // Apply envelope for smooth attack/release
                double envelope = 1.0;
                int attackSamples = SampleRate / 100; // 10ms attack
                int releaseSamples = SampleRate / 100; // 10ms release

                if (i < attackSamples)
                    envelope = (double)i / attackSamples;
                else if (i > totalSamples - releaseSamples)
                    envelope = (double)(totalSamples - i) / releaseSamples;

                short amplitude = (short)(short.MaxValue * 0.5 * envelope); // 50% volume
                beepBuffer[i] = high ? amplitude : (short)(-amplitude);
            }

            // Play beep directly
            _audioOutput?.Play(beepBuffer);
        }

        _logger.LogTrace("Beep played ({Frequency}Hz, {Duration}s)", BeepFrequency, BeepDuration);
    }

    /// <summary>
    /// Flushes accumulated click audio to the output device.
    /// </summary>
    public void Flush()
    {
        if (_disposed) return;

        lock (_lock)
        {
            FlushInternal();
        }
    }

    private void FlushInternal()
    {
        if (_audioBuffer.Count == 0) return;

        try
        {
            var samples = _audioBuffer.ToArray();
            _audioBuffer.Clear();
            _audioOutput?.Play(samples);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to play audio buffer");
            _audioBuffer.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _flushTimer.Stop();
        _flushTimer.Dispose();

        lock (_lock)
        {
            FlushInternal();
        }

        _audioOutput?.Dispose();
        _logger.LogDebug("Apple II speaker emulation disposed");
    }
}