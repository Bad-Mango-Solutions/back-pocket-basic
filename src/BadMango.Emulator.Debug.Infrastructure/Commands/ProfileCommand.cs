// <copyright file="ProfileCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

/// <summary>
/// Shows the current machine profile in a human-readable format.
/// </summary>
/// <remarks>
/// <para>
/// Displays configuration information about the current machine, including
/// CPU type, memory size, and any attached peripherals.
/// </para>
/// <para>
/// This command requires machine info to be attached to the debug context.
/// </para>
/// </remarks>
public sealed class ProfileCommand : CommandHandlerBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileCommand"/> class.
    /// </summary>
    public ProfileCommand()
        : base("profile", "Show current machine profile")
    {
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["machine", "info"];

    /// <inheritdoc/>
    public override string Usage => "profile";

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        debugContext.Output.WriteLine("Machine Profile:");
        debugContext.Output.WriteLine();

        // Display MachineInfo if available
        if (debugContext.MachineInfo is not null)
        {
            var info = debugContext.MachineInfo;
            debugContext.Output.WriteLine($"  Name:         {info.DisplayName}");
            debugContext.Output.WriteLine($"  ID:           {info.Name}");
            debugContext.Output.WriteLine($"  CPU:          {info.CpuType}");
            debugContext.Output.WriteLine($"  Memory:       {FormatMemorySize((int)info.MemorySize)}");
        }
        else
        {
            debugContext.Output.WriteLine("  No machine profile available.");
        }

        debugContext.Output.WriteLine();

        // Display CPU info if available
        if (debugContext.Cpu is not null)
        {
            var cpu = debugContext.Cpu;
            debugContext.Output.WriteLine("CPU Status:");
            debugContext.Output.WriteLine($"  PC:           ${cpu.GetPC():X4}");
            debugContext.Output.WriteLine($"  Halted:       {cpu.Halted}");

            if (cpu.Halted)
            {
                debugContext.Output.WriteLine($"  Halt Reason:  {cpu.HaltReason}");
            }
        }

        // Display bus info if available
        if (debugContext.Bus is not null)
        {
            var bus = debugContext.Bus;
            var pageSize = 1 << bus.PageShift;
            var totalMemory = bus.PageCount * pageSize;

            debugContext.Output.WriteLine();
            debugContext.Output.WriteLine("Memory Bus:");
            debugContext.Output.WriteLine($"  Page Count:   {bus.PageCount}");
            debugContext.Output.WriteLine($"  Page Size:    {FormatMemorySize(pageSize)}");
            debugContext.Output.WriteLine($"  Address Space: {FormatMemorySize(totalMemory)}");
        }

        return CommandResult.Ok();
    }

    private static string FormatMemorySize(int bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return $"{bytes / (1024 * 1024)} MB ({bytes:N0} bytes)";
        }

        if (bytes >= 1024)
        {
            return $"{bytes / 1024} KB ({bytes:N0} bytes)";
        }

        return $"{bytes} bytes";
    }
}