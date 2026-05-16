// <copyright file="WatchCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

/// <summary>
/// Manages memory watchpoints that fire on read / write of an effective address.
/// </summary>
/// <remarks>
/// Watchpoints are implemented as a debug step listener that inspects the
/// effective address of each instruction. They support read-only, write-only,
/// or read/write triggers and can optionally request a CPU stop on hit.
/// </remarks>
public sealed class WatchCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WatchCommand"/> class.
    /// </summary>
    public WatchCommand()
        : base("watch", "Manage memory watchpoints")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["wp"];

    /// <inheritdoc/>
    public override string Usage => "watch <add|remove|list|clear|enable|disable|log> [address] [r|w|rw] [--stop] [label]";

    /// <inheritdoc/>
    public string Synopsis => "watch <subcommand> [args]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Adds or removes memory watchpoints. Each watchpoint matches a specific " +
        "effective address and access kind (read, write, or read/write). Hits are " +
        "logged to the debug output; pass --stop to request a CPU stop on hit. Use " +
        "'watch log on|off' to enable/disable real-time hit logging.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "watch add $3D0 rw --stop boot-vector  Stop on any access to $3D0",
        "watch add $C0E9 w                     Log writes to motor-on switch",
        "watch list                            Show all watchpoints",
        "watch clear                           Remove every watchpoint",
        "watch log on                          Enable hit logging to console",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Attaches a step listener to the CPU. Watchpoints with --stop will halt execution.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["bp", "trace", "run", "stop"];

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
            "log" => Log(debugContext, args),
            _ => CommandResult.Error($"Unknown subcommand: '{args[0]}'. Use add/remove/list/clear/enable/disable/log."),
        };
    }

    private static CommandResult Add(IDebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: watch add <address> [r|w|rw] [--stop] [label]");
        }

        if (!AddressParser.TryParse(args[1], context.Machine, out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'.");
        }

        var access = WatchAccess.ReadWrite;
        bool stopOnHit = false;
        var labelParts = new List<string>();

        for (int i = 2; i < args.Length; i++)
        {
            string token = args[i];
            switch (token.ToLowerInvariant())
            {
                case "r":
                case "read":
                    access = WatchAccess.Read;
                    break;
                case "w":
                case "write":
                    access = WatchAccess.Write;
                    break;
                case "rw":
                case "readwrite":
                    access = WatchAccess.ReadWrite;
                    break;
                case "--stop":
                case "-s":
                    stopOnHit = true;
                    break;
                default:
                    labelParts.Add(token);
                    break;
            }
        }

        string? label = labelParts.Count == 0 ? null : string.Join(' ', labelParts);

        if (!context.Watchpoints.Add(address, access, stopOnHit, label))
        {
            return CommandResult.Error($"Watchpoint at ${address:X4} already exists.");
        }

        context.Output.WriteLine(
            $"Watchpoint set at ${address:X4} ({access}{(stopOnHit ? ", stop" : string.Empty)})" +
            (label is null ? string.Empty : $"  ; {label}") +
            ".");
        return CommandResult.Ok();
    }

    private static CommandResult Remove(IDebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: watch remove <address>");
        }

        if (!AddressParser.TryParse(args[1], context.Machine, out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'.");
        }

        if (!context.Watchpoints.Remove(address))
        {
            return CommandResult.Error($"No watchpoint at ${address:X4}.");
        }

        context.Output.WriteLine($"Removed watchpoint at ${address:X4}.");
        return CommandResult.Ok();
    }

    private static CommandResult Clear(IDebugContext context)
    {
        int count = context.Watchpoints.GetAll().Count;
        context.Watchpoints.Clear();
        context.Output.WriteLine($"Cleared {count} watchpoint(s).");
        return CommandResult.Ok();
    }

    private static CommandResult SetEnabled(IDebugContext context, string[] args, bool enabled)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error($"Usage: watch {(enabled ? "enable" : "disable")} <address>");
        }

        if (!AddressParser.TryParse(args[1], context.Machine, out uint address))
        {
            return CommandResult.Error($"Invalid address: '{args[1]}'.");
        }

        if (!context.Watchpoints.SetEnabled(address, enabled))
        {
            return CommandResult.Error($"No watchpoint at ${address:X4}.");
        }

        context.Output.WriteLine($"Watchpoint at ${address:X4} {(enabled ? "enabled" : "disabled")}.");
        return CommandResult.Ok();
    }

    private static CommandResult Log(IDebugContext context, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: watch log <on|off>");
        }

        switch (args[1].ToLowerInvariant())
        {
            case "on":
            case "enable":
                context.Watchpoints.SetLogOutput(context.Output);
                context.Output.WriteLine("Watchpoint hit logging enabled.");
                return CommandResult.Ok();
            case "off":
            case "disable":
                context.Watchpoints.SetLogOutput(null);
                context.Output.WriteLine("Watchpoint hit logging disabled.");
                return CommandResult.Ok();
            default:
                return CommandResult.Error($"Unknown log mode: '{args[1]}'. Use on or off.");
        }
    }

    private static CommandResult List(IDebugContext context)
    {
        var all = context.Watchpoints.GetAll();
        if (all.Count == 0)
        {
            context.Output.WriteLine("No watchpoints set.");
            return CommandResult.Ok();
        }

        context.Output.WriteLine("ADDR   ACCESS  STOP  EN  HITS       LABEL");
        foreach (var wp in all)
        {
            context.Output.WriteLine(string.Format(
                CultureInfo.InvariantCulture,
                "${0:X4}  {1,-6}  {2,-4}  {3}   {4,-9}  {5}",
                wp.Address,
                wp.Access,
                wp.StopOnHit ? "Y" : "n",
                wp.Enabled ? "Y" : "n",
                wp.Hits,
                wp.Label ?? string.Empty));
        }

        var lastHit = context.Watchpoints.LastHitAddress;
        if (lastHit is not null)
        {
            context.Output.WriteLine();
            context.Output.WriteLine($"Last hit: ${lastHit.Value:X4} ({context.Watchpoints.LastHitAccess})");
        }

        return CommandResult.Ok();
    }
}