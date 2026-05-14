// <copyright file="DiskIIBootRom.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Read-only 256-byte Disk II boot ROM (P5A) backed by user-supplied bytes.
/// </summary>
/// <remarks>
/// <para>
/// Per PRD §6.2 FR-D9, the Disk II boot ROM is loaded from a user-supplied 256-byte P5A
/// image via the profile's existing <c>rom-images</c> mechanism. This class wraps such a
/// payload as an <see cref="IBusTarget"/> for installation at <c>$Cn00–$CnFF</c>.
/// </para>
/// <para>
/// No Apple-IP ROM bytes ship in this assembly: the buffer is provided by the host. If
/// the user has not supplied a ROM, the controller mounts <see cref="DiskIIBootRomStub"/>
/// (or no slot ROM at all) instead — see <see cref="DiskIIController"/>.
/// </para>
/// </remarks>
public sealed class DiskIIBootRom : IBusTarget
{
    /// <summary>
    /// Required size, in bytes, of a Disk II P5A boot ROM image.
    /// </summary>
    public const int RomSize = 256;

    private readonly PhysicalMemory physicalMemory;
    private readonly RomTarget romTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIIBootRom"/> class.
    /// </summary>
    /// <param name="bootRomBytes">A 256-byte buffer containing the P5A boot ROM payload.</param>
    /// <param name="name">Optional name for diagnostics; defaults to "DiskII Boot ROM".</param>
    /// <exception cref="ArgumentNullException">If <paramref name="bootRomBytes"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">If <paramref name="bootRomBytes"/> is not exactly <see cref="RomSize"/> bytes.</exception>
    public DiskIIBootRom(ReadOnlySpan<byte> bootRomBytes, string? name = null)
    {
        if (bootRomBytes.Length != RomSize)
        {
            throw new ArgumentException($"Disk II boot ROM must be exactly {RomSize} bytes; got {bootRomBytes.Length}.", nameof(bootRomBytes));
        }

        var copy = new byte[RomSize];
        bootRomBytes.CopyTo(copy);
        physicalMemory = new(copy, name ?? "DiskII Boot ROM");
        romTarget = new(physicalMemory.Slice(0, RomSize));
    }

    /// <inheritdoc />
    public TargetCaps Capabilities => romTarget.Capabilities;

    /// <inheritdoc />
    public byte Read8(uint offset, in BusAccess context) => romTarget.Read8(offset & 0xFF, in context);

    /// <inheritdoc />
    public void Write8(uint offset, byte value, in BusAccess context) => romTarget.Write8(offset & 0xFF, value, in context);

    /// <inheritdoc />
    public ushort Read16(uint offset, in BusAccess context) => romTarget.Read16(offset & 0xFF, in context);

    /// <inheritdoc />
    public void Write16(uint offset, ushort value, in BusAccess context) => romTarget.Write16(offset & 0xFF, value, in context);

    /// <inheritdoc />
    public uint Read32(uint offset, in BusAccess context) => romTarget.Read32(offset & 0xFF, in context);

    /// <inheritdoc />
    public void Write32(uint offset, uint value, in BusAccess context) => romTarget.Write32(offset & 0xFF, value, in context);
}