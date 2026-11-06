// <copyright file="BusLogCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

/// <summary>
/// Displays the bus fault log captured by the memory bus and lets the
/// operator clear it.
/// </summary>
/// <remarks>
/// <para>
/// The memory bus pushes every fault it observes (unmapped, permission,
/// NX, misaligned, device, plus synthetic faults for silent floating-bus
/// reads from composite targets with no sub-handler) into a fixed-capacity
/// ring buffer. This command renders that ring buffer.
/// </para>
/// <para>
/// Recording is off by default so normal emulation does not pay for it
/// (see <see cref="Bus.Interfaces.IMemoryBus.FaultRecordingEnabled"/>). The
/// first <c>buslog</c> or <c>fault</c> invocation turns it on; faults that
/// occurred before that point are not captured. Use <c>buslog off</c> to
/// stop recording again.
/// </para>
/// <para>
/// This command requires a bus to be attached to the debug context.
/// </para>
/// </remarks>
public sealed class BusLogCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusLogCommand"/> class.
    /// </summary>
    public BusLogCommand()
        : base("buslog", "Show or clear the bus fault log")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["bl"];

    /// <inheritdoc/>
    public override string Usage => "buslog [show|tail|clear|status|on|off] [N]";

    /// <inheritdoc/>
    public string Synopsis => "buslog [show|tail|clear|status|on|off] [N]";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Inspects the bus fault ring buffer. The bus records every fault it " +
        "observes (Unmapped, Permission, Nx, Misaligned, DeviceFault) plus " +
        "synthetic Unmapped entries for silent floating-bus reads where a " +
        "composite target had no sub-handler for the requested offset.\n" +
        "Recording is off by default to keep emulation fast; the first use of " +
        "'buslog' or 'fault' turns it on (earlier faults are not captured).\n" +
        "  show [N] | tail [N]   Print the last N faults (default 20).\n" +
        "  clear                 Empty the buffer and reset counters.\n" +
        "  status                Show buffer capacity / counters.\n" +
        "  on | off              Enable or disable fault recording.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "buslog                   Show the last 20 faults",
        "buslog show 100          Show the last 100 faults",
        "buslog clear             Empty the buffer",
        "buslog status            Show buffer capacity and counters",
        "buslog off               Stop recording faults",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "'clear' discards the captured fault history. Any subcommand other " +
        "than 'off' enables bus fault recording, which adds a small cost to " +
        "faulting bus accesses.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["fault", "switches", "regions"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        bool useJson = context.JsonOutput || args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase) || a.Equals("-j", StringComparison.OrdinalIgnoreCase));

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (!debugContext.IsBusAttached || debugContext.Bus is null)
        {
            return CommandResult.Error("No bus attached. This command requires a bus-based system.");
        }

        var ring = debugContext.Bus.FaultRing;
        if (ring is null)
        {
            return CommandResult.Error("No bus fault ring is configured for this bus.");
        }

        string subcommand = args.Length > 0 ? args[0].ToLowerInvariant() : "show";

        if (subcommand == "off")
        {
            debugContext.Bus.FaultRecordingEnabled = false;
            debugContext.Output.WriteLine("Bus fault recording disabled.");
            return CommandResult.Ok();
        }

        bool justEnabled = EnsureRecordingEnabled(debugContext.Bus, debugContext.Output);
        if (subcommand == "on")
        {
            if (!justEnabled)
            {
                debugContext.Output.WriteLine("Bus fault recording is already enabled.");
            }

            return CommandResult.Ok();
        }

        return subcommand switch
        {
            "show" or "tail" or "" => ShowLog(debugContext, ring, args),
            "clear" => ClearLog(debugContext, ring),
            "status" => ShowStatus(debugContext, ring),
            _ => CommandResult.Error($"Unknown subcommand: '{args[0]}'. Use: show, tail, clear, status, on, or off."),
        };
    }

    /// <summary>
    /// Turns on bus fault recording if it is currently off and tells the
    /// operator that only faults from now on will be captured.
    /// </summary>
    /// <param name="bus">The bus whose recording flag to set.</param>
    /// <param name="output">Where to write the notice.</param>
    /// <returns>
    /// <see langword="true"/> if recording was off and has just been enabled;
    /// otherwise <see langword="false"/>.
    /// </returns>
    internal static bool EnsureRecordingEnabled(Bus.Interfaces.IMemoryBus bus, TextWriter output)
    {
        if (bus.FaultRecordingEnabled)
        {
            return false;
        }

        bus.FaultRecordingEnabled = true;
        output.WriteLine("Bus fault recording enabled; faults from this point on will be captured ('buslog off' to disable).");
        output.WriteLine();
        return true;
    }

    private static CommandResult ShowLog(IDebugContext context, BadMango.Emulator.Bus.Interfaces.IBusFaultRing ring, string[] args)
    {
        int n = 20;
        if (args.Length > 1 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) && parsed > 0)
        {
            n = parsed;
        }

        bool useJson = context.JsonOutput;
        var snapshot = ring.Snapshot();
        if (snapshot.Length == 0)
        {
            if (useJson)
                context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { count = 0 }));
            else
                context.Output.WriteLine("Bus fault log is empty.");
            return CommandResult.Ok();
        }

        if (useJson)
        {
            var items = snapshot.TakeLast(n).Select(f => new {
                cycle = f.Cycle,
                kind = f.Kind.ToString(),
                address = $"${f.Address:X4}",
                width = f.WidthBits,
                intent = f.Intent.ToString(),
                device = f.DeviceId,
                region = f.RegionTag
            }).ToArray();
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { shown = items.Length, total = ring.TotalFaults, faults = items }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        int start = Math.Max(0, snapshot.Length - n);
        int shown = snapshot.Length - start;

        context.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"Bus fault log (showing {shown} of {ring.Count}; {ring.TotalFaults} total recorded):"));
        context.Output.WriteLine();
        context.Output.WriteLine("  Cycle           Kind        Addr  W  Intent             Device  Region");
        context.Output.WriteLine("  --------------- ----------- ----- -- ------------------ ------- ------------");
        for (int i = start; i < snapshot.Length; i++)
        {
            var f = snapshot[i];
            context.Output.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"  {f.Cycle,15} {f.Kind,-11} ${f.Address:X4} {f.WidthBits,2} {f.Intent,-18} {f.DeviceId,7} {f.RegionTag}"));
        }

        return CommandResult.Ok();
    }

    private static CommandResult ClearLog(IDebugContext context, BadMango.Emulator.Bus.Interfaces.IBusFaultRing ring)
    {
        ring.Clear();
        context.Output.WriteLine("Bus fault log cleared.");
        return CommandResult.Ok();
    }

    private static CommandResult ShowStatus(IDebugContext context, BadMango.Emulator.Bus.Interfaces.IBusFaultRing ring)
    {
        bool useJson = context.JsonOutput;
        if (useJson)
        {
            var st = new { capacity = ring.Capacity, count = ring.Count, total = ring.TotalFaults };
            context.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(st, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }
        context.Output.WriteLine("Bus Fault Log Status:");
        context.Output.WriteLine();
        context.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Buffer capacity:  {ring.Capacity}"));
        context.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Faults in buffer: {ring.Count}"));
        context.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Total recorded:   {ring.TotalFaults}"));
        return CommandResult.Ok();
    }
}