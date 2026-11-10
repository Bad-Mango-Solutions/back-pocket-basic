// <copyright file="Machine.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Collections.Concurrent;
using System.Diagnostics;

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
/// <para>
/// <b>Threading.</b> The CPU, scheduler, signal bus, bus mappings and devices are single-threaded
/// and owned by the <em>machine thread</em>: the run loop while the machine is running, or the
/// caller while it is quiescent. At most one run loop exists at a time; starting a loop waits for
/// a previous one that is still winding down. Work from other threads goes through
/// <see cref="Post"/>, which runs it inline when the machine is quiescent and otherwise queues it
/// for the run loop to execute at the next instruction boundary (the CPU sees the request through
/// the same cached attention flag it already checks for interrupts). <see cref="Reset"/>,
/// <see cref="PulseReset"/> and <see cref="Halt"/> are marshalled this way. <see cref="Stop"/>,
/// <see cref="Pause"/> and <see cref="Step"/> wait for the run loop to exit, except when they are
/// called from the machine thread itself (for example from a scheduled callback), where they only
/// request the stop and, for <see cref="Step"/>, defer the step until the current one unwinds.
/// </para>
/// </remarks>
public sealed class Machine : IMachine
{
    /// <summary>
    /// Interval in cycles between yield points for async execution.
    /// </summary>
    private const int YieldIntervalCycles = 10000;

    /// <summary>
    /// The signal-bus source id the machine uses for its own RESET source (<see cref="Reset"/>).
    /// </summary>
    private const int MachineResetSourceId = int.MinValue + 1;

    /// <summary>
    /// Number of CPU cycles a RESET pulse holds the line low before releasing it. The
    /// W65C02S requires RESB to be held low for at least two clock cycles.
    /// </summary>
    private const int ResetHoldCycles = 2;

    /// <summary>
    /// The machine whose work the current thread is executing (run loop slice, step, scheduled
    /// callback or posted action), or <see langword="null"/>. Thread-static rather than
    /// <see cref="AsyncLocal{T}"/> so that it is set exactly around synchronous machine work and
    /// cleared across the run loop's awaits (which may resume on another thread), and so it does
    /// not flow into tasks that a callback starts.
    /// </summary>
    [ThreadStatic]
    private static Machine? executingMachine;

    private readonly List<object> components = [];
    private readonly List<IScheduledDevice> scheduledDevices = [];

    /// <summary>
    /// Guards the run-loop lifecycle (<see cref="activeLoopGeneration"/>, state transitions)
    /// and is the monitor that waiters block on until the run loop exits.
    /// </summary>
    private readonly object lifecycleSync = new();

    /// <summary>
    /// Work posted from other threads while a run loop is active.
    /// </summary>
    private readonly ConcurrentQueue<Action> pendingRequests = new();

    /// <summary>
    /// Work that must run once the CPU step currently on the stack has unwound (machine thread only).
    /// </summary>
    private readonly Queue<Action> deferredWork = new();

    /// <summary>
    /// The concrete signal bus, when available, used to raise the CPU's boundary attention flag
    /// for posted requests. <see langword="null"/> for other <see cref="ISignalBus"/> implementations,
    /// in which case the run loop drains requests between slices instead.
    /// </summary>
    private readonly SignalBus? concreteSignals;

    private volatile MachineState state = MachineState.Stopped;

    /// <summary>
    /// Generation of the run loop that currently owns the CPU, or 0 when no loop is active.
    /// Written under <see cref="lifecycleSync"/>.
    /// </summary>
    private int activeLoopGeneration;

    /// <summary>
    /// Last generation handed out (under <see cref="lifecycleSync"/>).
    /// </summary>
    private int lastLoopGeneration;

    /// <summary>
    /// Depth of CPU steps currently on the machine thread's stack (machine thread only). While
    /// non-zero, code running in a scheduled callback or a boundary request must not step the CPU.
    /// </summary>
    private int cpuStepDepth;

    /// <summary>
    /// <see langword="true"/> until the first reset (and again after a hard reset): the next RESET
    /// release performs a power-on CPU reset instead of the warm reset sequence (machine thread only).
    /// </summary>
    private bool powerOnResetPending = true;

    /// <summary>
    /// The scheduled release of a RESET pulse issued while running, if one is pending (machine thread only).
    /// </summary>
    private EventHandle? pendingResetRelease;

    /// <summary>
    /// The release action(s) for <see cref="pendingResetRelease"/> (machine thread only).
    /// </summary>
    private Action? pendingResetReleaseAction;

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
        Signals.SignalChanged += OnSignalChanged;

        concreteSignals = signals as SignalBus;
        concreteSignals?.SetServiceHandler(OnServiceRequested);

        if (scheduler is Scheduler concreteScheduler)
        {
            concreteScheduler.OwnerThreadCheck = IsOwnerThread;
        }
    }

    /// <inheritdoc />
    public event Action<MachineState>? StateChanged;

    /// <summary>
    /// Outcome of one synchronous run-loop slice.
    /// </summary>
    private enum SliceOutcome
    {
        /// <summary>The loop must exit (stop requested, state changed, or the CPU halted).</summary>
        Exit,

        /// <summary>The slice consumed its cycle budget; yield (and throttle) before the next one.</summary>
        Yield,

        /// <summary>The CPU is in WAI with no scheduled events; yield and poll again.</summary>
        Idle,
    }

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
        get => Scheduler.Now;
    }

    /// <inheritdoc />
    public MachineState State => state;

    /// <inheritdoc />
    public long ClockSpeedHz { get; set; }

    /// <inheritdoc />
    public bool IsOnMachineThread => executingMachine == this;

    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action, nameof(action));

        if (executingMachine == this)
        {
            action();
            return;
        }

        lock (lifecycleSync)
        {
            if (activeLoopGeneration != 0)
            {
                pendingRequests.Enqueue(action);
                concreteSignals?.RequestService();
                return;
            }

            // Quiescent: run inline while holding the lifecycle lock so no run loop can
            // start (and take ownership of the CPU) until the action has finished.
            RunAsMachine(action);
        }
    }

    /// <inheritdoc />
    public void Reset(bool hardReset = false)
    {
        PulseResetCore(
            () => Signals.Assert(SignalLine.Reset, MachineResetSourceId, Scheduler.Now),
            () => Signals.Deassert(SignalLine.Reset, MachineResetSourceId, Scheduler.Now),
            hardReset);
    }

    /// <inheritdoc />
    public void PulseReset(Action assertLine, Action releaseLine)
    {
        ArgumentNullException.ThrowIfNull(assertLine, nameof(assertLine));
        ArgumentNullException.ThrowIfNull(releaseLine, nameof(releaseLine));
        PulseResetCore(assertLine, releaseLine, hardReset: false);
    }

    /// <inheritdoc />
    public void Run()
    {
        if (!TryBeginRunLoop(requirePaused: false, out int generation))
        {
            return;
        }

        try
        {
            try
            {
                while (RunSlice(out _) != SliceOutcome.Exit)
                {
                    // Run() is synchronous: no yielding or throttling between slices.
                }
            }
            catch (Exception)
            {
                TransitionFromRunning(MachineState.Stopped);
                throw;
            }

            // If we exited the loop, transition to stopped
            TransitionFromRunning(MachineState.Stopped);
        }
        finally
        {
            EndRunLoop(generation);
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        StopRunLoop();
    }

    /// <inheritdoc />
    public CpuStepResult Step()
    {
        if (executingMachine == this && cpuStepDepth > 0)
        {
            // Called from inside a CPU step (a scheduled callback or a request serviced at an
            // instruction boundary). The CPU cannot be stepped re-entrantly, and waiting for the
            // run loop here would deadlock, so pause the loop and step once this step unwinds.
            TryLeaveRunning(MachineState.Paused, out _);
            deferredWork.Enqueue(() =>
            {
                StepCpu();
                SetState(MachineState.Paused);
            });
            return new(CpuRunState.Running, Cycle.Zero);
        }

        CpuStepResult result;
        bool raisePaused = false;
        lock (lifecycleSync)
        {
            // If running, pause first and wait for the run loop to let go of the CPU. A Resume
            // that slips in while this thread waits is paused again: the step wins.
            while (activeLoopGeneration != 0 && executingMachine != this)
            {
                if (state == MachineState.Running)
                {
                    state = MachineState.Paused;
                    raisePaused = true;
                }

                Cpu.RequestStop();
                Monitor.Wait(lifecycleSync);
            }

            // CPU.Step() advances the scheduler internally — each code path
            // in Cpu65C02.Step() calls Scheduler.Advance(cycles) before returning.
            // Do NOT advance the scheduler again here to avoid double-advancement.
            result = StepCpu();

            // Transition to paused after stepping
            if (state != MachineState.Paused)
            {
                state = MachineState.Paused;
                raisePaused = true;
            }
        }

        if (raisePaused)
        {
            StateChanged?.Invoke(MachineState.Paused);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginRunLoop(requirePaused: false, out int generation))
        {
            return;
        }

        await ExecuteLoopAsync(generation, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Pause()
    {
        if (!TryLeaveRunning(MachineState.Paused, out int generation))
        {
            return;
        }

        WaitForRunLoopToExit(generation);
    }

    /// <inheritdoc />
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        if (!TryBeginRunLoop(requirePaused: true, out int generation))
        {
            return;
        }

        await ExecuteLoopAsync(generation, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task BootAsync(CancellationToken cancellationToken = default)
    {
        Reset();
        await RunAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Halt()
    {
        // Force CPU into halted state (equivalent to STP). The halt reason is CPU state, so it
        // is applied on the machine thread (immediately when quiescent, otherwise at the next
        // instruction boundary or when the loop exits).
        Post(() => Cpu.HaltReason = HaltState.Stp);
        StopRunLoop();
    }

    /// <inheritdoc />
    public T? GetComponent<T>()
        where T : class
    {
        return components.OfType<T>().FirstOrDefault();
    }

    /// <inheritdoc />
    public IEnumerable<T> GetComponents<T>()
        where T : class
    {
        return components.OfType<T>();
    }

    /// <inheritdoc />
    public bool HasComponent<T>()
        where T : class
    {
        return components.OfType<T>().Any();
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
    /// <para>
    /// This is called once by the builder after all components are assembled.
    /// Devices use this to store references to system services and schedule
    /// their initial events.
    /// </para>
    /// <para>
    /// For subsequent power-on (hard) resets, <see cref="PowerOnResetDevices"/> calls each
    /// device's <see cref="IPeripheral.Reset"/> method instead, returning it to power-on
    /// state and rescheduling events without repeating one-time initialization (ROM
    /// registration, swap group lookup, etc.).
    /// </para>
    /// </remarks>
    internal void InitializeDevices()
    {
        foreach (var device in scheduledDevices)
        {
            device.Initialize(this);
        }
    }

    /// <summary>
    /// Returns every scheduled device and installed slot card to its power-on state, each exactly once.
    /// </summary>
    /// <param name="cardsAlreadyReset">
    /// Slot cards that already received their power-on <see cref="IPeripheral.Reset"/> as their
    /// RESET-signal response from the slot manager, and must not be reset a second time.
    /// </param>
    /// <remarks>
    /// <para>
    /// Called by a hard reset while the RESET line is held, after the scheduler flush, so devices
    /// reschedule their periodic events (e.g., VideoDevice reschedules VBLANK).
    /// </para>
    /// <para>
    /// This is distinct from <see cref="InitializeDevices"/>, which performs
    /// one-time setup during machine construction and should not be called again.
    /// </para>
    /// </remarks>
    private void PowerOnResetDevices(IReadOnlyCollection<ISlotCard> cardsAlreadyReset)
    {
        var done = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var card in cardsAlreadyReset)
        {
            done.Add(card);
        }

        foreach (var device in scheduledDevices)
        {
            if (device is IPeripheral peripheral && done.Add(peripheral))
            {
                peripheral.Reset();
            }
        }

        if (GetComponent<ISlotManager>() is { } slotManager)
        {
            foreach (var card in slotManager.Slots.Values)
            {
                if (done.Add(card))
                {
                    card.Reset();
                }
            }
        }
    }

    private void ClearHardResetMemory()
    {
        bool anyRegistered = false;
        foreach (var memory in components.OfType<IHardResetMemory>())
        {
            memory.ClearForHardReset();
            anyRegistered = true;
        }

        // Fallback for minimal machines that do not yet expose direct RAM components.
        if (!anyRegistered)
        {
            Bus.Clear();
        }
    }

    /// <summary>
    /// Marshals a RESET pulse (assert, hold, release) onto the machine thread.
    /// </summary>
    /// <param name="assertLine">Drives the RESET line low through some source.</param>
    /// <param name="releaseLine">Releases that source.</param>
    /// <param name="hardReset">Whether to also perform a power-on (hard) reset while the line is held.</param>
    private void PulseResetCore(Action assertLine, Action releaseLine, bool hardReset)
    {
        Post(() => ExecuteResetPulse(assertLine, releaseLine, hardReset));
    }

    /// <summary>
    /// Performs a RESET pulse on the machine thread.
    /// </summary>
    /// <param name="assertLine">Drives the RESET line low through some source.</param>
    /// <param name="releaseLine">Releases that source.</param>
    /// <param name="hardReset">Whether to also perform a power-on (hard) reset while the line is held.</param>
    /// <remarks>
    /// <para>
    /// Reset works like the real button: it never changes the run state. When the machine is
    /// running (this executes inside the run loop), the line is held for
    /// <see cref="ResetHoldCycles"/> CPU cycles by a scheduled release and the CPU then runs its
    /// reset sequence and continues into the ROM reset handler. When the machine is stopped or
    /// paused, the hold window and the reset sequence are stepped here and execution stops with
    /// PC at the reset vector.
    /// </para>
    /// </remarks>
    private void ExecuteResetPulse(Action assertLine, Action releaseLine, bool hardReset)
    {
        bool lineWasHeld = Signals.IsAsserted(SignalLine.Reset);
        assertLine();

        if (hardReset)
        {
            // Cards the slot manager power-on reset as their RESET response (only when the
            // line transitioned just now) must not be reset again.
            IReadOnlyCollection<ISlotCard> cardsAlreadyReset = !lineWasHeld && Signals.IsAsserted(SignalLine.Reset)
                ? GetSlotCardsResetBySignal()
                : [];
            ClearHardResetMemory();
            PowerOnResetDevices(cardsAlreadyReset);
            powerOnResetPending = true;
        }

        if (cpuStepDepth > 0 && activeLoopGeneration != 0 && state == MachineState.Running)
        {
            // Inside the running loop: let the CPU hold through the window, then release.
            ScheduleResetRelease(releaseLine);
            return;
        }

        if (cpuStepDepth > 0)
        {
            // Inside a step that is not part of a running loop (e.g., a callback during Step()
            // or while the loop winds down): finish the pulse once that step has unwound.
            deferredWork.Enqueue(() => CompleteResetPulse(releaseLine));
            return;
        }

        CompleteResetPulse(releaseLine);
    }

    /// <summary>
    /// Steps the hold window, releases the line and steps the reset sequence (machine thread, no CPU step on the stack).
    /// </summary>
    /// <param name="releaseLine">Releases the source that is holding the line.</param>
    private void CompleteResetPulse(Action releaseLine)
    {
        cpuStepDepth++;
        try
        {
            for (int i = 0; i < ResetHoldCycles; i++)
            {
                Cpu.Step();
            }

            bool powerOn = powerOnResetPending;
            releaseLine();

            // Advance exactly the reset release sequence and stop there. A power-on reset
            // (first reset or --hard) already initialized the CPU when the line released.
            if (!powerOn && !Signals.IsAsserted(SignalLine.Reset))
            {
                Cpu.Step();
            }
        }
        finally
        {
            cpuStepDepth--;
        }
    }

    private void ScheduleResetRelease(Action releaseLine)
    {
        // A second pulse during the hold window extends the hold and releases every source together.
        if (pendingResetRelease is { } previous)
        {
            Scheduler.Cancel(previous);
        }

        pendingResetReleaseAction += releaseLine;
        pendingResetRelease = Scheduler.ScheduleAfter(
            ResetHoldCycles,
            ScheduledEventKind.DeviceTimer,
            priority: int.MinValue,
            _ => FirePendingResetRelease(),
            tag: "reset-release");
    }

    private void FirePendingResetRelease()
    {
        var release = pendingResetReleaseAction;
        pendingResetRelease = null;
        pendingResetReleaseAction = null;
        release?.Invoke();
    }

    /// <summary>
    /// Completes a RESET pulse whose scheduled release has not fired when the run loop exits,
    /// so a reset is never left half-done (line held, PC not at the vector) once stopped.
    /// </summary>
    private void FinishPendingResetRelease()
    {
        if (pendingResetRelease is not { } handle)
        {
            return;
        }

        Scheduler.Cancel(handle);
        var release = pendingResetReleaseAction;
        pendingResetRelease = null;
        pendingResetReleaseAction = null;
        if (release is not null)
        {
            CompleteResetPulse(release);
        }
    }

    private List<ISlotCard> GetSlotCardsResetBySignal()
    {
        var cards = new List<ISlotCard>();
        if (GetComponent<ISlotManager>() is IResetReceiver and ISlotManager slotManager)
        {
            foreach (var card in slotManager.Slots.Values)
            {
                if (card is not IResetReceiver)
                {
                    cards.Add(card);
                }
            }
        }

        return cards;
    }

    private void OnSignalChanged(SignalLine line, bool asserted, int deviceId, Cycle cycle)
    {
        if (line != SignalLine.Reset)
        {
            return;
        }

        Debug.Assert(IsOwnerThread(), "RESET line changed off the machine thread while the machine is running.");

        if (asserted)
        {
            // Flush outstanding device work first; receivers then reset and reschedule.
            Scheduler.FlushPending();
            NotifyResetReceivers(asserted: true, cycle);
        }
        else
        {
            // Receivers (MMU, language card, slots) restore their post-reset state before
            // the CPU fetches the vector at its next instruction boundary.
            NotifyResetReceivers(asserted: false, cycle);

            if (powerOnResetPending)
            {
                // First reset after power-on (or a hard reset): initialize the CPU to its
                // power-on register state and load PC from the vector through the post-reset
                // banking. This replaces the warm reset sequence.
                powerOnResetPending = false;
                Cpu.Reset();
            }
        }
    }

    /// <summary>
    /// Executes the main run loop asynchronously with cycle-accurate throttling.
    /// </summary>
    /// <param name="generation">The generation token of this loop (from <see cref="TryBeginRunLoop"/>).</param>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>A task that completes when the loop exits.</returns>
    /// <remarks>
    /// <para>
    /// When <see cref="ClockSpeedHz"/> is set to a positive value, the loop paces
    /// CPU execution so that emulated cycles match wall-clock time at the configured
    /// clock rate. For example, at 1,020,484 Hz (Apple IIe speed), the loop ensures
    /// that 1,020,484 emulated cycles elapse per second of real time.
    /// </para>
    /// <para>
    /// Throttling uses a <see cref="Stopwatch"/> to measure elapsed wall-clock time
    /// and inserts delays when the CPU runs ahead of real time. The delay is applied
    /// at each yield point (every <see cref="YieldIntervalCycles"/> cycles), providing
    /// smooth pacing without excessive timer overhead.
    /// </para>
    /// <para>
    /// Instructions run in synchronous slices (<see cref="RunSlice"/>) that mark the thread as
    /// the machine thread; the marker is cleared across each await because the continuation
    /// may resume on a different thread pool thread. Cancellation is observed between slices.
    /// </para>
    /// </remarks>
    private async Task ExecuteLoopAsync(int generation, CancellationToken cancellationToken)
    {
        try
        {
            var throttleStopwatch = Stopwatch.StartNew();
            long throttleCycleBase = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                var outcome = RunSlice(out long sliceCycles);
                if (outcome == SliceOutcome.Exit)
                {
                    break;
                }

                if (outcome == SliceOutcome.Idle)
                {
                    // No events pending - yield to allow external interrupt injection
                    await Task.Yield();
                    continue;
                }

                // Yield periodically to keep the caller responsive
                throttleCycleBase += sliceCycles;

                // Apply throttling if a clock speed is configured
                long clockSpeed = ClockSpeedHz;
                if (clockSpeed > 0)
                {
                    // Calculate how many milliseconds should have elapsed for the
                    // cycles we've executed since the throttle window started
                    double expectedMs = throttleCycleBase * 1000.0 / clockSpeed;
                    double actualMs = throttleStopwatch.Elapsed.TotalMilliseconds;
                    double aheadMs = expectedMs - actualMs;

                    if (aheadMs >= 1.0)
                    {
                        // CPU is ahead of real time — delay to catch up
                        await Task.Delay((int)aheadMs, cancellationToken).ConfigureAwait(false);
                    }
                    else if (aheadMs < -100.0)
                    {
                        // CPU fell significantly behind (e.g., debugger pause, GC stall).
                        // Reset the throttle window to avoid a burst of catch-up cycles.
                        throttleStopwatch.Restart();
                        throttleCycleBase = 0;
                    }
                }

                await Task.Yield();
            }

            // If we exited the loop, transition to appropriate state
            TransitionFromRunning(cancellationToken.IsCancellationRequested ? MachineState.Paused : MachineState.Stopped);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Task.Delay honors the token; treat cancellation as a pause rather than
            // leaving the machine stuck in Running if the exception escaped the loop.
            TransitionFromRunning(MachineState.Paused);
        }
        catch (Exception)
        {
            // An exception from Cpu.Step or a scheduled device callback (e.g. VBlank)
            // used to unwind the loop while state stayed Running. Leave the machine
            // Stopped (there is no Faulted state) and rethrow so BootCommand /
            // ResumeCommand still surface "Boot error:" / "Resume error:".
            TransitionFromRunning(MachineState.Stopped);
            throw;
        }
        finally
        {
            EndRunLoop(generation);
        }
    }

    /// <summary>
    /// Runs instructions on the calling thread until the slice budget is used, the CPU idles in
    /// WAI with nothing scheduled, or the loop must exit.
    /// </summary>
    /// <param name="cycles">The cycles consumed by instructions in this slice.</param>
    /// <returns>Why the slice ended.</returns>
    private SliceOutcome RunSlice(out long cycles)
    {
        cycles = 0;
        var previous = executingMachine;
        executingMachine = this;
        cpuStepDepth++;
        try
        {
            // A CPU that services requests at instruction boundaries (through
            // SignalBus.ServiceRequests) picks them up within one instruction; slice
            // boundaries are the fallback for CPUs or signal buses that do not.
            if (!pendingRequests.IsEmpty)
            {
                DrainRequests();
            }

            // Execute in a loop until stopped
            while (state == MachineState.Running && !Cpu.IsStopRequested)
            {
                var result = Cpu.Step();

                // Handle WAI state specially - use scheduler to advance time efficiently
                if (result.State == CpuRunState.WaitingForInterrupt)
                {
                    // When CPU is waiting for interrupt, jump to next scheduled event
                    // This allows devices (video refresh, timers, etc.) to fire and potentially
                    // trigger interrupts that will wake the CPU from WAI state
                    if (!Scheduler.JumpToNextEventAndDispatch())
                    {
                        // No events pending - need to wait for external interrupt
                        return SliceOutcome.Idle;
                    }

                    // After dispatching event, loop will call Step() again
                    // If an interrupt was signaled, Step() will process it and clear WAI state
                    continue;
                }

                // Check for halt conditions (STP instruction or true halt)
                if (result.State == CpuRunState.Stopped || result.State == CpuRunState.Halted)
                {
                    return SliceOutcome.Exit;
                }

                cycles += (long)result.CyclesConsumed.Value;
                if (cycles >= YieldIntervalCycles)
                {
                    return SliceOutcome.Yield;
                }
            }

            return SliceOutcome.Exit;
        }
        finally
        {
            cpuStepDepth--;
            executingMachine = previous;
        }
    }

    /// <summary>
    /// Steps the CPU once on the calling thread as the machine thread, then runs work deferred
    /// by callbacks during that step.
    /// </summary>
    /// <returns>The step result.</returns>
    private CpuStepResult StepCpu()
    {
        var previous = executingMachine;
        executingMachine = this;
        try
        {
            CpuStepResult result;
            cpuStepDepth++;
            try
            {
                result = Cpu.Step();
            }
            finally
            {
                cpuStepDepth--;
            }

            if (cpuStepDepth == 0)
            {
                DrainDeferredWork();
            }

            return result;
        }
        finally
        {
            executingMachine = previous;
        }
    }

    /// <summary>
    /// Invoked by the CPU at an instruction boundary when a service request is pending.
    /// </summary>
    private void OnServiceRequested()
    {
        if (pendingRequests.IsEmpty)
        {
            return;
        }

        if (executingMachine != this || state != MachineState.Running)
        {
            // Either the CPU is being stepped outside the machine's control, or the run loop is
            // winding down (Stop/Pause requested). Leave the requests for the loop exit, which
            // runs them with no CPU step on the stack.
            concreteSignals?.RequestService();
            return;
        }

        DrainRequests();
    }

    private void DrainRequests()
    {
        while (pendingRequests.TryDequeue(out var request))
        {
            request();
        }
    }

    private void DrainDeferredWork()
    {
        while (deferredWork.TryDequeue(out var work))
        {
            work();
        }
    }

    private void NotifyResetReceivers(bool asserted, Cycle cycle)
    {
        foreach (var receiver in CollectResetReceivers())
        {
            if (asserted)
            {
                receiver.OnResetAsserted(cycle);
            }
            else
            {
                receiver.OnResetReleased(cycle);
            }
        }
    }

    /// <summary>
    /// Collects the RESET receivers in delivery order, each exactly once.
    /// </summary>
    /// <returns>The receivers: components first, then scheduled devices.</returns>
    /// <remarks>
    /// Slot cards installed in a slot manager that is itself a receiver are excluded: on the
    /// IIe the line reaches each card through slot pin 31, so the slot manager delivers RESET
    /// to its cards (including cards added with <c>WithCard</c>, which are not scheduled
    /// devices). Profile-built cards are also scheduled devices, and would otherwise be
    /// notified twice.
    /// </remarks>
    private List<IResetReceiver> CollectResetReceivers()
    {
        HashSet<ISlotCard>? cardsViaSlots = null;
        if (GetComponent<ISlotManager>() is IResetReceiver and ISlotManager slotManager)
        {
            cardsViaSlots = new HashSet<ISlotCard>(slotManager.Slots.Values, ReferenceEqualityComparer.Instance);
        }

        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        var receivers = new List<IResetReceiver>();
        foreach (var candidate in components.Concat<object>(scheduledDevices))
        {
            if (candidate is IResetReceiver receiver
                && !(candidate is ISlotCard card && cardsViaSlots is not null && cardsViaSlots.Contains(card))
                && seen.Add(candidate))
            {
                receivers.Add(receiver);
            }
        }

        return receivers;
    }

    /// <summary>
    /// Claims the CPU for a new run loop.
    /// </summary>
    /// <param name="requirePaused">
    /// <see langword="true"/> for resume (only from <see cref="MachineState.Paused"/>);
    /// <see langword="false"/> for run (from any state other than <see cref="MachineState.Running"/>).
    /// </param>
    /// <param name="generation">The new loop's generation token.</param>
    /// <returns><see langword="true"/> if a loop may start; <see langword="false"/> if the call is a no-op.</returns>
    /// <remarks>
    /// Waits for a previous loop that has been asked to stop but has not exited yet, so two loops
    /// never drive one CPU. Refuses to start a loop from the machine thread itself (a scheduled
    /// callback or posted action), which would nest the run loop inside a CPU step.
    /// </remarks>
    private bool TryBeginRunLoop(bool requirePaused, out int generation)
    {
        generation = 0;
        if (executingMachine == this)
        {
            return false;
        }

        bool raiseRunning = false;
        lock (lifecycleSync)
        {
            while (true)
            {
                bool allowed = requirePaused ? state == MachineState.Paused : state != MachineState.Running;
                if (!allowed)
                {
                    return false;
                }

                if (activeLoopGeneration == 0)
                {
                    break;
                }

                // A previous loop is winding down; wait for it to release the CPU.
                Monitor.Wait(lifecycleSync);
            }

            generation = ++lastLoopGeneration;
            activeLoopGeneration = generation;
            Cpu.ClearStopRequest();
            if (state != MachineState.Running)
            {
                state = MachineState.Running;
                raiseRunning = true;
            }
        }

        if (raiseRunning)
        {
            StateChanged?.Invoke(MachineState.Running);
        }

        return true;
    }

    /// <summary>
    /// Releases the CPU at the end of a run loop.
    /// </summary>
    /// <param name="generation">The generation of the loop that is exiting.</param>
    /// <remarks>
    /// Before releasing, completes a RESET pulse still in its hold window and runs requests that
    /// were posted while the loop was winding down, with no CPU step on the stack. A stale loop
    /// (generation mismatch) never clears another loop's ownership.
    /// </remarks>
    private void EndRunLoop(int generation)
    {
        lock (lifecycleSync)
        {
            if (activeLoopGeneration != generation)
            {
                return;
            }

            try
            {
                RunAsMachine(() =>
                {
                    FinishPendingResetRelease();
                    DrainRequests();
                    DrainDeferredWork();
                });
            }
            finally
            {
                activeLoopGeneration = 0;
                Monitor.PulseAll(lifecycleSync);
            }
        }
    }

    /// <summary>
    /// Waits until the run loop with the given generation has exited.
    /// </summary>
    /// <param name="generation">The loop to wait for (0 when none was active).</param>
    /// <remarks>
    /// Only that loop is waited for: if another thread resumes the machine meanwhile, the new
    /// loop is that thread's, and waiting for it would block until someone else stops it.
    /// </remarks>
    private void WaitForRunLoopToExit(int generation)
    {
        // Never wait for the loop from inside it (e.g., Stop() from a scheduled callback):
        // the stop request is enough, and the loop exits after the current instruction.
        if (generation == 0 || executingMachine == this)
        {
            return;
        }

        lock (lifecycleSync)
        {
            while (activeLoopGeneration == generation)
            {
                Monitor.Wait(lifecycleSync);
            }
        }
    }

    /// <summary>
    /// Moves the machine to <see cref="MachineState.Stopped"/>, requests the CPU to stop, and
    /// waits for the active run loop (if any) to exit.
    /// </summary>
    private void StopRunLoop()
    {
        int generation;
        bool changed;
        lock (lifecycleSync)
        {
            if (state == MachineState.Stopped && activeLoopGeneration == 0)
            {
                return;
            }

            changed = state != MachineState.Stopped;
            state = MachineState.Stopped;

            // Under the lock, so a concurrent Run/Resume (which clears the stop request under
            // the same lock) cannot have its fresh loop stopped by this request.
            Cpu.RequestStop();
            generation = activeLoopGeneration;
        }

        if (changed)
        {
            StateChanged?.Invoke(MachineState.Stopped);
        }

        WaitForRunLoopToExit(generation);
    }

    /// <summary>
    /// Atomically moves the machine out of <see cref="MachineState.Running"/> and requests the
    /// CPU to stop.
    /// </summary>
    /// <param name="newState">The state to enter.</param>
    /// <param name="generation">The run loop that was active, to wait for.</param>
    /// <returns><see langword="true"/> if the machine was running.</returns>
    private bool TryLeaveRunning(MachineState newState, out int generation)
    {
        lock (lifecycleSync)
        {
            generation = activeLoopGeneration;
            if (state != MachineState.Running)
            {
                return false;
            }

            state = newState;
            Cpu.RequestStop();
        }

        StateChanged?.Invoke(newState);
        return true;
    }

    private void RunAsMachine(Action action)
    {
        var previous = executingMachine;
        executingMachine = this;
        try
        {
            action();
        }
        finally
        {
            executingMachine = previous;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when the calling thread may mutate machine state: it is the
    /// machine thread, or no run loop is active (the machine is quiescent).
    /// </summary>
    /// <returns>Whether the caller owns the machine's single-threaded components.</returns>
    private bool IsOwnerThread()
    {
        return executingMachine == this || Volatile.Read(ref activeLoopGeneration) == 0;
    }

    /// <summary>
    /// Atomically moves the machine out of <see cref="MachineState.Running"/>.
    /// </summary>
    /// <param name="newState">The state to enter.</param>
    /// <returns><see langword="true"/> if the machine was running and the state changed.</returns>
    private bool TransitionFromRunning(MachineState newState)
    {
        lock (lifecycleSync)
        {
            if (state != MachineState.Running)
            {
                return false;
            }

            state = newState;
        }

        StateChanged?.Invoke(newState);
        return true;
    }

    private void SetState(MachineState newState)
    {
        bool changed;
        lock (lifecycleSync)
        {
            changed = state != newState;
            state = newState;
        }

        if (changed)
        {
            StateChanged?.Invoke(newState);
        }
    }
}