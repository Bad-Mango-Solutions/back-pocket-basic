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
/// A brief delay (1 second by default) is applied before booting to give
/// the user time to hold down any modifier keys (such as Open Apple or
/// Closed Apple) that affect the boot process.
/// </para>
/// <para>
/// After boot, use 'pause' to suspend execution, 'resume' to continue,
/// or 'halt' to force a complete stop.
/// </para>
/// <para>
/// If the machine profile has <c>autoVideoWindowOpen</c> set to <see langword="true"/>, the video window will be opened automatically when the machine boots.
/// </para>
/// </remarks>
public sealed class BootCommand : CommandHandlerBase, ICommandHelp
{
    /// <summary>
    /// Default delay in milliseconds before booting to allow modifier keys to be pressed.
    /// </summary>
    private const int DefaultBootDelayMs = 1000;

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
        "A brief delay is applied before booting to allow time to hold modifier keys " +
        "(Open Apple, Closed Apple) that affect the boot process. " +
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

        // Start boot with delay to allow modifier keys to be pressed
        _ = BootWithDelayAsync(debugContext, DefaultBootDelayMs);

        string message = autoOpenVideo && windowManager is not null
            ? $"Booting in {DefaultBootDelayMs / 1000.0:F1}s... Hold modifier keys now. Video window opened."
            : $"Booting in {DefaultBootDelayMs / 1000.0:F1}s... Hold modifier keys (Open Apple, Closed Apple) now.";

        return CommandResult.Ok(message);
    }

    private static async Task BootWithDelayAsync(IDebugContext debugContext, int delayMs)
    {
        try
        {
            // Wait to give user time to press modifier keys
            await Task.Delay(delayMs).ConfigureAwait(false);

            // Now boot the machine
            await debugContext.Machine!.BootAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            debugContext.Error.WriteLine($"Boot error: {ex.Message}");
        }
    }
}