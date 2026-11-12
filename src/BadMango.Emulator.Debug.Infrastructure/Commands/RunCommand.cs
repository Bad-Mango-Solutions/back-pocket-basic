// <copyright file="RunCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Debug.Infrastructure;

using Core.Cpu;

/// <summary>
/// Runs the CPU until it halts or reaches a limit.
/// </summary>
/// <remarks>
/// <para>
/// Executes instructions continuously until:
/// - The CPU halts (STP or WAI instruction).
/// - The instruction limit is reached.
/// - The cycle limit is reached.
/// - A stop is requested (via StopCommand or externally).
/// </para>
/// <para>
/// Guards against infinite loops by enforcing configurable limits.
/// Optional logging can trace execution for debugging purposes.
/// </para>
/// </remarks>
public class RunCommand : ExecutionCommandBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunCommand"/> class.
    /// </summary>
    public RunCommand()
        : this("run", "Run CPU until halt or limit reached")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunCommand"/> class with custom name and description.
    /// </summary>
    /// <param name="name">The primary command name (e.g. "run" or "run-until").</param>
    /// <param name="description">Brief description of the command.</param>
    protected RunCommand(string name, string description)
        : base(name, description)
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["g", "go"];

    /// <inheritdoc/>
    public override string Usage => "run [instruction_limit] [--cycles=<limit>] [--trace] [--trace-file=<path>] [--trace-buffer]";

    /// <inheritdoc/>
    public override string Synopsis => "run [limit] [options]";

    /// <inheritdoc/>
    public override string DetailedDescription =>
        "Executes CPU instructions continuously until the processor halts (STP or WAI), " +
        "a breakpoint is hit, or a stop is requested. By default no instruction, cycle, " +
        "or timeout limit is applied — execution continues until externally stopped. " +
        "Use --instructions=<n>, --cycles=<n>, or --timeout=<ms> to enforce explicit limits. " +
        "Optional tracing captures execution history for debugging.";

    /// <inheritdoc/>
    public override IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--trace", "-t", "flag", "Enable instruction tracing", "off"),
        new("--trace-buffer", "-tb", "flag", "Buffer trace output instead of streaming", "off"),
        new("--trace-file", null, "path", "Write trace output to specified file", null),
        new("--trace-last", null, "int", "Show only last N trace records", "100"),
        new("--trace-buffer-size", null, "int", "Maximum buffered trace records", "10000"),
        new("--cycles", null, "int", "Maximum cycles to execute", "unlimited"),
        new("--instructions", null, "int", "Maximum instructions to execute", "unlimited"),
        new("--timeout", null, "int", "Maximum wall-clock milliseconds to execute", "unlimited"),
        new("--until", null, "addr", "Run until this PC address is reached (or 'bp'/'watch'/'mem $addr $val')", null),
        new("--until-bp", null, "flag", "Run until a breakpoint is hit", "off"),
        new("--until-watch", null, "flag", "Run until a watchpoint is hit", "off"),
        new("--until-cycles", null, "int", "Run with this cycle limit", "unlimited"),
        new("--until-mem", null, "addr=val", "Run until memory[addr] == val", null),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples { get; } =
    [
        "run                          Execute until halt, breakpoint, or stop",
        "run 1000                     Execute up to 1000 instructions",
        "run --cycles=50000           Execute up to 50,000 cycles",
        "run until $c000              Run until PC == $c000",
        "run --until=$c000            Same using flag",
        "run until bp                 Run until next breakpoint hit",
        "run --until-bp               Same",
        "run until watch              Run until next watchpoint hit",
        "run --until-watch            Same",
        "run until mem $c030 01       Run until memory at $c030 equals 0x01",
        "run --until-mem=$c030:01     Same",
        "run --until-cycles=100000    Run with explicit cycle limit",
        "run --trace                  Execute with instruction tracing",
        "run --trace-buffer --trace-last=50   Buffer and show last 50 instructions",
    ];

    /// <inheritdoc/>
    public override string? SideEffects =>
        "Modifies CPU state (PC, registers, flags). May modify memory and trigger " +
        "I/O device state changes depending on executed code.";

    /// <inheritdoc/>
    public override IReadOnlyList<string> SeeAlso { get; } = ["step", "stop", "call", "reset"];

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

        if (debugContext.Cpu.Halted)
        {
            return CommandResult.Error("CPU is halted. Use 'reset' to restart.");
        }

        // 6.4: run status / stop / start (background for agent polling)
        string first = args.Length > 0 ? args[0].ToLowerInvariant() : "";
        if (first == "status" || first == "poll")
        {
            return ShowRunStatus(debugContext);
        }
        if (first == "stop" || first == "cancel")
        {
            debugContext.RequestRunStop();
            return CommandResult.Ok("Run stop requested.");
        }
        bool startBackground = first == "start" || first == "bg" || first == "background";
        if (startBackground && args.Length > 0)
        {
            // shift args
            args = args.Skip(1).ToArray();
        }

        // Support dedicated "run-until" command as shorthand for "run until ..."
        // e.g. "run-until $c000" or "run-until bp" or "run-until mem $c030 01"
        if (this.Name.Equals("run-until", StringComparison.OrdinalIgnoreCase)
            && args.Length > 0
            && !args[0].Equals("until", StringComparison.OrdinalIgnoreCase)
            && !args[0].StartsWith("--until", StringComparison.OrdinalIgnoreCase))
        {
            var newArgs = new string[args.Length + 1];
            newArgs[0] = "until";
            Array.Copy(args, 0, newArgs, 1, args.Length);
            args = newArgs;
        }

        // Parse options
        var options = ParseRunOptions(args);

        // Resolve any until target now that we have the machine.
        // Supports:
        //   run until $addr
        //   run --until=$addr
        //   run --until-pc=0xaddr
        //   run until bp
        //   run --until-bp
        //   run --until-cycles=10000   (or --cycles=...)
        bool untilBp = false;
        bool untilWatch = false;
        long? untilCycles = null;
        uint? untilMemAddr = null;
        byte? untilMemVal = null;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (a.Equals("until", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                string next = args[i + 1].ToLowerInvariant();
                if (next == "bp" || next == "breakpoint")
                {
                    untilBp = true;
                }
                else if (next == "watch" || next == "watchpoint")
                {
                    untilWatch = true;
                }
                else if (next == "mem" && i + 3 < args.Length)
                {
                    if (TryParseAddress(args[i + 2], debugContext.Machine, out uint addr) &&
                        TryParseNumber(args[i + 3], out long v) && v >= 0 && v <= 255)
                    {
                        untilMemAddr = addr;
                        untilMemVal = (byte)v;
                    }
                }
                else if (TryParseAddress(args[i + 1], debugContext.Machine, out uint addr))
                {
                    options.UntilPc = addr;
                }

                break;
            }

            if (a.StartsWith("--until=", StringComparison.OrdinalIgnoreCase) ||
                a.StartsWith("--until-pc=", StringComparison.OrdinalIgnoreCase))
            {
                int eq = a.IndexOf('=');
                if (eq > 0)
                {
                    string val = a[(eq + 1)..];
                    if (val.Equals("bp", StringComparison.OrdinalIgnoreCase) || val.Equals("breakpoint", StringComparison.OrdinalIgnoreCase))
                    {
                        untilBp = true;
                    }
                    else if (TryParseAddress(val, debugContext.Machine, out uint addr))
                    {
                        options.UntilPc = addr;
                    }
                }
            }

            if (a.Equals("--until-bp", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--until-breakpoint", StringComparison.OrdinalIgnoreCase))
            {
                untilBp = true;
            }

            if (a.Equals("--until-watch", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--until-watchpoint", StringComparison.OrdinalIgnoreCase))
            {
                untilWatch = true;
            }

            if (a.StartsWith("--until-mem=", StringComparison.OrdinalIgnoreCase))
            {
                string val = a["--until-mem=".Length..];
                var parts = val.Split('=', ':');
                if (parts.Length == 2 &&
                    TryParseAddress(parts[0], debugContext.Machine, out uint addr) &&
                    TryParseNumber(parts[1], out long v) && v >= 0 && v <= 255)
                {
                    untilMemAddr = addr;
                    untilMemVal = (byte)v;
                }
            }

            if (a.StartsWith("--until-cycles=", StringComparison.OrdinalIgnoreCase))
            {
                string val = a["--until-cycles=".Length..];
                if (TryParseNumber(val, out long c))
                {
                    untilCycles = c;
                }
            }
        }

        if (untilBp && debugContext.Breakpoints != null)
        {
            debugContext.Breakpoints.ResetLastHit();

            // Run already stops on bp by default, we just mark the intent for reporting.
        }

        if (untilWatch && debugContext.Watchpoints != null)
        {
            debugContext.Watchpoints.ResetLastHit();

            // Similar for watchpoints that have stopOnHit.
        }

        if (untilCycles.HasValue)
        {
            options.CycleLimit = untilCycles.Value;
        }

        debugContext.Output.WriteLine($"Running from PC=${debugContext.Cpu.GetPC():X4}...");

        uint? untilTarget = options.UntilPc;

        var terminate = (IDebugContext dc, byte _) =>
        {
            if (untilTarget.HasValue && dc.Cpu is not null)
            {
                return dc.Cpu.GetPC() == untilTarget.Value;
            }

            if (untilMemAddr.HasValue && untilMemVal.HasValue && dc.Bus is not null)
            {
                var access = new BusAccess(
                    Address: untilMemAddr.Value,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Decomposed,
                    EmulationFlag: true,
                    Intent: AccessIntent.DebugRead,
                    SourceId: 0,
                    Cycle: 0,
                    Flags: AccessFlags.NoSideEffects);
                var res = dc.Bus.TryRead8(access);
                if (!res.Fault.IsFault && res.Value == untilMemVal.Value)
                {
                    return true;
                }
            }

            return false;
        };

        if (startBackground)
        {
            // 6.4: launch on background task so MCP/agent can poll with "run status"
            if (debugContext is DebugContext dcImpl)
            {
                dcImpl.StartBackgroundRun(ct =>
                {
                    // Note: the inner loop doesn't take ct yet, but respects stop request + we cancel
                    var res = ExecuteInstructionLoop(debugContext, options, terminate);
                    return Task.FromResult(res);
                }, "background run");
            }
            else
            {
                // fallback
                _ = Task.Run(() => ExecuteInstructionLoop(debugContext, options, terminate));
            }
            debugContext.Output.WriteLine("Background run started. Use 'run status' (or emudbg_run_status) to poll.");
            return CommandResult.Ok();
        }

        // Execute the instruction loop (synchronous / blocking for REPL)
        var result = ExecuteInstructionLoop(
            debugContext,
            options,
            terminate);

        (debugContext as DebugContext)?.SetLastRunResult(result);

        bool bpHit = debugContext.Breakpoints?.LastHitAddress != null;
        bool watchHit = debugContext.Watchpoints?.LastHitAddress != null;
        bool untilHit = false;

        if (untilTarget.HasValue && debugContext.Cpu is not null)
        {
            untilHit = debugContext.Cpu.GetPC() == untilTarget.Value;
        }
        else if (untilBp && bpHit)
        {
            untilHit = true;
        }
        else if (untilWatch && watchHit)
        {
            untilHit = true;
        }
        else if (untilMemAddr.HasValue && untilMemVal.HasValue)
        {
            // For mem, we check after if it matched in the terminate func
            // For simplicity, re-check here for reporting
            if (debugContext.Bus is not null)
            {
                var access = new BusAccess(
                    Address: untilMemAddr.Value,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Decomposed,
                    EmulationFlag: true,
                    Intent: AccessIntent.DebugRead,
                    SourceId: 0,
                    Cycle: 0,
                    Flags: AccessFlags.NoSideEffects);
                var res = debugContext.Bus.TryRead8(access);
                if (!res.Fault.IsFault && res.Value == untilMemVal.Value)
                {
                    untilHit = true;
                }
            }
        }

        bool useJson = context.JsonOutput;

        if (useJson)
        {
            object? finalRegs = null;
            if (debugContext.Cpu is not null)
            {
                var registers = debugContext.Cpu.GetRegisters();
                finalRegs = new
                {
                    pc = registers.PC.GetWord(),
                    sp = registers.SP.GetWord(),
                    a = registers.A.GetWord(),
                    x = registers.X.GetWord(),
                    y = registers.Y.GetWord(),
                    flags = new
                    {
                        N = registers.P.HasFlag(ProcessorStatusFlags.N),
                        V = registers.P.HasFlag(ProcessorStatusFlags.V),
                        M = registers.P.HasFlag(ProcessorStatusFlags.M),
                        X = registers.P.HasFlag(ProcessorStatusFlags.X),
                        D = registers.P.HasFlag(ProcessorStatusFlags.D),
                        I = registers.P.HasFlag(ProcessorStatusFlags.I),
                        Z = registers.P.HasFlag(ProcessorStatusFlags.Z),
                        C = registers.P.HasFlag(ProcessorStatusFlags.C),
                    },
                    e = registers.E,
                    cp = registers.CP,
                };
            }

            object? traceRecs = null;
            if (options.EnableTrace && options.BufferTrace && debugContext.TracingListener is not null)
            {
                var recs = debugContext.TracingListener.GetRecentRecords(Math.Min(20, options.TraceLastN));
                traceRecs = recs.Select(r => new { pc = $"${r.PC:X4}", instruction = r.Instruction.ToString() }).ToList();
            }

            var runResult = new
            {
                stopReason = result.StopReason,
                instructionCount = result.InstructionCount,
                cycleCount = result.CycleCount,
                elapsedMs = result.ElapsedMs,
                finalPc = $"${debugContext.Cpu!.GetPC():X4}",
                normalCompletion = result.NormalCompletion,
                untilTarget = untilTarget.HasValue ? $"${untilTarget.Value:X4}" : (untilBp ? "bp" : (untilWatch ? "watch" : (untilMemAddr.HasValue ? $"mem ${untilMemAddr.Value:X4}=${untilMemVal:X2}" : null))),
                untilBp,
                untilWatch,
                untilHit,
                bpHit,
                watchHit,
                finalRegisters = finalRegs,
                traceRecords = traceRecs,
            };
            debugContext.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(runResult, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine($"Stopped: {result.StopReason}");
            debugContext.Output.WriteLine($"  Instructions executed: {result.InstructionCount:N0}");
            debugContext.Output.WriteLine($"  Cycles consumed: {result.CycleCount:N0}");
            debugContext.Output.WriteLine($"  Final PC = ${debugContext.Cpu!.GetPC():X4}");

            if (untilTarget.HasValue)
            {
                debugContext.Output.WriteLine(untilHit
                    ? $"  Until target ${untilTarget.Value:X4} reached."
                    : $"  Until target ${untilTarget.Value:X4} not reached.");
            }
            else if (untilBp)
            {
                debugContext.Output.WriteLine(bpHit
                    ? "  Until breakpoint: hit."
                    : "  Until breakpoint: no breakpoint hit (stopped for other reason).");
            }
            else if (untilWatch)
            {
                debugContext.Output.WriteLine(watchHit
                    ? "  Until watchpoint: hit."
                    : "  Until watchpoint: no watchpoint hit (stopped for other reason).");
            }
            else if (untilMemAddr.HasValue && untilMemVal.HasValue)
            {
                debugContext.Output.WriteLine(untilHit
                    ? $"  Until mem[${untilMemAddr.Value:X4}]==${untilMemVal.Value:X2}: matched."
                    : $"  Until mem[${untilMemAddr.Value:X4}]==${untilMemVal.Value:X2}: not matched.");
            }
            if (bpHit && !untilBp)
            {
                debugContext.Output.WriteLine($"  Breakpoint hit at ${debugContext.Breakpoints!.LastHitAddress:X4}");
            }
            if (watchHit && !untilWatch)
            {
                debugContext.Output.WriteLine($"  Watchpoint hit at ${debugContext.Watchpoints!.LastHitAddress:X4}");
            }
        }

        // Output buffered trace if requested
        if (options.EnableTrace && options.BufferTrace && debugContext.TracingListener is not null)
        {
            OutputBufferedTrace(debugContext, debugContext.TracingListener, options.TraceLastN);
        }

        return CommandResult.Ok();
    }

    private static ExecutionOptions ParseRunOptions(string[] args)
    {
        // RunCommand should default to unlimited execution. Start from the unlimited
        // baseline and let ParseCommonOptions populate any explicit user limits.
        var options = new ExecutionOptions
        {
            InstructionLimit = int.MaxValue,
            CycleLimit = long.MaxValue,
            TimeoutMs = 0,
        };

        ApplyCommonOptions(args, options);

        // Parse positional argument as instruction limit
        // Avoid treating until-targets (keywords or addrs starting with $ / 0x) as limits
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.Equals("until", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("bp", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("watch", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("mem", StringComparison.OrdinalIgnoreCase))
            {
                // skip the keyword and its following value(s) if present
                if (arg.Equals("mem", StringComparison.OrdinalIgnoreCase) && i + 2 < args.Length) i += 2;
                else if (i + 1 < args.Length) i++;
                continue;
            }
            if (!arg.StartsWith("-", StringComparison.Ordinal) &&
                !arg.StartsWith("$", StringComparison.Ordinal) &&
                !arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
                TryParseNumber(arg, out long limit) &&
                limit <= int.MaxValue && limit > 0)
            {
                options.InstructionLimit = (int)limit;
                break;
            }
        }

        return options;
    }

    private static CommandResult ShowRunStatus(IDebugContext debugContext)
    {
        bool useJson = debugContext.JsonOutput;

        var isActive = debugContext.IsRunActive;
        var desc = debugContext.ActiveRunDescription;
        var last = debugContext.LastRunResult;
        var cpu = debugContext.Cpu;

        if (useJson)
        {
            var status = new
            {
                isRunActive = isActive,
                description = desc,
                currentPc = cpu != null ? $"${cpu.GetPC():X4}" : null,
                lastResult = last != null ? new
                {
                    instructionCount = last.InstructionCount,
                    cycleCount = last.CycleCount,
                    elapsedMs = last.ElapsedMs,
                    stopReason = last.StopReason,
                    normalCompletion = last.NormalCompletion
                } : null
            };
            debugContext.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(status, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        debugContext.Output.WriteLine("Run status:");
        debugContext.Output.WriteLine($"  Active      : {isActive}");
        if (desc != null) debugContext.Output.WriteLine($"  Description : {desc}");
        if (cpu != null) debugContext.Output.WriteLine($"  Current PC  : ${cpu.GetPC():X4}");
        if (last != null)
        {
            debugContext.Output.WriteLine("  Last result :");
            debugContext.Output.WriteLine($"    Instructions: {last.InstructionCount}");
            debugContext.Output.WriteLine($"    Cycles      : {last.CycleCount}");
            debugContext.Output.WriteLine($"    Elapsed     : {last.ElapsedMs} ms");
            debugContext.Output.WriteLine($"    Stop reason : {last.StopReason}");
            debugContext.Output.WriteLine($"    Normal      : {last.NormalCompletion}");
        }
        else
        {
            debugContext.Output.WriteLine("  Last result : (none)");
        }
        return CommandResult.Ok();
    }
}