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
/// When a machine is attached, reset goes through the machine lifecycle
/// (signals, scheduler, devices, slots, CPU). By default, this is a soft reset
/// that preserves RAM. With the --hard flag, RAM backing stores are cleared.
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
        "Performs a CPU reset, setting PC to the reset vector ($FFFC-$FFFD), " +
        "initializing processor status flags, and clearing the halt state. " +
        "By default performs a soft reset (CPU only). With --hard, also clears " +
        "all memory to zeros.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--hard", "-h", "flag", "Also clear memory to zeros", "off"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "reset                   Soft reset (CPU only)",
        "reset --hard            Hard reset (CPU + memory clear)",
        "rst                     Alias for reset",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Resets CPU registers to initial state. With --hard, also clears all memory.";

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

        if (debugContext.Machine is not null)
        {
            if (!hardReset && debugContext.Machine.GetComponent<IKeyboardDevice>() is { } keyboard)
            {
                TriggerKeyboardReset(debugContext.Machine, keyboard);
            }
            else
            {
                debugContext.Machine.Reset(hardReset);
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

    private static void TriggerKeyboardReset(IMachine machine, IKeyboardDevice keyboard)
    {
        var originalModifiers = keyboard.Modifiers;
        keyboard.SetModifiers(originalModifiers | KeyboardModifiers.Control);
        keyboard.PressResetKey();

        if (machine.State == MachineState.Running)
        {
            machine.Scheduler.ScheduleAfter(
                2,
                ScheduledEventKind.DeviceTimer,
                int.MinValue,
                _ =>
                {
                    keyboard.ReleaseResetKey();
                    keyboard.SetModifiers(originalModifiers);
                },
                "debug-reset-release");
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            machine.Cpu.Step();
        }

        keyboard.ReleaseResetKey();
        keyboard.SetModifiers(originalModifiers);
        machine.Cpu.Step();
    }
}