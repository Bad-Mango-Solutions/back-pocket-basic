// <copyright file="PixelBuffer.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

/// <summary>
/// Manages a pixel buffer backed by pooled memory for efficient rendering to a <see cref="WriteableBitmap"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a zero-copy approach to pixel buffer management using <see cref="MemoryPool{T}"/>
/// for efficient memory allocation. The buffer is accessed as a <see cref="Span{T}"/> of <see cref="uint"/>
/// values in BGRA format (Bgra8888).
/// </para>
/// <para>
/// The underlying array is accessed via <see cref="MemoryMarshal.TryGetArray{T}"/> for safe interop
/// with <see cref="Marshal.Copy(byte[], int, IntPtr, int)"/> when copying to the framebuffer.
/// </para>
/// </remarks>
public sealed class PixelBuffer : IDisposable
{
    private readonly int byteSize;
    private IMemoryOwner<byte>? bufferOwner;
    private Memory<byte> buffer;
    private WriteableBitmap? bitmap;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PixelBuffer"/> class with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the pixel buffer in pixels.</param>
    /// <param name="height">The height of the pixel buffer in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.
    /// </exception>
    public PixelBuffer(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        this.Width = width;
        this.Height = height;
        byteSize = width * height * sizeof(uint);

        AllocateBuffer();
        CreateBitmap();
    }

    /// <summary>
    /// Gets the width of the pixel buffer in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the pixel buffer in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the stride (bytes per row) of the pixel buffer.
    /// </summary>
    public int Stride => Width;

    /// <summary>
    /// Gets the <see cref="WriteableBitmap"/> that can be used as an image source.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer has been disposed.</exception>
    public WriteableBitmap Bitmap
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return bitmap!;
        }
    }

    /// <summary>
    /// Gets the pixel data as a span of unsigned 32-bit integers in BGRA format.
    /// </summary>
    /// <returns>A span of pixels that can be written to directly.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<uint> GetPixels()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return MemoryMarshal.Cast<byte, uint>(buffer.Span);
    }

    /// <summary>
    /// Fills the entire pixel buffer with the specified color.
    /// </summary>
    /// <param name="color">The BGRA color value to fill with.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Fill(uint color)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        GetPixels().Fill(color);
    }

    /// <summary>
    /// Sets a single pixel at the specified coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate of the pixel.</param>
    /// <param name="y">The y-coordinate of the pixel.</param>
    /// <param name="color">The BGRA color value.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when coordinates are outside the buffer bounds.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPixel(int x, int y, uint color)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"X coordinate must be between 0 and {Width - 1}.");
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Y coordinate must be between 0 and {Height - 1}.");
        }

        GetPixels()[(y * Width) + x] = color;
    }

    /// <summary>
    /// Commits the pixel buffer to the <see cref="WriteableBitmap"/> for display.
    /// </summary>
    /// <remarks>
    /// This method locks the bitmap framebuffer and copies the pixel data using
    /// a zero-copy approach via <see cref="MemoryMarshal.TryGetArray{T}"/>.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer has been disposed.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the underlying buffer array cannot be accessed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Commit()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        using var framebuffer = bitmap!.Lock();

        if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> segment))
        {
            Marshal.Copy(
                segment.Array!,
                segment.Offset,
                framebuffer.Address,
                segment.Count);
        }
        else
        {
            throw new InvalidOperationException(
                "Failed to access underlying buffer array. The MemoryPool implementation may not support array access.");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        bufferOwner?.Dispose();
        bufferOwner = null;
        buffer = default;
        bitmap = null;
        disposed = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AllocateBuffer()
    {
        bufferOwner = MemoryPool<byte>.Shared.Rent(byteSize);
        buffer = bufferOwner.Memory[..byteSize];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CreateBitmap()
    {
        bitmap = new(
            new(Width, Height),
            new(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);
    }
}