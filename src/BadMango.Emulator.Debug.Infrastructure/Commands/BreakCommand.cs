// <copyright file="BreakCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

/// <summary>
/// Manages execution breakpoints backed by the trap registry.
/// </summary>
/// <remarks>
/// Breakpoints register a <see cref="Bus.TrapOperation.Call"/> trap that requests
/// the CPU to stop after the instruction at the breakpoint address completes.
/// Use this with <c>run</c> to diagnose execution that reaches a specific PC.
/// </remarks>
public sealed class BreakCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BreakCommand"/> class.
    /// </summary>
    public BreakCommand()
        : base("bp", "Manage execution breakpoints")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["breakpoint"];

    /// <inheritdoc/>
    public override string Usage => "bp <add|remove|list|clear|enable|disable> [address] [label]";

    /// <inheritdoc/>
    public string Synopsis => "bp <add|remove|list|clear|enable|disable> [address] [label]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Adds, removes, lists, enables, or disables execution breakpoints. " +
        "Breakpoints fire when the CPU fetches an instruction at the given address " +
        "and request a stop at the next instruction boundary.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "bp add $3A00            Break when PC reaches $3A00",
        "bp add $3D00 boot-end   Break with label",
        "bp list                 Show all breakpoints",
        "bp remove $3A00         Remove a single breakpoint",
        "bp disable $3A00        Keep entry but stop matching",
        "bp clear                Remove every breakpoint",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Registers / unregisters Call traps on the active trap registry.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["run", "stop", "watch", "trace"];

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
            return CommandResult.Error("No CPU attached. Boot a profile first.");
        }

        string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "list";

        return sub switch
        {
            "add" or "set" => Add(debugContext, args),
            "remove" or "rm" or "del" => Remove(debugContext, args),
            "list" or "ls" or "" => List(debugContext),
            "clear" => Clear(debugContext),
            "enable" => SetEnabled(debugContext, args, enabled: true),
            "disable" => SetEnabled(debugContext, args, enabled: false),
            _ => CommandResult.Error($"Unknown subcommand: '{args[0]}'. Use add/remove/list/clear/enable/disable."),
        };
    }

    private static CommandResult Add(IDebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: bp add <address> [label]");
        }

        if (!AddressParser.TryParse(args[1], context.Machine, out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'.");
        }

        string? label = args.Length > 2 ? string.Join(' ', args, 2, args.Length - 2) : null;

        if (!context.Breakpoints.Add(address, label))
        {
            return CommandResult.Error($"Breakpoint at ${address:X4} already exists.");
        }

        context.Output.WriteLine($"Breakpoint set at ${address:X4}{(label is null ? string.Empty : $"  ; {label}")}.");
        return CommandResult.Ok();
    }

    private static CommandResult Remove(IDebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: bp remove <address>");
        }

        if (!AddressParser.TryParse(args[1], context.Machine, out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'.");
        }

        if (!context.Breakpoints.Remove(address))
        {
            return CommandResult.Error($"No breakpoint at ${address:X4}.");
        }

        context.Output.WriteLine($"Removed breakpoint at ${address:X4}.");
        return CommandResult.Ok();
    }

    private static CommandResult Clear(IDebugContext context)
    {
        int count = context.Breakpoints.GetAll().Count;
        context.Breakpoints.Clear();
        context.Output.WriteLine($"Cleared {count} breakpoint(s).");
        return CommandResult.Ok();
    }

    private static CommandResult SetEnabled(IDebugContext context, string[] args, bool enabled)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error($"Usage: bp {(enabled ? "enable" : "disable")} <address>");
        }

        if (!AddressParser.TryParse(args[1], context.Machine, out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'.");
        }

        if (!context.Breakpoints.SetEnabled(address, enabled))
        {
            return CommandResult.Error($"No breakpoint at ${address:X4}.");
        }

        context.Output.WriteLine($"Breakpoint at ${address:X4} {(enabled ? "enabled" : "disabled")}.");
        return CommandResult.Ok();
    }

    private static CommandResult List(IDebugContext context)
    {
        var all = context.Breakpoints.GetAll();
        if (all.Count == 0)
        {
            context.Output.WriteLine("No breakpoints set.");
            var last = context.Breakpoints.LastHitAddress;
            if (last is not null)
            {
                context.Output.WriteLine($"Last hit (before clear): ${last.Value:X4}");
            }

            return CommandResult.Ok();
        }

        context.Output.WriteLine("ADDR   EN  HITS       LABEL");
        foreach (var bp in all)
        {
            context.Output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "${0:X4}  {1}   {2,-9}  {3}",
                bp.Address,
                bp.Enabled ? "Y" : "n",
                bp.Hits,
                bp.Label ?? string.Empty));
        }

        var lastHit = context.Breakpoints.LastHitAddress;
        if (lastHit is not null)
        {
            context.Output.WriteLine();
            context.Output.WriteLine($"Last hit: ${lastHit.Value:X4}");
        }

        return CommandResult.Ok();
    }
}