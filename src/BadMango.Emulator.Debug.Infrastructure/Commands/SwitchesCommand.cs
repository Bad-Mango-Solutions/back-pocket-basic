// <copyright file="SwitchesCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using System.Linq;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Lists current soft switch state flags.
/// </summary>
/// <remarks>
/// <para>
/// Displays the current state of soft switches in the system. Soft switches
/// are memory-mapped I/O locations that control various hardware features
/// in Apple II-compatible systems.
/// </para>
/// <para>
/// This command queries components that implement <see cref="ISoftSwitchProvider"/>
/// to gather and display their soft switch states.
/// </para>
/// </remarks>
public sealed class SwitchesCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SwitchesCommand"/> class.
    /// </summary>
    public SwitchesCommand()
        : base("switches", "List current soft switch state flags")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["sw", "softswitch"];

    /// <inheritdoc/>
    public override string Usage => "switches";

    /// <inheritdoc/>
    public string Synopsis => "switches";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays the current state of soft switches in the system. Soft switches " +
        "are memory-mapped I/O locations ($C000-$CFFF) that control hardware features " +
        "like text/graphics mode, memory banking, and peripheral access. This command " +
        "queries all components that implement ISoftSwitchProvider to gather switch states.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } =
    [
        new("--json", "-j", "flag", "Output switch states as JSON", "off"),
    ];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "switches                 Display soft switch state",
        "switches --json          Output as JSON",
        "sw                       Alias for switches",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["buslog", "read", "write"];

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

        bool useJson = context.JsonOutput ||
                       args.Any(a => a.Equals("--json", StringComparison.OrdinalIgnoreCase) ||
                                     a.Equals("-j", StringComparison.OrdinalIgnoreCase));

        // Try to find soft switch providers through the machine
        var providers = GetSoftSwitchProviders(debugContext);

        if (useJson)
        {
            var jsonData = providers.Select(p => new
            {
                provider = p.ProviderName,
                switches = p.GetSoftSwitchStates().Select(s => new
                {
                    name = s.Name,
                    address = $"${s.Address:X4}",
                    value = s.Value,
                    description = s.Description,
                }).ToList(),
            }).ToList();

            string json = System.Text.Json.JsonSerializer.Serialize(jsonData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            debugContext.Output.WriteLine(json);
            return CommandResult.Ok();
        }

        debugContext.Output.WriteLine("Soft Switch State:");
        debugContext.Output.WriteLine();

        if (providers.Count == 0)
        {
            debugContext.Output.WriteLine("  No soft switch providers found in this configuration.");
            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("Note: Soft switch state requires components that implement");
            debugContext.Output.WriteLine("      ISoftSwitchProvider (e.g., LanguageCardController,");
            debugContext.Output.WriteLine("      AuxiliaryMemoryController).");
        }
        else
        {
            foreach (var provider in providers)
            {
                DisplayProviderSwitches(debugContext, provider);
            }
        }

        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine("Common Apple II soft switch addresses:");
        debugContext.Output.WriteLine("  $C000-$C00F  Keyboard and memory switches");
        debugContext.Output.WriteLine("  $C010-$C01F  Status reads");
        debugContext.Output.WriteLine("  $C050-$C05F  Graphics/text mode switches");
        debugContext.Output.WriteLine("  $C080-$C08F  Language card bank switches");

        return CommandResult.Ok();
    }

    private static List<ISoftSwitchProvider> GetSoftSwitchProviders(IDebugContext context)
    {
        var providers = new List<ISoftSwitchProvider>();

        // If we have a machine, query it for soft switch providers
        if (context.Machine is not null)
        {
            var machineProviders = context.Machine.GetComponents<ISoftSwitchProvider>();
            providers.AddRange(machineProviders);
        }

        return providers;
    }

    private static void DisplayProviderSwitches(IDebugContext context, ISoftSwitchProvider provider)
    {
        context.Output.WriteLine($"  {provider.ProviderName}:");

        var switches = provider.GetSoftSwitchStates();
        foreach (var sw in switches)
        {
            string state = sw.Value ? "ON " : "OFF";
            string address = $"${sw.Address:X4}";
            string description = sw.Description ?? string.Empty;

            if (!string.IsNullOrEmpty(description))
            {
                context.Output.WriteLine($"    {sw.Name,-16} {state}  ({address})  {description}");
            }
            else
            {
                context.Output.WriteLine($"    {sw.Name,-16} {state}  ({address})");
            }
        }

        context.Output.WriteLine();
    }
}