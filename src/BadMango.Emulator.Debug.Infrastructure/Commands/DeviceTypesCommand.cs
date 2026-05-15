// <copyright file="DeviceTypesCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Devices;

/// <summary>
/// Lists every device type that has been auto-discovered by
/// <see cref="DeviceFactoryRegistry"/>, regardless of whether the active
/// machine profile actually instantiates it.
/// </summary>
/// <remarks>
/// <para>
/// This is the catalog of <em>available</em> devices — what a profile
/// <c>devices.motherboard[].type</c> or <c>devices.slots.cards[].type</c>
/// entry can refer to. Use <c>devicemap</c> instead to see the devices that
/// the current profile actually wired up.
/// </para>
/// <para>
/// The output is split into three sections:
/// </para>
/// <list type="bullet">
/// <item><description>Motherboard device factories registered with the registry.</description></item>
/// <item><description>Slot card factories registered with the registry.</description></item>
/// <item><description>Types that were discovered (annotated with <see cref="DeviceTypeAttribute"/>)
/// but could not be auto-registered, with the diagnostic reason from
/// <see cref="DeviceFactoryRegistry.SkippedDeviceTypes"/>.</description></item>
/// </list>
/// </remarks>
public sealed class DeviceTypesCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceTypesCommand"/> class.
    /// </summary>
    public DeviceTypesCommand()
        : base("devicetypes", "List all auto-discovered device types available for profiles")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["devtypes", "devicecatalog"];

    /// <inheritdoc/>
    public override string Usage => "devicetypes";

    /// <inheritdoc/>
    public string Synopsis => "devicetypes";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Lists every device type registered with the DeviceFactoryRegistry — i.e. every " +
        "type id that a machine profile may reference in devices.motherboard[].type or " +
        "devices.slots.cards[].type, regardless of whether the currently loaded profile " +
        "uses it. Also reports types that were discovered (annotated with [DeviceType]) " +
        "but could not be auto-registered, together with the diagnostic reason. Use " +
        "'devicemap' to see devices that the active profile actually wired up.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "devicetypes             List every auto-discovered device type",
        "devtypes                Alias for devicetypes",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["devicemap", "profile"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        DeviceFactoryRegistry.EnsureInitialized();

        var output = context.Output;

        var motherboard = DeviceFactoryRegistry.MotherboardDeviceFactories.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        var slotCards = DeviceFactoryRegistry.SlotCardFactories.Keys
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
        var skipped = DeviceFactoryRegistry.SkippedDeviceTypes
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).ToList();

        output.WriteLine("Available Device Types:");
        output.WriteLine();

        output.WriteLine($"Motherboard devices ({motherboard.Count}):");
        if (motherboard.Count == 0)
        {
            output.WriteLine("  (none)");
        }
        else
        {
            foreach (var typeId in motherboard)
            {
                output.WriteLine($"  {typeId}");
            }
        }

        output.WriteLine();
        output.WriteLine($"Slot cards ({slotCards.Count}):");
        if (slotCards.Count == 0)
        {
            output.WriteLine("  (none)");
        }
        else
        {
            foreach (var typeId in slotCards)
            {
                output.WriteLine($"  {typeId}");
            }
        }

        output.WriteLine();
        output.WriteLine($"Skipped (not auto-registrable) ({skipped.Count}):");
        if (skipped.Count == 0)
        {
            output.WriteLine("  (none)");
        }
        else
        {
            foreach (var (type, reason) in skipped)
            {
                output.WriteLine($"  {type}");
                output.WriteLine($"    reason: {reason}");
            }
        }

        return CommandResult.Ok();
    }
}