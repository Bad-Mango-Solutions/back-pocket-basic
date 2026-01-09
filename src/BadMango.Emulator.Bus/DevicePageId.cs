// <copyright file="DevicePageId.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Structured device page identifier for 65832 compatibility.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="DevicePageId"/> encodes device classification, instance number,
/// and page offset in a 24-bit value:
/// </para>
/// <list type="bullet">
/// <item><description>Bits 23-20: Device class (4 bits)</description></item>
/// <item><description>Bits 19-8: Instance number (12 bits)</description></item>
/// <item><description>Bits 7-0: Page number (8 bits)</description></item>
/// </list>
/// <para>
/// This encoding allows up to 16 device classes, 4096 instances per class,
/// and 256 pages per instance. The structure supports direct use as a
/// page frame number (PFN) in DEV-backed page table entries.
/// </para>
/// </remarks>
public readonly struct DevicePageId : IEquatable<DevicePageId>
{
    private const int ClassShift = 20;
    private const int InstanceShift = 8;
    private const int ClassBits = 4;
    private const int InstanceBits = 12;
    private const int PageBits = 8;

    // Masks derived from shift constants for consistency
    private const uint ClassMask = ((1u << ClassBits) - 1) << ClassShift;     // 0xF00000
    private const uint InstanceMask = ((1u << InstanceBits) - 1) << InstanceShift; // 0x0FFF00
    private const uint PageMask = (1u << PageBits) - 1;                        // 0x0000FF

    private readonly uint value;

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicePageId"/> struct from a raw value.
    /// </summary>
    /// <param name="rawValue">The raw 24-bit encoded value.</param>
    public DevicePageId(uint rawValue)
    {
        value = rawValue & 0xFFFFFF; // Mask to 24 bits
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DevicePageId"/> struct.
    /// </summary>
    /// <param name="deviceClass">The device class.</param>
    /// <param name="instance">The instance number (0-4095).</param>
    /// <param name="page">The page number (0-255).</param>
    public DevicePageId(DevicePageClass deviceClass, ushort instance, byte page)
    {
        value = ((uint)deviceClass << ClassShift) |
                ((uint)(instance & 0xFFF) << InstanceShift) |
                page;
    }

    /// <summary>
    /// Gets the default (invalid) device page ID.
    /// </summary>
    /// <value>A device page ID with class <see cref="DevicePageClass.Invalid"/>.</value>
    public static DevicePageId Default => default;

    /// <summary>
    /// Gets the raw encoded value.
    /// </summary>
    /// <value>The 24-bit encoded device page identifier.</value>
    public uint RawValue => value;

    /// <summary>
    /// Gets the device class.
    /// </summary>
    /// <value>The device class extracted from the encoded value.</value>
    public DevicePageClass Class => (DevicePageClass)((value & ClassMask) >> ClassShift);

    /// <summary>
    /// Gets the instance number.
    /// </summary>
    /// <value>The 12-bit instance number (0-4095).</value>
    public ushort Instance => (ushort)((value & InstanceMask) >> InstanceShift);

    /// <summary>
    /// Gets the page number.
    /// </summary>
    /// <value>The 8-bit page number (0-255).</value>
    public byte Page => (byte)(value & PageMask);

    /// <summary>
    /// Gets a value indicating whether this is a valid device page ID.
    /// </summary>
    /// <value><see langword="true"/> if the class is not <see cref="DevicePageClass.Invalid"/>; otherwise, <see langword="false"/>.</value>
    public bool IsValid => Class != DevicePageClass.Invalid;

    /// <summary>
    /// Determines whether two <see cref="DevicePageId"/> values are equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(DevicePageId left, DevicePageId right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="DevicePageId"/> values are not equal.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> if not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(DevicePageId left, DevicePageId right) => !left.Equals(right);

    /// <summary>
    /// Creates a device page ID for a compatibility I/O device.
    /// </summary>
    /// <param name="guestId">The guest device identifier.</param>
    /// <param name="page">The page number.</param>
    /// <returns>A new <see cref="DevicePageId"/> for the compatibility I/O device.</returns>
    public static DevicePageId CreateCompatIO(byte guestId, byte page = 0)
        => new(DevicePageClass.CompatIO, guestId, page);

    /// <summary>
    /// Creates a device page ID for a slot ROM region.
    /// </summary>
    /// <param name="slotNumber">The slot number (0-7).</param>
    /// <param name="page">The page number.</param>
    /// <returns>A new <see cref="DevicePageId"/> for the slot ROM.</returns>
    public static DevicePageId CreateSlotROM(byte slotNumber, byte page = 0)
        => new(DevicePageClass.SlotROM, slotNumber, page);

    /// <summary>
    /// Creates a device page ID for a storage device.
    /// </summary>
    /// <param name="controllerId">The controller identifier.</param>
    /// <param name="page">The page number.</param>
    /// <returns>A new <see cref="DevicePageId"/> for the storage device.</returns>
    public static DevicePageId CreateStorage(byte controllerId, byte page = 0)
        => new(DevicePageClass.Storage, controllerId, page);

    /// <summary>
    /// Creates a device page ID for a timer device.
    /// </summary>
    /// <param name="controllerId">The controller identifier.</param>
    /// <param name="page">The page number.</param>
    /// <returns>A new <see cref="DevicePageId"/> for the timer device.</returns>
    public static DevicePageId CreateTimer(byte controllerId, byte page = 0)
        => new(DevicePageClass.Timer, controllerId, page);

    /// <summary>
    /// Creates a device page ID for a debug device.
    /// </summary>
    /// <param name="channelId">The debug channel identifier.</param>
    /// <param name="page">The page number.</param>
    /// <returns>A new <see cref="DevicePageId"/> for the debug device.</returns>
    public static DevicePageId CreateDebug(byte channelId = 0, byte page = 0)
        => new(DevicePageClass.Debug, channelId, page);

    /// <summary>
    /// Creates a device page ID with the specified class, instance, and page.
    /// </summary>
    /// <param name="deviceClass">The device class.</param>
    /// <param name="instance">The instance number.</param>
    /// <param name="page">The page number.</param>
    /// <returns>A new <see cref="DevicePageId"/>.</returns>
    public static DevicePageId Create(DevicePageClass deviceClass, ushort instance, byte page = 0)
        => new(deviceClass, instance, page);

    /// <inheritdoc />
    public bool Equals(DevicePageId other) => value == other.value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is DevicePageId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => value.GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"{Class}:{Instance}:{Page}";
}