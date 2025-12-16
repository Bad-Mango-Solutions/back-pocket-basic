// <copyright file="WaveOutAudioOutput.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Interpreter.Emulation;

/// <summary>
/// Windows/Cross-platform audio output using raw wave API.
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