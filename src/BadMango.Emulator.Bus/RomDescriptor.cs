// <copyright file="RomDescriptor.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Describes a ROM image for loading into machine memory.
/// </summary>
/// <remarks>
/// <para>
/// ROM descriptors provide all information needed to load and map a ROM image
/// into the emulated machine's address space. They support named ROMs for
/// easier identification in debugging and logging.
/// </para>
/// <para>
/// Factory methods provide common ROM configurations for known system types.
/// </para>
/// </remarks>
/// <param name="Data">The ROM data bytes to load.</param>
/// <param name="LoadAddress">The address at which to load the ROM in the machine's address space.</param>
/// <param name="Name">An optional human-readable name for the ROM.</param>
/// <param name="Description">An optional description of the ROM's purpose.</param>
public readonly record struct RomDescriptor(
    byte[] Data,
    Addr LoadAddress,
    string? Name = null,
    string? Description = null)
{
    /// <summary>
    /// The standard size of an Apple IIe full ROM image (16KB).
    /// </summary>
    public const int FullRomSize = 16384;

    /// <summary>
    /// The standard size of the Applesoft BASIC ROM (10KB).
    /// </summary>
    public const int ApplesoftSize = 10240;

    /// <summary>
    /// The standard size of the Monitor ROM (2KB).
    /// </summary>
    public const int MonitorSize = 2048;

    /// <summary>
    /// Load address for the full IIe ROM ($C000).
    /// </summary>
    public const Addr FullRomBase = 0xC000;

    /// <summary>
    /// Load address for Applesoft BASIC ($D800).
    /// </summary>
    public const Addr ApplesoftBase = 0xD800;

    /// <summary>
    /// Load address for the Monitor ROM ($F800).
    /// </summary>
    public const Addr MonitorBase = 0xF800;

    /// <summary>
    /// Creates a ROM descriptor for a full 16KB IIe-compatible ROM image.
    /// </summary>
    /// <param name="data">The ROM data (16KB expected).</param>
    /// <returns>A ROM descriptor configured for full ROM loading at $C000.</returns>
    /// <remarks>
    /// The full ROM includes I/O space handlers, Applesoft BASIC, and the Monitor.
    /// This is the typical ROM configuration for Apple IIe and compatible machines.
    /// </remarks>
    public static RomDescriptor PocketIIeFull(byte[] data)
    {
        return new RomDescriptor(
            data,
            FullRomBase,
            "Pocket IIe Full ROM",
            "Complete 16KB ROM image including I/O handlers, BASIC, and Monitor");
    }

    /// <summary>
    /// Creates a ROM descriptor for the Applesoft BASIC interpreter.
    /// </summary>
    /// <param name="data">The Applesoft ROM data (10KB expected).</param>
    /// <returns>A ROM descriptor configured for Applesoft loading at $D800.</returns>
    /// <remarks>
    /// Applesoft BASIC occupies $D800-$FFFF (minus the Monitor at $F800-$FFFF).
    /// This ROM provides the BASIC language interpreter.
    /// </remarks>
    public static RomDescriptor Applesoft(byte[] data)
    {
        return new RomDescriptor(
            data,
            ApplesoftBase,
            "Applesoft BASIC",
            "Applesoft BASIC interpreter ROM");
    }

    /// <summary>
    /// Creates a ROM descriptor for the Monitor ROM.
    /// </summary>
    /// <param name="data">The Monitor ROM data (2KB expected).</param>
    /// <returns>A ROM descriptor configured for Monitor loading at $F800.</returns>
    /// <remarks>
    /// The Monitor provides low-level machine language routines including
    /// the reset handler, character I/O, and machine language debugging.
    /// </remarks>
    public static RomDescriptor Monitor(byte[] data)
    {
        return new RomDescriptor(
            data,
            MonitorBase,
            "Monitor",
            "System Monitor ROM with reset handler and ML debugging");
    }

    /// <summary>
    /// Creates a custom ROM descriptor with arbitrary address and optional metadata.
    /// </summary>
    /// <param name="data">The ROM data bytes.</param>
    /// <param name="loadAddress">The address at which to load the ROM.</param>
    /// <param name="name">An optional name for the ROM.</param>
    /// <returns>A ROM descriptor with the specified configuration.</returns>
    /// <remarks>
    /// Use this factory method for non-standard ROM configurations or
    /// custom ROM images that don't fit the predefined patterns.
    /// </remarks>
    public static RomDescriptor Custom(byte[] data, Addr loadAddress, string? name = null)
    {
        return new RomDescriptor(data, loadAddress, name);
    }
}