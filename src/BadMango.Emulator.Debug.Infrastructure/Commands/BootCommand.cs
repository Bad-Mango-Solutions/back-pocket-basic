// <copyright file="BootCommand.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands;

using BadMango.Emulator.Core.Configuration;

/// <summary>
/// Boots the machine by performing a reset and starting execution.
/// </summary>
/// <remarks>
/// <para>
/// This command combines a reset operation with starting execution.
/// The machine is reset to its initial state, then begins running
/// in the background, allowing the debugger to remain responsive.
/// </para>
/// <para>
/// After boot, use 'pause' to suspend execution, 'resume' to continue,
/// or 'halt' to force a complete stop.
/// </para>
/// <para>
/// If the machine profile has <c>autoVideoWindowOpen</c> set to <see langword="true"/>, /// the video window will be opened automatically when the machine boots.
/// </para>
/// </remarks>
public sealed class BootCommand : CommandHandlerBase, ICommandHelp
{
    private readonly MachineProfile? profile;
    private readonly IDebugWindowManager? windowManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootCommand"/> class.
    /// </summary>
    public BootCommand()
        : this(null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BootCommand"/> class with profile and window manager.
    /// </summary>
    /// <param name="profile">The machine profile containing boot configuration.</param>
    /// <param name="windowManager">The debug window manager for opening the video window.</param>
    public BootCommand(MachineProfile? profile, IDebugWindowManager? windowManager)
        : base("boot", "Reset and start machine running")
    {
        this.profile = profile;
        this.windowManager = windowManager;
    }

    /// <inheritdoc/>
    public override IReadOnlyList<string> Aliases { get; } = ["startup"];

    /// <inheritdoc/>
    public override string Usage => "boot";

    /// <inheritdoc/>
    public string Synopsis => "boot";

    /// <inheritdoc/>
    public string DetailedDescription =>
        "Resets the machine to its initial state and immediately starts execution. " +
        "This is equivalent to pressing the power/reset button on a real computer. " +
        "The CPU loads its reset vector and begins executing from the reset handler. " +
        "Execution runs in the background, keeping the debugger responsive. " +
        "If the profile has autoVideoWindowOpen enabled, the video window opens automatically.";

    /// <inheritdoc/>
    public IReadOnlyList<CommandOption> Options { get; } = [];

    /// <inheritdoc/>
    public IReadOnlyList<string> Examples { get; } =
    [
        "boot                    Reset and start the machine",
        "startup                 Alias for boot",
    ];

    /// <inheritdoc/>
    public string? SideEffects =>
        "Resets all CPU state (registers, flags, PC) and begins execution. " +
        "Memory and device state may be modified by executed code. " +
        "May open the video window if autoVideoWindowOpen is enabled in the profile.";

    /// <inheritdoc/>
    public IReadOnlyList<string> SeeAlso { get; } = ["reset", "pause", "resume", "halt", "video"];

    /// <inheritdoc/>
    public override CommandResult Execute(ICommandContext context, string[] args)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context is not IDebugContext debugContext)
        {
            return CommandResult.Error("Debug context required for this command.");
        }

        if (debugContext.Machine is null)
        {
            return CommandResult.Error("No machine attached to debug context.");
        }

        // Check if auto video window open is enabled
        bool autoOpenVideo = profile?.Boot?.AutoVideoWindowOpen ?? false;

        // Open video window if configured and window manager is available
        if (autoOpenVideo && windowManager is not null)
        {
            // Fire and forget - don't block the boot process
            _ = windowManager.ShowWindowAsync("Video", debugContext.Machine);
        }

        // Start boot asynchronously with error handling
        _ = debugContext.Machine.BootAsync().ContinueWith(
            task =>
            {
                if (task.IsFaulted && task.Exception != null)
                {
                    debugContext.Error.WriteLine($"Boot error: {task.Exception.InnerException?.Message ?? task.Exception.Message}");
                }
            },
            TaskScheduler.Default);

        string message = autoOpenVideo && windowManager is not null
            ? "Machine booted and running. Video window opened. Use 'pause' to suspend execution."
            : "Machine booted and running. Use 'pause' to suspend execution.";

        return CommandResult.Ok(message);
    }
}