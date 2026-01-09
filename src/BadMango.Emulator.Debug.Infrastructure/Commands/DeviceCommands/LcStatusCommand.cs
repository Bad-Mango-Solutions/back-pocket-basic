// <copyright file="LcStatusCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using BadMango.Emulator.Devices;

/// <summary>
/// Displays Language Card status and memory configuration.
/// </summary>
/// <remarks>
/// <para>
/// Shows the current state of the Language Card including:
/// </para>
/// <list type="bullet">
/// <item><description>RAM read enable state (ROM vs RAM at $D000-$FFFF)</description></item>
/// <item><description>RAM write enable state</description></item>
/// <item><description>Selected bank (1 or 2) for $D000-$DFFF</description></item>
/// <item><description>R�2 protocol state</description></item>
/// <item><description>Soft switch addresses and their effects</description></item>
/// </list>
/// </remarks>
[DeviceDebugCommand]
public sealed class LcStatusCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LcStatusCommand"/> class.
    /// </summary>
    public LcStatusCommand()
        : base("lcstatus", "Display Language Card status and configuration")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["lc", "langcard"];

    /// <inheritdoc/>
    public override string Usage => "lcstatus";

    /// <inheritdoc/>
    public string Synopsis => "lcstatus";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Displays the current state of the Language Card, including RAM read/write enable, " +
        "bank selection, and R�2 protocol state.\n\n" +
        "The Language Card provides 16KB of RAM at $D000-$FFFF:\n" +
        "  - $D000-$DFFF: Two 4KB banks (Bank 1 or Bank 2)\n" +
        "  - $E000-$FFFF: One 8KB bank\n\n" +
        "Soft switches at $C080-$C08F control the Language Card:\n" +
        "  Bit 0: Write enable address (odd = can enable writes)\n" +
        "  Bits 0,1: RAM read (00 or 11 = RAM, 01 or 10 = ROM)\n" +
        "  Bit 3: Bank select (0 = Bank 2, 1 = Bank 1)";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "lcstatus                  Show Language Card status",
    ];

    /// <inheritdoc/>
    public string? SideEffects => null;

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["switches", "pages", "regions"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        var machine = debugContext.Machine;
        if (machine is null)
        {
            return CommandResult.Error("No machine attached.");
        }

        // Try to find the Language Card device
        var languageCard = machine.GetComponent<LanguageCardDevice>();
        if (languageCard is null)
        {
            return CommandResult.Error("No Language Card found in this system.");
        }

        // Display status
        debugContext.Output.WriteLine("Language Card Status");
        debugContext.Output.WriteLine("====================");
        debugContext.Output.WriteLine();

        // Current state
        debugContext.Output.WriteLine("Current State:");
        debugContext.Output.WriteLine($"  RAM Read:    {(languageCard.IsRamReadEnabled ? "ENABLED (RAM visible)" : "DISABLED (ROM visible)")}");
        debugContext.Output.WriteLine($"  RAM Write:   {(languageCard.IsRamWriteEnabled ? "ENABLED" : "DISABLED")}");
        debugContext.Output.WriteLine($"  Bank:        Bank {languageCard.SelectedBank} selected for $D000-$DFFF");
        debugContext.Output.WriteLine();

        // Memory layout
        debugContext.Output.WriteLine("Memory Layout:");
        if (languageCard.IsRamReadEnabled)
        {
            string writeStatus = languageCard.IsRamWriteEnabled ? "R/W" : "R/O";
            debugContext.Output.WriteLine($"  $D000-$DFFF: LC RAM Bank {languageCard.SelectedBank} ({writeStatus})");
            debugContext.Output.WriteLine($"  $E000-$FFFF: LC RAM ({writeStatus})");
        }
        else
        {
            debugContext.Output.WriteLine("  $D000-$DFFF: ROM");
            debugContext.Output.WriteLine("  $E000-$FFFF: ROM");
        }

        debugContext.Output.WriteLine();

        // Soft switch reference
        debugContext.Output.WriteLine("Soft Switch Reference ($C080-$C08F):");
        debugContext.Output.WriteLine("  Address  Read Effect             Write Effect");
        debugContext.Output.WriteLine("  -------  ----------------------  ------------");
        debugContext.Output.WriteLine("  $C080    RAM read, Bank 2        (no effect)");
        debugContext.Output.WriteLine("  $C081    ROM read, Bank 2        (no effect)");
        debugContext.Output.WriteLine("  $C082    ROM read, Bank 2        (no effect)");
        debugContext.Output.WriteLine("  $C083    RAM read+write*, Bk2    (no effect)");
        debugContext.Output.WriteLine("  $C088    RAM read, Bank 1        (no effect)");
        debugContext.Output.WriteLine("  $C089    ROM read, Bank 1        (no effect)");
        debugContext.Output.WriteLine("  $C08A    ROM read, Bank 1        (no effect)");
        debugContext.Output.WriteLine("  $C08B    RAM read+write*, Bk1    (no effect)");
        debugContext.Output.WriteLine();
        debugContext.Output.WriteLine("* Write enable requires R�2 protocol (two consecutive reads");
        debugContext.Output.WriteLine("  of the same odd address). Writes to these switches are ignored.");

        return CommandResult.Ok();
    }
}