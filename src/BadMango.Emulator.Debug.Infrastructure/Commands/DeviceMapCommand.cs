// <copyright file="DeviceMapCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Prints a tree of all devices registered in the system.
/// </summary>
/// <remarks>
/// <para>
/// Displays all devices from the DeviceRegistry in a hierarchical tree format,
/// showing the device hierarchy including slot and slot-card relationships.
/// </para>
/// <para>
/// This command requires a machine with a device registry to be attached.
/// </para>
/// </remarks>
public sealed class DeviceMapCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceMapCommand"/> class.
    /// </summary>
    public DeviceMapCommand()
        : base("devicemap", "Print tree of all registered devices")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["devices", "devmap"];

    /// <inheritdoc/>
    public override string Usage => "devicemap";

    /// <inheritdoc/>
    public string Synopsis => "devicemap";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays all devices from the DeviceRegistry in a table format, showing " +
        "device ID, kind, name, and wiring path. Useful for understanding the current " +
        "machine configuration and verifying device registration. Requires a bus-based " +
        "system with a device registry.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "devicemap               Display all registered devices",
        "devices                 Alias for devicemap",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["regions", "pages", "profile"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (!debugContext.IsBusAttached || debugContext.Bus is null)
        {
            return CommandResult.Error("No bus attached. This command requires a bus-based system.");
        }

        // Try to get device registry from the machine
        IDeviceRegistry? registry = null;
        if (debugContext.Machine is IMachine machine)
        {
            registry = machine.Devices;
        }

        if (registry is null || registry.Count == 0)
        {
            debugContext.Output.WriteLine("Device Registry:");
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("  No devices registered.");
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("Note: Device information is only available when a full machine is attached.");
            return CommandResult.Ok();
        }

        debugContext.Output.WriteLine("Device Registry:");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"{"ID",-6} {"Kind",-16} {"Name",-24} {"Wiring Path"}");
        debugContext.Output.WriteLine(new string('─', 80));

        // Sort devices by wiring path for hierarchical display
        var devices = registry.GetAll().OrderBy(d => d.WiringPath).ToList();

        foreach (var device in devices)
        {
            string indent = GetIndent(device.WiringPath);
            string displayName = device.Name;

            debugContext.Output.WriteLine($"{device.Id,-6} {device.Kind,-16} {indent}{displayName,-24} {device.WiringPath}");
        }

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine($"Total devices: {registry.Count}");

        return CommandResult.Ok();
    }

    private static string GetIndent(string wiringPath)
    {
        // Count the depth based on '/' separators in the wiring path
        int depth = wiringPath.Count(c => c == '/');
        return new(' ', depth * 2);
    }
}