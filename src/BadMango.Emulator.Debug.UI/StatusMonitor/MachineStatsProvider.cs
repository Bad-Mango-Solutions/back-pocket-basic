// <copyright file="MachineStatsProvider.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.StatusMonitor;

using System.Diagnostics;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Provides machine statistics and performance metrics by sampling machine state.
/// </summary>
/// <remarks>
/// <para>
/// This class implements <see cref="IMachineStatsProvider"/> to collect and calculate
/// machine performance metrics. It uses time-based sampling to compute rates like
/// instructions per second and CPU utilization.
/// </para>
/// <para>
/// The provider should be sampled periodically (e.g., 60Hz) to maintain accurate
/// rolling statistics for display in the status monitor window.
/// </para>
/// </remarks>
public sealed class MachineStatsProvider : IMachineStatsProvider
{
    /// <summary>
    /// Target clock speed for Apple IIe (1.0227 MHz).
    /// </summary>
    private const double AppleIIeClockMHz = 1.0227;

    /// <summary>
    /// Minimum sampling interval in seconds to avoid division by very small numbers.
    /// </summary>
    private const double MinSampleIntervalSeconds = 0.001;

    /// <summary>
    /// Estimated average cycles per instruction for 65C02 processor.
    /// This is an approximation as actual CPI varies by instruction type
    /// (e.g., branches 2-4, loads 2-5, stores 3-4, arithmetic 2-7).
    /// </summary>
    private const double EstimatedAverageCpi = 3.0;

    private readonly IMachine machine;
    private readonly IVideoDevice? videoDevice;
    private readonly Stopwatch stopwatch;
    private readonly List<IStatusWindowExtension> extensions = [];

    private ulong lastCycles;
    private long lastSampleTicks;
    private double instructionsPerSecond;
    private double averageCyclesPerInstruction;
    private double actualMHz;
    private double cpuUtilization;
    private DateTime? waiStartTime;

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineStatsProvider"/> class.
    /// </summary>
    /// <param name="machine">The machine to monitor.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="machine"/> is null.</exception>
    public MachineStatsProvider(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);

        this.machine = machine;
        videoDevice = machine.GetComponent<IVideoDevice>();
        stopwatch = Stopwatch.StartNew();

        // Subscribe to state changes to track WAI state
        machine.StateChanged += OnMachineStateChanged;

        // Initialize tracking
        ResetStats();
    }

    /// <inheritdoc/>
    public IMachine Machine => machine;

    /// <inheritdoc/>
    public MachineState State => machine.State;

    /// <inheritdoc/>
    public bool IsWaitingForInterrupt =>
        machine.State == MachineState.Running && machine.Cpu.IsWaitingForInterrupt;

    /// <inheritdoc/>
    public TimeSpan WaiDuration
    {
        get
        {
            if (!IsWaitingForInterrupt || waiStartTime == null)
            {
                return TimeSpan.Zero;
            }

            return DateTime.UtcNow - waiStartTime.Value;
        }
    }

    /// <inheritdoc/>
    public Registers Registers => machine.Cpu.GetRegisters();

    /// <inheritdoc/>
    public ulong TotalCycles => machine.Cpu.GetCycles();

    /// <inheritdoc/>
    public ulong TotalInstructions { get; private set; }

    /// <inheritdoc/>
    public double InstructionsPerSecond => instructionsPerSecond;

    /// <inheritdoc/>
    public double AverageCyclesPerInstruction => averageCyclesPerInstruction;

    /// <inheritdoc/>
    public double ActualMHz => actualMHz;

    /// <inheritdoc/>
    public double TargetMHz => AppleIIeClockMHz;

    /// <inheritdoc/>
    public double CpuUtilization => cpuUtilization;

    /// <inheritdoc/>
    public int SchedulerQueueDepth => machine.Scheduler.PendingEventCount;

    /// <inheritdoc/>
    public ulong? NextEventCycles
    {
        get
        {
            var nextEvent = machine.Scheduler.PeekNextDue();
            if (!nextEvent.HasValue)
            {
                return null;
            }

            var currentTime = machine.Now.Value;

            if (nextEvent.Value.Value > currentTime)
            {
                return nextEvent.Value.Value - currentTime;
            }

            return 0;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<bool> AnnunciatorStates
    {
        get
        {
            if (videoDevice == null)
            {
                return [false, false, false, false];
            }

            return videoDevice.Annunciators;
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<IStatusWindowExtension> Extensions => extensions;

    /// <inheritdoc/>
    public void Sample()
    {
        var currentTicks = stopwatch.ElapsedTicks;
        var currentCycles = TotalCycles;

        // Calculate elapsed time in seconds
        var elapsedTicks = currentTicks - lastSampleTicks;
        var elapsedSeconds = elapsedTicks / (double)Stopwatch.Frequency;

        if (elapsedSeconds > MinSampleIntervalSeconds)
        {
            // Calculate cycles per second
            var deltaCycles = currentCycles - lastCycles;
            var cyclesPerSecond = deltaCycles / elapsedSeconds;

            // Convert to MHz
            actualMHz = cyclesPerSecond / 1_000_000.0;

            // Estimate CPU utilization based on actual vs target speed
            // If running faster than target, cap at 100%
            cpuUtilization = Math.Min(100.0, (actualMHz / TargetMHz) * 100.0);

            // Track WAI state for utilization adjustment
            if (IsWaitingForInterrupt)
            {
                if (waiStartTime == null)
                {
                    waiStartTime = DateTime.UtcNow;
                }

                // When in WAI, cycles don't advance but CPU isn't really "busy"
                cpuUtilization = 0;
            }
            else
            {
                waiStartTime = null;
            }

            // Estimate IPS based on average CPI for 65C02
            instructionsPerSecond = cyclesPerSecond / EstimatedAverageCpi;
            TotalInstructions += (ulong)(instructionsPerSecond * elapsedSeconds);

            // Calculate average CPI
            if (instructionsPerSecond > 0)
            {
                averageCyclesPerInstruction = cyclesPerSecond / instructionsPerSecond;
            }

            // Update tracking
            lastCycles = currentCycles;
            lastSampleTicks = currentTicks;
        }
    }

    /// <inheritdoc/>
    public void ResetStats()
    {
        lastCycles = TotalCycles;
        lastSampleTicks = stopwatch.ElapsedTicks;
        instructionsPerSecond = 0;
        averageCyclesPerInstruction = 0;
        actualMHz = 0;
        cpuUtilization = 0;
        TotalInstructions = 0;
        waiStartTime = null;
    }

    /// <inheritdoc/>
    public void RegisterExtension(IStatusWindowExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);
        extensions.Add(extension);
        extensions.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    private void OnMachineStateChanged(MachineState newState)
    {
        // Reset WAI tracking when machine state changes
        if (newState != MachineState.Running)
        {
            waiStartTime = null;
        }
    }
}