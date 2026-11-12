// <copyright file="RunUntilCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Dedicated command for "run-until" as a first-class shorthand.
/// "run-until $addr" is equivalent to "run until $addr", etc.
/// This provides clearer agent-friendly syntax.
/// </summary>
public sealed class RunUntilCommand : RunCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RunUntilCommand"/> class.
    /// </summary>
    public RunUntilCommand()
        : base("run-until", "Run until a condition (PC, bp, watch, mem, cycles) - shorthand for 'run until ...'")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = Array.Empty<string>();

    /// <inheritdoc/>
    public override string Usage => "run-until <addr|bp|watch|mem addr val> [options]";

    /// <inheritdoc/>
    public override string Synopsis => "run-until <condition> [options]";

    /// <inheritdoc/>
    public override string DetailedDescription =>
        "Convenience form of 'run until ...'. Supports the same conditions and options as 'run' with until targets. " +
        "Intended for agents and scripts for explicit 'until' semantics. " +
        "For Apple II DOS 3.3 boot confirmation: use run-until $0801 after C600 slot ROM activity (the C600 loader reads the boot sector into $0800 then does JMP $0801). " +
        "Examples: run-until $c000 ; run-until bp ; run-until mem $c030 01 ; run-until --until-cycles=10000 ; run-until $0801 --trace-buffer";

    /// <inheritdoc/>
    public override IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--trace", "-t", "flag", "Enable instruction tracing", "off"),
        new("--trace-buffer", "-tb", "flag", "Buffer trace output instead of streaming", "off"),
        new("--trace-file", null, "path", "Write trace output to specified file", null),
        new("--trace-last", null, "int", "Show only last N trace records", "100"),
        new("--trace-buffer-size", null, "int", "Maximum buffered trace records", "10000"),
        new("--timeout", null, "int", "Maximum wall-clock milliseconds to execute", "unlimited"),
    ];

    /// <inheritdoc/>
    public override IReadOnlyList<string> Examples { get; } =
    [
        "run-until $c000            Run until PC reaches $c000",
        "run-until bp               Run until breakpoint",
        "run-until watch            Run until watchpoint",
        "run-until mem $c030 01     Run until mem[$c030] == 0x01",
        "run-until --until-cycles=100000",
        "run-until $0801 --trace-buffer   Apple II disk boot: C600 ROM loads sector to 0800 then JMP $0801",
        "run-until $c000 --trace-buffer --trace-last=10",
    ];

    /// <inheritdoc/>
    public override string? SideEffects => base.SideEffects;

    /// <inheritdoc/>
    public override IReadOnlyList<string> SeeAlso { get; } = ["run", "step", "bp", "watch"];
}