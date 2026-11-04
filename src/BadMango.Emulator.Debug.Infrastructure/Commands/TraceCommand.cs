// <copyright file="TraceCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

/// <summary>
/// Controls the CPU instruction tracing listener.
/// </summary>
/// <remarks>
/// Provides fine-grained control over <see cref="TracingDebugListener"/>:
/// enable/disable, buffering, file output, PC-range filtering, and dumping
/// captured records. Use this in combination with <c>run</c> to trace narrow
/// regions like the BOOT2 / DOS bootstrap path without flooding the console.
/// </remarks>
public sealed class TraceCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TraceCommand"/> class.
    /// </summary>
    public TraceCommand()
        : base("trace", "Control CPU instruction tracing")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["tr"];

    /// <inheritdoc/>
    public override string Usage =>
        "trace <on|off|status|buffer|file|filter|dump|clear|tail> [args]";

    /// <inheritdoc/>
    public string Synopsis => "trace <subcommand> [args]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Controls the CPU instruction trace listener. Subcommands:\n" +
        "  on / off              Enable or disable tracing.\n" +
        "  status                Show tracing configuration and counters.\n" +
        "  buffer on|off [N]     Buffer records in memory (optional max count).\n" +
        "  file <path> | off     Write trace lines to a file.\n" +
        "  filter <start> <end>  Trace only when start <= PC <= end (hex or decimal).\n" +
        "  filter off            Remove the PC filter.\n" +
        "  dump [N]              Print the last N buffered records (default 50).\n" +
        "  tail N                Alias for 'dump N'.\n" +
        "  clear                 Reset the instruction count and buffer.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "trace on                       Start tracing",
        "trace filter $3A00 $3D00       Only trace BOOT2 region",
        "trace buffer on 50000          Buffer up to 50k records",
        "trace file boot.trace          Send trace to a file",
        "trace dump 100                 Show the last 100 records",
        "trace off                      Stop tracing",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Adjusts the shared TracingDebugListener attached to the CPU. File output opens a writer that is closed when changed or disabled.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["bp", "watch", "run", "stop"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.TracingListener is null)
        {
            return CommandResult.Error("No tracing listener attached. Boot a profile first.");
        }

        var tracer = debugContext.TracingListener;
        string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "status";

        return sub switch
        {
            "on" or "enable" => Enable(debugContext, tracer, true),
            "off" or "disable" => Enable(debugContext, tracer, false),
            "status" or "" => Status(debugContext, tracer),
            "buffer" => Buffer(debugContext, tracer, args),
            "file" => File(debugContext, tracer, args),
            "filter" => Filter(debugContext, tracer, args),
            "dump" or "tail" => Dump(debugContext, tracer, args),
            "clear" => Clear(debugContext, tracer),
            _ => CommandResult.Error($"Unknown subcommand: '{args[0]}'."),
        };
    }

    private static CommandResult Enable(IDebugContext context, TracingDebugListener tracer, bool enabled)
    {
        tracer.IsEnabled = enabled;
        context.Output.WriteLine($"Tracing {(enabled ? "enabled" : "disabled")}.");
        return CommandResult.Ok();
    }

    private static CommandResult Status(IDebugContext context, TracingDebugListener tracer)
    {
        context.Output.WriteLine($"Enabled        : {tracer.IsEnabled}");
        context.Output.WriteLine($"Buffer output  : {tracer.BufferOutput} (max {tracer.MaxBufferedRecords})");
        context.Output.WriteLine($"Buffered       : {tracer.GetBufferedRecords().Count}");
        context.Output.WriteLine($"Instructions   : {tracer.InstructionCount}");
        context.Output.WriteLine($"Address filter : {(tracer.AddressFilter is null ? "(none)" : "(set)")}");
        return CommandResult.Ok();
    }

    private static CommandResult Buffer(IDebugContext context, TracingDebugListener tracer, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: trace buffer <on|off> [max-records]");
        }

        switch (args[1].ToLowerInvariant())
        {
            case "on":
            case "enable":
                tracer.BufferOutput = true;
                if (args.Length >= 3 && int.TryParse(args[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int max))
                {
                    tracer.MaxBufferedRecords = max;
                }

                context.Output.WriteLine($"Buffering enabled (max {tracer.MaxBufferedRecords}).");
                return CommandResult.Ok();
            case "off":
            case "disable":
                tracer.BufferOutput = false;
                context.Output.WriteLine("Buffering disabled.");
                return CommandResult.Ok();
            default:
                return CommandResult.Error($"Unknown buffer mode: '{args[1]}'.");
        }
    }

    private static CommandResult File(IDebugContext context, TracingDebugListener tracer, string[] args)
    {
        if (args.Length < 2)
        {
            return CommandResult.Error("Usage: trace file <path|off>");
        }

        if (string.Equals(args[1], "off", StringComparison.OrdinalIgnoreCase))
        {
            tracer.SetFileOutput(null);
            context.Output.WriteLine("Trace file output disabled.");
            return CommandResult.Ok();
        }

        string path = args[1];
        if (context.PathResolver is not null)
        {
            try
            {
                path = context.PathResolver.Resolve(path);
            }
            catch (ArgumentException)
            {
                // Fall through with the raw path on resolver failure.
            }
        }

        try
        {
            tracer.SetFileOutput(path);
            context.Output.WriteLine($"Trace output -> {path}");
            return CommandResult.Ok();
        }
        catch (IOException ex)
        {
            return CommandResult.Error($"Failed to open trace file: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return CommandResult.Error($"Access denied: {ex.Message}");
        }
    }

    private static CommandResult Filter(IDebugContext context, TracingDebugListener tracer, string[] args)
    {
        if (args.Length >= 2 && string.Equals(args[1], "off", StringComparison.OrdinalIgnoreCase))
        {
            tracer.AddressFilter = null;
            context.Output.WriteLine("Address filter cleared.");
            return CommandResult.Ok();
        }

        if (args.Length < 3)
        {
            return CommandResult.Error("Usage: trace filter <start> <end>  |  trace filter off");
        }

        if (!AddressParser.TryParse(args[1], context.Machine, out uint start) ||
            !AddressParser.TryParse(args[2], context.Machine, out uint end))
        {
            return CommandResult.Error("Could not parse address range.");
        }

        if (end < start)
        {
            (start, end) = (end, start);
        }

        uint lo = start;
        uint hi = end;
        tracer.AddressFilter = pc => pc >= lo && pc <= hi;
        context.Output.WriteLine($"Address filter set: ${lo:X4}..${hi:X4}");
        return CommandResult.Ok();
    }

    private static CommandResult Dump(IDebugContext context, TracingDebugListener tracer, string[] args)
    {
        int count = 50;
        if (args.Length >= 2 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) && parsed > 0)
        {
            count = parsed;
        }

        var records = tracer.GetRecentRecords(count);
        if (records.Count == 0)
        {
            context.Output.WriteLine("(no buffered records)");
            return CommandResult.Ok();
        }

        foreach (var rec in records)
        {
            context.Output.WriteLine(TracingDebugListener.FormatTraceRecord(rec));
        }

        context.Output.WriteLine();
        context.Output.WriteLine($"{records.Count} record(s) shown (total instructions: {tracer.InstructionCount}).");
        return CommandResult.Ok();
    }

    private static CommandResult Clear(IDebugContext context, TracingDebugListener tracer)
    {
        tracer.ClearBuffer();
        context.Output.WriteLine("Trace buffer cleared.");
        return CommandResult.Ok();
    }
}