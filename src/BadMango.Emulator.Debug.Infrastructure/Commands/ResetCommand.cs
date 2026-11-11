// <copyright file="ResetCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Resets the machine or CPU to its initial state.
/// </summary>
/// <remarks>
/// <para>
/// Performs a reset, which:
/// - Sets PC to the reset vector ($FFFC-$FFFD).
/// - Initializes processor status flags.
/// - Clears the halt state.
/// </para>
/// <para>
/// When a machine is attached, a soft reset pulses the RESET line through the keyboard's
/// Reset key (as Ctrl-Reset would) or, without a keyboard, through the machine's own RESET
/// source. Devices respond to RESET, RAM is preserved, and the run state is unchanged: a
/// running machine continues into the ROM reset handler, while a stopped or paused machine
/// stops with PC at the reset vector. The pulse is marshalled onto the machine thread, so the
/// command is safe to use while the machine is running. With the --hard flag, the machine
/// also performs a power-on reset of the CPU and devices and clears memory.
/// </para>
/// <para>
/// If no machine is attached, reset falls back to CPU-only behavior and uses
/// the attached bus for --hard memory clear when available.
/// </para>
/// </remarks>
public sealed class ResetCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetCommand"/> class.
    /// </summary>
    public ResetCommand()
        : base("reset", "Reset the CPU (soft or hard)")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["rst"];

    /// <inheritdoc/>
    public override string Usage => "reset [--hard] [--open-apple|-o] [--closed-apple|-a]";

    /// <inheritdoc/>
    public string Synopsis => "reset [--hard] [--open-apple|--closed-apple]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Pulses the RESET line like Ctrl-Reset: Control is implied and devices respond to RESET " +
        "while the CPU runs its reset sequence through the reset vector ($FFFC-$FFFD), setting I " +
        "and clearing D. RAM is preserved, and a running machine keeps running into the ROM reset " +
        "handler; a stopped or paused machine stops at the reset vector. With --hard, also performs " +
        "a power-on reset of the CPU and devices and clears memory to zeros. With --open-apple / " +
        "--reboot / --cold, or --closed-apple / --diag, the corresponding Apple key is held through " +
        "the ROM reset sampling window for a short time after RESET releases, matching the hardware " +
        "behavior of the Apple IIe reset handler.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--hard", "-h", "flag", "Also clear memory to zeros", "off"),
        new("--open-apple", "-o", "flag", "Hold Open Apple (PB0) through the ROM reset sampling window; aliases: --command --reboot --cold", "off"),
        new("--closed-apple", "-a", "flag", "Hold Closed Apple (PB1) through the ROM reset sampling window; aliases: --option --diag", "off"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "reset                        Soft reset (like Ctrl-Reset; RAM preserved)",
        "reset --hard                 Hard reset (CPU + memory clear)",
        "reset --open-apple           Soft reset while holding Open Apple (cold reboot)",
        "reset --closed-apple         Soft reset while holding Closed Apple (self-test)",
        "reset --cold                 Alias for holding Open Apple through reset",
        "rst                          Alias for reset",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Resets devices and runs the CPU reset sequence. With --hard, also re-initializes the CPU " +
        "and devices to their power-on state and clears all memory.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["run", "step", "stop", "regs"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Cpu is null)
        {
            return CommandResult.Error("No CPU attached to debug context.");
        }

        bool hardReset = args.Any(arg =>
            arg.Equals("--hard", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-h", StringComparison.OrdinalIgnoreCase));
        bool holdOpenApple = args.Any(arg =>
            arg.Equals("--open-apple", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--command", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--reboot", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--cold", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-o", StringComparison.OrdinalIgnoreCase));
        bool holdClosedApple = args.Any(arg =>
            arg.Equals("--closed-apple", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--option", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("--diag", StringComparison.OrdinalIgnoreCase) ||
            arg.Equals("-a", StringComparison.OrdinalIgnoreCase));

        if (debugContext.Machine is { } machine)
        {
            if (machine.GetComponent<IKeyboardDevice>() is { } keyboard)
            {
                TriggerKeyboardReset(machine, keyboard, holdOpenApple, holdClosedApple, hardReset);
            }
            else
            {
                machine.Reset(hardReset);
            }

            if (machine.State == MachineState.Running)
            {
                // The reset is applied by the run loop; the CPU's registers belong to it now.
                debugContext.Output.WriteLine(hardReset ? "Hard reset signalled; machine is running." : "Soft reset signalled; machine is running.");
                return CommandResult.Ok();
            }
        }
        else
        {
            if (hardReset && debugContext.Bus is not null)
            {
                debugContext.Bus.Clear();
                debugContext.Output.WriteLine("Memory cleared.");
            }

            debugContext.Cpu.Reset();
            debugContext.Cpu.ClearStopRequest();
        }

        var pc = debugContext.Cpu.GetPC();
        debugContext.Output.WriteLine($"CPU reset. PC = ${pc:X4}");

        if (hardReset)
        {
            debugContext.Output.WriteLine("Hard reset completed.");
        }
        else
        {
            debugContext.Output.WriteLine("Soft reset completed.");
        }

        return CommandResult.Ok();
    }

    /// <summary>
    /// Pulses RESET through the keyboard's Reset key, holding Control for the duration of the pulse.
    /// </summary>
    /// <param name="machine">The machine to reset.</param>
    /// <param name="keyboard">The keyboard whose Reset key drives the line.</param>
    /// <param name="holdOpenApple"><see langword="true"/> to hold Open Apple through the ROM reset sampling window.</param>
    /// <param name="holdClosedApple"><see langword="true"/> to hold Closed Apple through the ROM reset sampling window.</param>
    /// <param name="hardReset"><see langword="true"/> to treat the pulse as a power-on reset while the line is held.</param>
    /// <remarks>
    /// The Apple-key bits are intentionally left active after RESET deasserts so the ROM's reset
    /// handler can sample them before the temporary reset-only flags are cleared. The release callback
    /// restores Control and releases the Reset key, but only the reset-added Apple keys are released
    /// later on a short scheduler-based delay that preserves any user-held modifiers present in the
    /// meantime.
    /// </remarks>
    private static void TriggerKeyboardReset(IMachine machine, IKeyboardDevice keyboard, bool holdOpenApple, bool holdClosedApple, bool hardReset)
    {
        KeyboardModifiers originalModifiers = KeyboardModifiers.None;
        KeyboardModifiers resetOnlyModifiers = KeyboardModifiers.None;
        machine.PulseReset(
            () =>
            {
                originalModifiers = keyboard.Modifiers;
                var resetModifiers = originalModifiers | KeyboardModifiers.Control;
                if (holdOpenApple)
                {
                    resetModifiers |= KeyboardModifiers.OpenApple;
                    resetOnlyModifiers |= KeyboardModifiers.OpenApple;
                }

                if (holdClosedApple)
                {
                    resetModifiers |= KeyboardModifiers.ClosedApple;
                    resetOnlyModifiers |= KeyboardModifiers.ClosedApple;
                }

                keyboard.SetModifiers(resetModifiers);
                keyboard.PressResetKey();
            },
            () =>
            {
                keyboard.ReleaseResetKey();
                keyboard.SetModifiers(originalModifiers | KeyboardModifiers.Control);

                if (resetOnlyModifiers == KeyboardModifiers.None)
                {
                    return;
                }

                var releaseCycleDelay = GetResetAppleReleaseDelayCycles(machine);
                machine.Scheduler.ScheduleAt(
                    machine.Scheduler.Now + releaseCycleDelay,
                    ScheduledEventKind.DeferredWork,
                    priority: 0,
                    _ =>
                    {
                        var pendingModifiers = keyboard.Modifiers;
                        pendingModifiers &= ~resetOnlyModifiers;
                        keyboard.SetModifiers(pendingModifiers);
                    },
                    tag: "reset-apple-release");
            },
            hardReset);
    }

    private static ulong GetResetAppleReleaseDelayCycles(IMachine machine)
    {
        const double AppleResetSamplingWindowMs = 500.0;
        long clockSpeedHz = machine.ClockSpeedHz > 0 ? machine.ClockSpeedHz : 1_000_000;
        return (ulong)Math.Max(1, (AppleResetSamplingWindowMs * clockSpeedHz) / 1000.0);
    }
}