// <copyright file="IMachineStatsProvider.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.StatusMonitor;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;

/// <summary>
/// Provides statistics and metrics about machine performance and state.
/// </summary>
/// <remarks>
/// <para>
/// This interface abstracts the collection of machine statistics for display
/// in the status monitor window. Implementations calculate metrics like
/// instructions per second, CPU utilization, and cycle counts.
/// </para>
/// </remarks>
public interface IMachineStatsProvider
{
    /// <summary>
    /// Gets the associated machine instance.
    /// </summary>
    /// <value>The machine being monitored.</value>
    IMachine Machine { get; }

    /// <summary>
    /// Gets the current machine state.
    /// </summary>
    /// <value>The current lifecycle state of the machine.</value>
    MachineState State { get; }

    /// <summary>
    /// Gets a value indicating whether the CPU is in WAI (Wait for Interrupt) state.
    /// </summary>
    /// <value><see langword="true"/> if the CPU is waiting for an interrupt; otherwise, <see langword="false"/>.</value>
    bool IsWaitingForInterrupt { get; }

    /// <summary>
    /// Gets the duration the CPU has been in WAI state.
    /// </summary>
    /// <value>The elapsed time since WAI was entered, or <see cref="TimeSpan.Zero"/> if not in WAI state.</value>
    TimeSpan WaiDuration { get; }

    /// <summary>
    /// Gets a copy of the current CPU registers.
    /// </summary>
    /// <value>A snapshot of the CPU register state.</value>
    Registers Registers { get; }

    /// <summary>
    /// Gets the total number of cycles executed.
    /// </summary>
    /// <value>The cumulative cycle count since machine reset.</value>
    ulong TotalCycles { get; }

    /// <summary>
    /// Gets the total number of instructions executed.
    /// </summary>
    /// <value>The cumulative instruction count since last statistics reset.</value>
    ulong TotalInstructions { get; }

    /// <summary>
    /// Gets the calculated instructions per second.
    /// </summary>
    /// <value>The current IPS rate.</value>
    double InstructionsPerSecond { get; }

    /// <summary>
    /// Gets the average cycles per instruction.
    /// </summary>
    /// <value>The average CPI calculated over the measurement window.</value>
    double AverageCyclesPerInstruction { get; }

    /// <summary>
    /// Gets the estimated MHz based on actual execution rate.
    /// </summary>
    /// <value>The actual MHz being achieved.</value>
    double ActualMHz { get; }

    /// <summary>
    /// Gets the target MHz for the emulated system.
    /// </summary>
    /// <value>The target clock speed (e.g., 1.0227 for Apple IIe).</value>
    double TargetMHz { get; }

    /// <summary>
    /// Gets the CPU utilization as a percentage.
    /// </summary>
    /// <value>A value from 0 to 100 representing CPU utilization.</value>
    double CpuUtilization { get; }

    /// <summary>
    /// Gets the scheduler queue depth.
    /// </summary>
    /// <value>The number of pending scheduler events.</value>
    int SchedulerQueueDepth { get; }

    /// <summary>
    /// Gets the time until the next scheduled event.
    /// </summary>
    /// <value>The cycle count until the next event fires, or null if no events are scheduled.</value>
    ulong? NextEventCycles { get; }

    /// <summary>
    /// Gets the current annunciator states.
    /// </summary>
    /// <value>An array of 4 boolean values representing annunciator 0-3 states.</value>
    IReadOnlyList<bool> AnnunciatorStates { get; }

    /// <summary>
    /// Gets the status window extensions registered with this provider.
    /// </summary>
    /// <value>A collection of registered status window extensions.</value>
    IReadOnlyList<IStatusWindowExtension> Extensions { get; }

    /// <summary>
    /// Updates statistics by sampling current machine state.
    /// </summary>
    /// <remarks>
    /// This method should be called periodically to refresh calculated metrics
    /// like IPS, MHz, and utilization.
    /// </remarks>
    void Sample();

    /// <summary>
    /// Resets all statistical counters and measurements.
    /// </summary>
    void ResetStats();

    /// <summary>
    /// Registers a status window extension.
    /// </summary>
    /// <param name="extension">The extension to register.</param>
    void RegisterExtension(IStatusWindowExtension extension);
}