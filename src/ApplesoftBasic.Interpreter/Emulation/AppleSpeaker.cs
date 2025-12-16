using System.Buffers;
using Microsoft.Extensions.Logging;

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Interface for Apple II speaker emulation
/// </summary>
public interface IAppleSpeaker : IDisposable
{
    /// <summary>
    /// Toggles the speaker cone (click), emulating access to $C030
    /// </summary>
    void Click();

    /// <summary>
    /// Plays the authentic Apple II beep tone (~1000Hz for ~0.1 seconds)
    /// Used when CHR$(7) is printed
    /// </summary>
    void Beep();

    /// <summary>
    /// Flushes any buffered audio to the output device
    /// </summary>
    void Flush();
}

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
    private const int BeepFrequency = 1000;
    private const double BeepDuration = 0.1;

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
    /// This emulates writing to memory location $C030 (49200)
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
    /// The Apple II beep is approximately 1000Hz for 0.1 seconds
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
    /// Flushes accumulated click audio to the output device
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

/// <summary>
/// Interface for platform-specific audio output
/// </summary>
internal interface IAudioOutput : IDisposable
{
    void Play(short[] samples);
}

/// <summary>
/// Null audio output for when audio is unavailable
/// </summary>
internal class NullAudioOutput : IAudioOutput
{
    public void Play(short[] samples) { }
    public void Dispose() { }
}

/// <summary>
/// Windows/Cross-platform audio output using raw wave API
/// </summary>
internal class WaveOutAudioOutput : IAudioOutput
{
    private readonly int _sampleRate;
    private readonly int _bitsPerSample;
    private readonly int _channels;
    private readonly Stream? _waveStream;
    private readonly BinaryWriter? _writer;
    private bool _disposed;

    // Simple ring buffer for audio output
    private readonly Queue<byte[]> _audioQueue = new();
    private readonly Thread _playbackThread;
    private readonly ManualResetEventSlim _audioAvailable = new(false);
    private readonly CancellationTokenSource _cts = new();

    public WaveOutAudioOutput(int sampleRate, int bitsPerSample, int channels)
    {
        _sampleRate = sampleRate;
        _bitsPerSample = bitsPerSample;
        _channels = channels;

        // Start playback thread
        _playbackThread = new Thread(PlaybackLoop)
        {
            IsBackground = true,
            Name = "AppleSpeaker-Playback"
        };
        _playbackThread.Start();
    }

    public void Play(short[] samples)
    {
        if (_disposed || samples.Length == 0) return;

        // Convert samples to bytes
        var bytes = new byte[samples.Length * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);

        lock (_audioQueue)
        {
            _audioQueue.Enqueue(bytes);
            _audioAvailable.Set();
        }
    }

    private void PlaybackLoop()
    {
        try
        {
            // Use System.Media.SoundPlayer alternative - write to temp WAV and play
            // For better cross-platform support, we'll use a simple approach
            while (!_cts.Token.IsCancellationRequested)
            {
                _audioAvailable.Wait(_cts.Token);

                byte[]? audioData = null;
                lock (_audioQueue)
                {
                    if (_audioQueue.Count > 0)
                    {
                        // Combine all queued audio
                        var allBytes = new List<byte>();
                        while (_audioQueue.Count > 0)
                        {
                            allBytes.AddRange(_audioQueue.Dequeue());
                        }
                        audioData = allBytes.ToArray();
                    }
                    _audioAvailable.Reset();
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
            writer.Write((short)_channels);
            writer.Write(_sampleRate);
            writer.Write(_sampleRate * _channels * _bitsPerSample / 8); // ByteRate
            writer.Write((short)(_channels * _bitsPerSample / 8)); // BlockAlign
            writer.Write((short)_bitsPerSample);
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

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts.Cancel();
        _audioAvailable.Set(); // Wake up thread
        _playbackThread.Join(1000);

        _cts.Dispose();
        _audioAvailable.Dispose();
    }
}
