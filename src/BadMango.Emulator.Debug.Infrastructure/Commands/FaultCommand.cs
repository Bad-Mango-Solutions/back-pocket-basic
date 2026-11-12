// <copyright file="FaultCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Globalization;

/// <summary>
/// Shows details about the most recent bus fault.
/// </summary>
/// <remarks>
/// <para>
/// Displays information about the last bus fault that occurred, including the
/// faulting address, fault kind, and any additional context available.
/// </para>
/// <para>
/// Bus fault recording is off by default; the first use of this command
/// (or <c>buslog</c>) enables it, so only faults from that point on are shown.
/// </para>
/// <para>
/// This command requires a bus to be attached to the debug context.
/// </para>
/// </remarks>
public sealed class FaultCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FaultCommand"/> class.
    /// </summary>
    public FaultCommand()
        : base("fault", "Show most recent bus fault details")
    {
    }

    /// <inheritdoc/>
    public override string Usage => "fault";

    /// <inheritdoc/>
    public string Synopsis => "fault";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays information about the most recent bus fault recorded by " +
        "the memory bus, including the faulting address and fault kind. " +
        "Useful for debugging unmapped memory access, permission violations, " +
        "and silent floating-bus reads. Shows 'No fault' if no fault has " +
        "occurred since the ring was last cleared. Recording is off by " +
        "default; the first use of 'fault' or 'buslog' turns it on, so only " +
        "faults from that point on are captured.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "fault                    Show last bus fault status",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Enables bus fault recording if it is not already on.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["buslog", "regions", "pages"];

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
            debugContext.Output.WriteLine("Bus Fault Status:");
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("  No fault ring is configured for this bus.");
            return CommandResult.Ok();
        }

        BusLogCommand.EnsureRecordingEnabled(debugContext.Bus, debugContext.Output);

        debugContext.Output.WriteLine("Bus Fault Status:");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Ring capacity:    {ring.Capacity}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Faults in buffer: {ring.Count}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Total recorded:   {ring.TotalFaults}"));
        debugContext.Output.WriteLine();

        var last = ring.Last;
        if (last is null)
        {
            if (useJson)
            {
                debugContext.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { hasFault = false, ring = new { capacity = ring.Capacity, count = ring.Count, total = ring.TotalFaults } }));
            }
            else
            {
                debugContext.Output.WriteLine("  No fault recorded.");
            }
            return CommandResult.Ok();
        }

        var f = last.Value;
        if (useJson)
        {
            debugContext.Output.WriteLine(System.Text.Json.JsonSerializer.Serialize(new {
                hasFault = true,
                kind = f.Kind.ToString(),
                address = $"${f.Address:X4}",
                widthBits = f.WidthBits,
                intent = f.Intent.ToString(),
                mode = f.Mode.ToString(),
                deviceId = f.DeviceId,
                region = f.RegionTag
            }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            return CommandResult.Ok();
        }

        debugContext.Output.WriteLine("Most recent fault:");
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Kind:       {f.Kind}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Address:    ${f.Address:X4}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Width:      {f.WidthBits} bits"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Intent:     {f.Intent}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Mode:       {f.Mode}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  DeviceId:   {f.DeviceId}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Region:     {f.RegionTag}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  SourceId:   {f.SourceId}"));
        debugContext.Output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"  Cycle:      {f.Cycle}"));

        return CommandResult.Ok();
    }
}