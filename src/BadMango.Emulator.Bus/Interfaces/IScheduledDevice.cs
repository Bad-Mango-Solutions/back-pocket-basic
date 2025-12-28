// <copyright file="IScheduledDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Represents a device that participates in the scheduler for cycle-accurate timing.
/// </summary>
/// <remarks>
/// <para>
/// Devices that need to schedule events (timers, periodic callbacks, DMA transfers, etc.)
/// implement this interface to receive the event context during system initialization.
/// </para>
/// <para>
/// The scheduler is the single source of truth for timing. Devices should not maintain
/// their own ad-hoc timers or time tracking. All timing-related work should be scheduled
/// through the <see cref="IScheduler"/> provided in the event context.
/// </para>
/// <para>
/// Device initialization occurs after all components are created and wired to the bus,
/// but before the machine starts running. This allows devices to schedule initial events
/// (e.g., a video controller scheduling the first scanline).
/// </para>
/// </remarks>
public interface IScheduledDevice
{
    /// <summary>
    /// Gets the name of this device.
    /// </summary>
    /// <value>A human-readable name for the device, used for diagnostics and debugging.</value>
    string Name { get; }

    /// <summary>
    /// Initializes the device with access to system services.
    /// </summary>
    /// <param name="context">Event context providing access to scheduler, signals, and bus.</param>
    /// <remarks>
    /// <para>
    /// Called after all devices are created but before the machine runs.
    /// Devices can use this to:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Store a reference to the event context for later use</description></item>
    /// <item><description>Schedule initial events (e.g., first timer tick)</description></item>
    /// <item><description>Register for signal callbacks</description></item>
    /// <item><description>Perform any other initialization that requires system services</description></item>
    /// </list>
    /// </remarks>
    void Initialize(IEventContext context);
}