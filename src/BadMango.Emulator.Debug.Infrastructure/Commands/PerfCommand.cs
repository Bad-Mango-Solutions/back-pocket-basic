// <copyright file="PerfCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Reports basic performance/introspection stats (instructions, cycles via tracer, current state).
/// </summary>
public sealed class PerfCommand : CommandHandlerBase, ICommandHelp
{
    public PerfCommand() : base("perf", "Show emulator performance/introspection stats") { }

    public override IReadOnlyList<string> Aliases { get; } = ["performance", "stats"];

    public override string Usage => "perf [--json]";

    public string Synopsis => "Report instruction count, current PC, tracer stats, etc. for introspection (6.9).";

    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext dc)
            return CommandResult.Error("Debug context required.");

        bool useJson = context.JsonOutput || args.Any(a => a is "--json" or "-j");

        long instr = dc.TracingListener?.InstructionCount ?? 0;
        var pc = dc.Cpu?.GetPC() ?? 0;
        int faults = dc.Bus?.FaultRing?.Count ?? 0;
        bool tracing = dc.TracingListener?.IsEnabled ?? false;

        if (useJson)
        {
            var info = new
            {
                instructions = instr,
                pc = $"${pc:X4}",
                busFaults = faults,
                tracingEnabled = tracing,
                note = "Use 'trace on' / 'run' to populate; extend for cycles/sec, hotspots in future."
            };
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        context.Output.WriteLine("Emulator Perf/Introspection:");
        context.Output.WriteLine($"  Instructions executed: {instr:N0}");
        context.Output.WriteLine($"  Current PC: ${pc:X4}");
        context.Output.WriteLine($"  Bus faults in ring: {faults}");
        context.Output.WriteLine($"  Tracing enabled: {tracing}");
        context.Output.WriteLine("  (Run with trace buffer on for more; use for agent profiling.)");
        return CommandResult.Ok();
    }

    public IReadOnlyList<CommandOption> Options { get; } = [
        new("--json", "-j", "flag", "Structured JSON output", null)
    ];

    public IReadOnlyList<string> Examples { get; } = [
        "perf",
        "perf --json"
    ];

    public string DetailedDescription => "Provides basic emulator efficiency stats (instructions via tracer, PC, faults). Part of 6.9 introspection.";
    public string? SideEffects => null;
    public IReadOnlyList<string> SeeAlso { get; } = ["regs", "trace", "run", "buslog"];
}
