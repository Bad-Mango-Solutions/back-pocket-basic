// <copyright file="DisplayMemorySnapshot.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

/// <summary>
/// Holds a copy of the display memory ranges of main and auxiliary RAM, captured at VBlank.
/// </summary>
/// <remarks>
/// <para>
/// The snapshot covers <c>$0400-$5FFF</c>: text/lo-res pages 1 and 2 (<c>$0400-$0BFF</c>)
/// and hi-res pages 1 and 2 (<c>$2000-$5FFF</c>). The gap at <c>$0C00-$1FFF</c> is included
/// so that each capture is a single contiguous block copy per memory bank.
/// </para>
/// <para>
/// Real IIe video hardware fetches display data from physical main RAM (and auxiliary RAM
/// for 80-column text and double hi-res) regardless of the CPU's MMU state (RAMRD, RAMWRT,
/// 80STORE, PAGE2, HIRES). Rendering from this snapshot instead of reading through the
/// memory bus keeps the displayed image independent of those soft switches.
/// </para>
/// <para>
/// This type is not thread-safe; callers must synchronize capture and reads.
/// </para>
/// </remarks>
public sealed class DisplayMemorySnapshot
{
    /// <summary>
    /// First address covered by the snapshot ($0400).
    /// </summary>
    public const int BaseAddress = 0x0400;

    /// <summary>
    /// Address one past the last address covered by the snapshot ($6000).
    /// </summary>
    public const int EndAddress = 0x6000;

    /// <summary>
    /// Number of bytes captured per memory bank.
    /// </summary>
    public const int Size = EndAddress - BaseAddress;

    private readonly byte[] main = new byte[Size];
    private readonly byte[] aux = new byte[Size];

    /// <summary>
    /// Copies the display range from a span over physical main RAM.
    /// </summary>
    /// <param name="mainRam">Physical main RAM, indexed from address $0000.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="mainRam"/> does not cover the display range.</exception>
    public void CaptureMain(ReadOnlySpan<byte> mainRam)
    {
        CopyRange(mainRam, main, nameof(mainRam));
    }

    /// <summary>
    /// Fills the main display range using a per-byte reader.
    /// </summary>
    /// <param name="readByte">Function returning the byte at an address.</param>
    /// <remarks>
    /// Fallback for machines that do not expose physical main RAM.
    /// </remarks>
    public void CaptureMain(Func<ushort, byte> readByte)
    {
        Fill(readByte, main);
    }

    /// <summary>
    /// Copies the display range from a span over physical auxiliary RAM.
    /// </summary>
    /// <param name="auxRam">Physical auxiliary RAM, indexed from address $0000.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="auxRam"/> does not cover the display range.</exception>
    public void CaptureAux(ReadOnlySpan<byte> auxRam)
    {
        CopyRange(auxRam, aux, nameof(auxRam));
    }

    /// <summary>
    /// Fills the auxiliary display range using a per-byte reader.
    /// </summary>
    /// <param name="readByte">Function returning the auxiliary byte at an address.</param>
    public void CaptureAux(Func<ushort, byte> readByte)
    {
        Fill(readByte, aux);
    }

    /// <summary>
    /// Clears both banks to zero.
    /// </summary>
    public void Clear()
    {
        Array.Clear(main);
        Array.Clear(aux);
    }

    /// <summary>
    /// Reads a byte from the main RAM snapshot.
    /// </summary>
    /// <param name="address">The address to read.</param>
    /// <returns>The captured byte, or 0 if the address is outside the snapshot range.</returns>
    public byte ReadMain(ushort address)
    {
        int offset = address - BaseAddress;
        return (uint)offset < Size ? main[offset] : (byte)0;
    }

    /// <summary>
    /// Reads a byte from the auxiliary RAM snapshot.
    /// </summary>
    /// <param name="address">The address to read.</param>
    /// <returns>The captured byte, or 0 if the address is outside the snapshot range.</returns>
    public byte ReadAux(ushort address)
    {
        int offset = address - BaseAddress;
        return (uint)offset < Size ? aux[offset] : (byte)0;
    }

    private static void CopyRange(ReadOnlySpan<byte> source, byte[] destination, string paramName)
    {
        if (source.Length < EndAddress)
        {
            throw new ArgumentException($"Memory must cover at least ${EndAddress:X4} bytes.", paramName);
        }

        source.Slice(BaseAddress, Size).CopyTo(destination);
    }

    private static void Fill(Func<ushort, byte> readByte, byte[] destination)
    {
        ArgumentNullException.ThrowIfNull(readByte);

        for (int i = 0; i < Size; i++)
        {
            destination[i] = readByte((ushort)(BaseAddress + i));
        }
    }
}