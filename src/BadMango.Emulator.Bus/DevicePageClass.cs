// <copyright file="DevicePageClass.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Device page classifications for structured device identification.
/// </summary>
/// <remarks>
/// <para>
/// The device page class is a 4-bit value encoded in the high nibble of a
/// <see cref="DevicePageId"/>. It categorizes devices by their function,
/// enabling type-safe device enumeration and discovery.
/// </para>
/// <para>
/// This classification scheme supports future 65832 compatibility where
/// device pages can be mapped into the processor's address space via
/// DEV-backed page table entries.
/// </para>
/// </remarks>
public enum DevicePageClass : byte
{
    /// <summary>
    /// Invalid or unassigned device page.
    /// </summary>
    Invalid = 0x0,

    /// <summary>
    /// Compatibility I/O devices (Apple II soft switches, slot I/O).
    /// </summary>
    CompatIO = 0x1,

    /// <summary>
    /// Slot ROM regions ($Cn00-$CnFF, $C800-$CFFF).
    /// </summary>
    SlotROM = 0x2,

    /// <summary>
    /// Storage controllers (Disk II, SmartPort, SCSI).
    /// </summary>
    Storage = 0x3,

    /// <summary>
    /// Timer and clock devices (Thunderclock, system timer).
    /// </summary>
    Timer = 0x4,

    /// <summary>
    /// Input devices (keyboard, mouse, joystick).
    /// </summary>
    Input = 0x5,

    /// <summary>
    /// Audio devices (speaker, Mockingboard, sound chips).
    /// </summary>
    Audio = 0x6,

    /// <summary>
    /// Network devices (Ethernet, serial communications).
    /// </summary>
    Network = 0x7,

    /// <summary>
    /// Framebuffer and video display devices.
    /// </summary>
    Framebuffer = 0x8,

    /// <summary>
    /// Debug and diagnostic devices.
    /// </summary>
    Debug = 0xF,
}