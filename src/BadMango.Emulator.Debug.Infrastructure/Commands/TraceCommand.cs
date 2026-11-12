// <copyright file="TraceCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

using BadMango.Emulator.Debug.Infrastructure;

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
        "  status                Show tracing configuration and counters (JSON friendly).\n" +
        "  buffer on|off [N]     Buffer records in memory (optional max count).\n" +
        "  file <path> | off     Write trace lines to a file.\n" +
        "  filter <start> <end>  Trace only when start <= PC <= end (hex or decimal).\n" +
        "  filter off            Remove the PC filter.\n" +
        "  dump [N]              Print or --json dump the last N buffered records (default 50).\n" +
        "  tail N                Alias for 'dump N'.\n" +
        "  clear                 Reset the instruction count and buffer.\n\n" +
        "Apple II disk boot tip (for stage confirmation): filter $C600-$C6FF for the slot ROM boot loader, " +
        "then watch for execution at $0801 (the C600 code loads the boot sector to $0800 and does JMP $0801).";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "trace on                       Start tracing",
        "trace filter $C600 $C6FF       Apple II: trace slot 6 ROM boot loader",
        "trace filter $0800 $0801       Apple II: after load, execution enters at $0801 (boot sector code)",
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
        bool useJson = context.JsonOutput;

        if (useJson)
        {
            var status = new
            {
                enabled = tracer.IsEnabled,
                bufferOutput = tracer.BufferOutput,
                maxBufferedRecords = tracer.MaxBufferedRecords,
                buffered = tracer.GetBufferedRecords().Count,
                instructionCount = tracer.InstructionCount,
                hasAddressFilter = tracer.AddressFilter is not null,
                // richer for agents (6.3)
                recentSample = tracer.GetRecentRecords(3).Select(r => new { pc = $"${r.PC:X4}", instr = r.Instruction.ToString() }).ToList()
            };
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

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

        bool useJson = context.JsonOutput;

        if (useJson)
        {
            var projected = records.Select(r => new
            {
                pc = $"${r.PC:X4}",
                opcode = $"${r.Opcode:X2}",
                instruction = r.Instruction.ToString(),
                operands = GetOperandBytes(r),
                effectiveAddress = r.EffectiveAddress != 0 ? $"${r.EffectiveAddress:X4}" : null,
                a = $"${r.A:X2}",
                x = $"${r.X:X2}",
                y = $"${r.Y:X2}",
                sp = $"${r.SP:X2}",
                p = r.P.ToString(),
                cycles = r.Cycles,
                instructionCycles = r.InstructionCycles,
                halted = r.Halted,
                haltReason = r.Halted ? r.HaltReason.ToString() : null,
            }).ToList();

            var dump = new
            {
                count = projected.Count,
                totalInstructions = tracer.InstructionCount,
                records = projected,
            };
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(dump, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
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

    private static byte[] GetOperandBytes(TraceRecord r)
    {
        var bytes = new byte[r.OperandSize];
        for (int i = 0; i < r.OperandSize; i++)
        {
            bytes[i] = r.Operands[i];
        }
        return bytes;
    }

    private static CommandResult Clear(IDebugContext context, TracingDebugListener tracer)
    {
        tracer.ClearBuffer();
        context.Output.WriteLine("Trace buffer cleared.");
        return CommandResult.Ok();
    }
}