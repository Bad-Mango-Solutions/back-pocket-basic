// <copyright file="DiskRuntimeHelpers.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Commands.DeviceCommands;

using System.Globalization;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Helpers shared by the runtime <c>disk</c> subcommands
/// (<see cref="DiskListCommand"/>, <see cref="DiskInsertCommand"/>,
/// <see cref="DiskEjectCommand"/>, <see cref="DiskFlushCommand"/>).
/// </summary>
/// <remarks>
/// <para>
/// Centralises <c>&lt;slot&gt;:&lt;drive&gt;</c> parsing and controller / slot-manager
/// resolution so the four runtime subcommands present consistent error messages and
/// behaviour. Slot numbers are 1..7 (matching <see cref="ISlotManager"/>); drive numbers
/// are 1-based on the user-facing surface and are translated to the controller's
/// 0-based <see cref="IDiskController"/> drive index.
/// </para>
/// </remarks>
internal static class DiskRuntimeHelpers
{
    /// <summary>
    /// Resolves the live <see cref="ISlotManager"/> from a debug context, or returns a
    /// human-readable error suitable for surfacing back to the console.
    /// </summary>
    /// <param name="context">The debug command context.</param>
    /// <param name="slotManager">When successful, the resolved slot manager.</param>
    /// <param name="error">When unsuccessful, a clear error message; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when a slot manager was resolved.</returns>
    public static bool TryGetSlotManager(ICommandContext context, out ISlotManager? slotManager, out string? error)
    {
        slotManager = null;
        error = null;

        if (context is not IDebugContext debugContext)
        {
            error = "Debug context required for this command.";
            return false;
        }

        var machine = debugContext.Machine;
        if (machine is null)
        {
            error = "No machine attached.";
            return false;
        }

        slotManager = machine.GetComponent<ISlotManager>();
        if (slotManager is null)
        {
            error = "No slot manager available on the attached machine.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Parses a <c>&lt;slot&gt;:&lt;drive&gt;</c> token (e.g. <c>"6:1"</c>) into its
    /// 1-based slot and 1-based drive components.
    /// </summary>
    /// <param name="token">The argument string supplied on the command line.</param>
    /// <param name="slot">When successful, the parsed slot in <c>1..7</c>.</param>
    /// <param name="driveOneBased">When successful, the parsed 1-based drive number.</param>
    /// <param name="error">When unsuccessful, a clear error message.</param>
    /// <returns><see langword="true"/> when the token was parsed.</returns>
    public static bool TryParseSlotDrive(string token, out int slot, out int driveOneBased, out string? error)
    {
        slot = 0;
        driveOneBased = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            error = "Expected '<slot>:<drive>' (e.g. '6:1').";
            return false;
        }

        var colon = token.IndexOf(':');
        if (colon <= 0 || colon == token.Length - 1)
        {
            error = $"Expected '<slot>:<drive>' (e.g. '6:1'); got '{token}'.";
            return false;
        }

        var slotPart = token[..colon];
        var drivePart = token[(colon + 1)..];

        if (!int.TryParse(slotPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out slot)
            || slot < 1 || slot > 7)
        {
            error = $"Slot must be an integer in 1..7; got '{slotPart}'.";
            return false;
        }

        if (!int.TryParse(drivePart, NumberStyles.Integer, CultureInfo.InvariantCulture, out driveOneBased)
            || driveOneBased < 1)
        {
            error = $"Drive must be a positive integer; got '{drivePart}'.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves the <see cref="IDiskController"/> installed in <paramref name="slot"/>
    /// and validates that <paramref name="driveOneBased"/> is in range for that controller.
    /// </summary>
    /// <param name="slotManager">The active slot manager.</param>
    /// <param name="slot">1-based slot number.</param>
    /// <param name="driveOneBased">1-based drive number.</param>
    /// <param name="controller">When successful, the resolved disk controller.</param>
    /// <param name="driveIndex">When successful, the corresponding 0-based drive index.</param>
    /// <param name="error">When unsuccessful, a clear error message.</param>
    /// <returns><see langword="true"/> when both the controller and drive index are valid.</returns>
    public static bool TryGetController(
        ISlotManager slotManager,
        int slot,
        int driveOneBased,
        out IDiskController? controller,
        out int driveIndex,
        out string? error)
    {
        controller = null;
        driveIndex = driveOneBased - 1;
        error = null;

        var card = slotManager.GetCard(slot);
        if (card is null)
        {
            error = $"Slot {slot} is empty.";
            return false;
        }

        if (card is not IDiskController disk)
        {
            error = $"Slot {slot} ({card.Name}) is not a disk controller.";
            return false;
        }

        if (driveOneBased < 1 || driveOneBased > disk.DriveCount)
        {
            error = $"Drive must be in 1..{disk.DriveCount} for slot {slot}; got {driveOneBased}.";
            return false;
        }

        controller = disk;
        return true;
    }
}