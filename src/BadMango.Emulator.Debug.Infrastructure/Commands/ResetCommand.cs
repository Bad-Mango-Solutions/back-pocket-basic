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
    public override string Usage => "reset [--hard]";

    /// <inheritdoc/>
    public string Synopsis => "reset [--hard]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Pulses the RESET line like Ctrl-Reset: devices respond to RESET and the CPU runs " +
        "its reset sequence through the reset vector ($FFFC-$FFFD), setting I and clearing D. " +
        "RAM is preserved, and a running machine keeps running into the ROM reset handler; " +
        "a stopped or paused machine stops at the reset vector. With --hard, also performs a " +
        "power-on reset of the CPU and devices and clears memory to zeros.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--hard", "-h", "flag", "Also clear memory to zeros", "off"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "reset                   Soft reset (like Ctrl-Reset; RAM preserved)",
        "reset --hard            Hard reset (CPU + memory clear)",
        "rst                     Alias for reset",
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

        if (debugContext.Machine is { } machine)
        {
            if (!hardReset && machine.GetComponent<IKeyboardDevice>() is { } keyboard)
            {
                TriggerKeyboardReset(machine, keyboard);
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
    /// <remarks>
    /// Both callbacks run on the machine thread (see <see cref="IMachine.PulseReset"/>), so the
    /// modifiers sampled here are the ones the machine sees when the key goes down.
    /// </remarks>
    private static void TriggerKeyboardReset(IMachine machine, IKeyboardDevice keyboard)
    {
        var originalModifiers = KeyboardModifiers.None;
        machine.PulseReset(
            () =>
            {
                originalModifiers = keyboard.Modifiers;
                keyboard.SetModifiers(originalModifiers | KeyboardModifiers.Control);
                keyboard.PressResetKey();
            },
            () =>
            {
                keyboard.ReleaseResetKey();
                keyboard.SetModifiers(originalModifiers);
            });
    }
}