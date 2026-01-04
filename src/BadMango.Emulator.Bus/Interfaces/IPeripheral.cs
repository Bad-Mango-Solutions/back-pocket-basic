// <copyright file="IPeripheral.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Base interface for all peripheral devices in the emulated system.
/// </summary>
/// <remarks>
/// <para>
/// Peripherals are devices that participate in the emulated machine but are
/// not the CPU or main memory. They include:
/// </para>
/// <list type="bullet">
/// <item><description>Motherboard devices (keyboard, speaker, video controller)</description></item>
/// <item><description>Slot cards (Disk II, Super Serial Card, Thunderclock, etc.)</description></item>
/// </list>
/// <para>
/// Specific device types implement derived interfaces:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="ISlotCard"/> for slot-based expansion cards</description></item>
/// <item><description><see cref="IMotherboardDevice"/> for motherboard-integrated devices</description></item>
/// </list>
/// </remarks>
public interface IPeripheral : IScheduledDevice
{
    /// <summary>
    /// Gets the device type identifier (e.g., "DiskII", "MockingBoard", "Keyboard").
    /// </summary>
    /// <value>A string identifying the type of peripheral device.</value>
    string DeviceType { get; }

    /// <summary>
    /// Gets the peripheral classification.
    /// </summary>
    /// <value>The kind of peripheral (Motherboard, SlotCard, or Internal).</value>
    PeripheralKind Kind { get; }

    /// <summary>
    /// Resets the peripheral to power-on state.
    /// </summary>
    /// <remarks>
    /// Called during system reset. The peripheral should clear any state
    /// and return to its initial configuration.
    /// </remarks>
    void Reset();
}