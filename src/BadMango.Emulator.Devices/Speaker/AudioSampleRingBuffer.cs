// <copyright file="AudioSampleRingBuffer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Speaker;

/// <summary>
/// Bounded sample ring buffer for streaming speaker PCM output.
/// </summary>
internal sealed class AudioSampleRingBuffer
{
    private readonly short[] buffer;
    private readonly object syncRoot = new();

    private int readIndex;
    private int writeIndex;
    private int count;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioSampleRingBuffer"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of samples that can be buffered.</param>
    public AudioSampleRingBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        buffer = new short[capacity];
    }

    /// <summary>
    /// Gets the number of buffered samples currently available to read.
    /// </summary>
    public int Count
    {
        get
        {
            lock (syncRoot)
            {
                return count;
            }
        }
    }

    /// <summary>
    /// Appends samples to the ring buffer.
    /// </summary>
    /// <param name="samples">The PCM samples to append.</param>
    public void Write(ReadOnlySpan<short> samples)
    {
        if (samples.IsEmpty)
        {
            return;
        }

        lock (syncRoot)
        {
            if (samples.Length >= buffer.Length)
            {
                ReadOnlySpan<short> tail = samples[^buffer.Length..];
                tail.CopyTo(buffer);
                readIndex = 0;
                writeIndex = 0;
                count = buffer.Length;
                return;
            }

            int free = buffer.Length - count;
            if (samples.Length > free)
            {
                int drop = samples.Length - free;
                readIndex = (readIndex + drop) % buffer.Length;
                count -= drop;
            }

            int firstCopy = Math.Min(samples.Length, buffer.Length - writeIndex);
            samples[..firstCopy].CopyTo(buffer.AsSpan(writeIndex, firstCopy));
            int remaining = samples.Length - firstCopy;
            if (remaining > 0)
            {
                samples[firstCopy..].CopyTo(buffer.AsSpan(0, remaining));
            }

            writeIndex = (writeIndex + samples.Length) % buffer.Length;
            count += samples.Length;
        }
    }

    /// <summary>
    /// Reads up to the requested number of samples from the ring buffer.
    /// </summary>
    /// <param name="destination">The destination span that receives the buffered samples.</param>
    /// <returns>The number of samples copied to <paramref name="destination"/>.</returns>
    public int Read(Span<short> destination)
    {
        if (destination.IsEmpty)
        {
            return 0;
        }

        lock (syncRoot)
        {
            int toRead = Math.Min(destination.Length, count);
            if (toRead == 0)
            {
                return 0;
            }

            int firstCopy = Math.Min(toRead, buffer.Length - readIndex);
            buffer.AsSpan(readIndex, firstCopy).CopyTo(destination);
            int remaining = toRead - firstCopy;
            if (remaining > 0)
            {
                buffer.AsSpan(0, remaining).CopyTo(destination[firstCopy..]);
            }

            readIndex = (readIndex + toRead) % buffer.Length;
            count -= toRead;
            return toRead;
        }
    }
}