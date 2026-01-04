// <copyright file="Machine.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Core.Interfaces.Signaling;
using BadMango.Emulator.Core.Signaling;

using Interfaces;

/// <summary>
/// Standard implementation of <see cref="IMachine"/> — a running emulator instance with lifecycle management.
/// </summary>
/// <remarks>
/// <para>
/// The machine is a component bucket that holds the CPU, memory bus, devices, and arbitrary
/// components registered during assembly. It provides lifecycle control (Run, Stop, Step, Reset)
/// and event notification when state changes.
/// </para>
/// <para>
/// Machines are created using <see cref="MachineBuilder"/> which handles the complex
/// initialization and wiring of components.
/// </para>
/// </remarks>
public sealed class Machine : IMachine
{
    private readonly List<object> components = [];
    private readonly List<IScheduledDevice> scheduledDevices = [];
    private MachineState state = MachineState.Stopped;

    /// <summary>
    /// Initializes a new instance of the <see cref="Machine"/> class.
    /// </summary>
    /// <param name="cpu">The CPU instance.</param>
    /// <param name="bus">The memory bus.</param>
    /// <param name="scheduler">The event scheduler.</param>
    /// <param name="signals">The signal bus.</param>
    /// <param name="devices">The device registry.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <see langword="null"/>.</exception>
    internal Machine(
        ICpu cpu,
        IMemoryBus bus,
        IScheduler scheduler,
        ISignalBus signals,
        IDeviceRegistry devices)
    {
        ArgumentNullException.ThrowIfNull(cpu, nameof(cpu));
        ArgumentNullException.ThrowIfNull(bus, nameof(bus));
        ArgumentNullException.ThrowIfNull(scheduler, nameof(scheduler));
        ArgumentNullException.ThrowIfNull(signals, nameof(signals));
        ArgumentNullException.ThrowIfNull(devices, nameof(devices));

        Cpu = cpu;
        Bus = bus;
        Scheduler = scheduler;
        Signals = signals;
        Devices = devices;
    }

    /// <inheritdoc />
    public event Action<MachineState>? StateChanged;

    /// <inheritdoc />
    public ICpu Cpu { get; }

    /// <inheritdoc />
    public IMemoryBus Bus { get; }

    /// <inheritdoc />
    public IScheduler Scheduler { get; }

    /// <inheritdoc />
    public ISignalBus Signals { get; }

    /// <inheritdoc />
    public IDeviceRegistry Devices { get; }

    /// <inheritdoc />
    public Cycle Now
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Scheduler.Now;
    }

    /// <inheritdoc />
    public MachineState State => state;

    /// <inheritdoc />
    public void Reset()
    {
        // Stop if running
        if (state == MachineState.Running)
        {
            Cpu.RequestStop();
        }

        // Assert RESET signal
        Signals.Assert(SignalLine.Reset, deviceId: 0, Scheduler.Now);

        // Reset CPU
        Cpu.Reset();

        // Reset scheduler
        Scheduler.Reset();

        // Deassert RESET signal
        Signals.Deassert(SignalLine.Reset, deviceId: 0, Scheduler.Now);

        // Transition to stopped state
        SetState(MachineState.Stopped);
    }

    /// <inheritdoc />
    public void Run()
    {
        if (state == MachineState.Running)
        {
            return;
        }

        SetState(MachineState.Running);
        Cpu.ClearStopRequest();

        // Execute in a loop until stopped
        while (state == MachineState.Running && !Cpu.IsStopRequested)
        {
            var result = Cpu.Step();
            Scheduler.Advance(result.CyclesConsumed);

            // Check for halt conditions
            if (result.State == CpuRunState.Stopped || result.State == CpuRunState.Halted)
            {
                break;
            }
        }

        // If we exited the loop, transition to stopped
        if (state == MachineState.Running)
        {
            SetState(MachineState.Stopped);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (state == MachineState.Stopped)
        {
            return;
        }

        Cpu.RequestStop();
        SetState(MachineState.Stopped);
    }

    /// <inheritdoc />
    public CpuStepResult Step()
    {
        // If running, pause first (request stop to interrupt the run loop)
        if (state == MachineState.Running)
        {
            Cpu.RequestStop();
        }

        var result = Cpu.Step();
        Scheduler.Advance(result.CyclesConsumed);

        // Transition to paused after stepping
        SetState(MachineState.Paused);

        return result;
    }

    /// <inheritdoc />
    public T? GetComponent<T>()
        where T : class
    {
        foreach (var component in components)
        {
            if (component is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public IEnumerable<T> GetComponents<T>()
        where T : class
    {
        foreach (var component in components)
        {
            if (component is T typed)
            {
                yield return typed;
            }
        }
    }

    /// <inheritdoc />
    public bool HasComponent<T>()
        where T : class
    {
        foreach (var component in components)
        {
            if (component is T)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Adds a component to the machine's component bucket.
    /// </summary>
    /// <typeparam name="T">The type of component being added.</typeparam>
    /// <param name="component">The component instance to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="component"/> is <see langword="null"/>.</exception>
    internal void AddComponent<T>(T component)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(component, nameof(component));
        components.Add(component);
    }

    /// <summary>
    /// Adds a scheduled device to the machine for initialization.
    /// </summary>
    /// <param name="device">The device to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="device"/> is <see langword="null"/>.</exception>
    internal void AddScheduledDevice(IScheduledDevice device)
    {
        ArgumentNullException.ThrowIfNull(device, nameof(device));
        scheduledDevices.Add(device);
    }

    /// <summary>
    /// Initializes all scheduled devices with this machine as the event context.
    /// </summary>
    /// <remarks>
    /// This is called by the builder after all components are assembled.
    /// </remarks>
    internal void InitializeDevices()
    {
        foreach (var device in scheduledDevices)
        {
            device.Initialize(this);
        }
    }

    private void SetState(MachineState newState)
    {
        if (state != newState)
        {
            state = newState;
            StateChanged?.Invoke(newState);
        }
    }
}