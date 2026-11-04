// <copyright file="DiskIIStatusExtension.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.UI.StatusMonitor;

using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

using BadMango.Emulator.Storage.Media;

/// <summary>
/// Status-window extension that surfaces live activity from a single
/// <see cref="IDiskController"/> (Disk II today; the same pattern is intended to
/// generalise to SmartPort and SCSI when those land).
/// </summary>
/// <remarks>
/// <para>
/// Renders a compact panel showing per-drive motor / head state alongside the
/// controller-wide counters returned by <see cref="IDiskController.GetActivitySnapshot"/>:
/// data-path reads (fresh / stale / settle-suppressed), phase pulses, track
/// changes, motor / drive-select transitions, cache loads and flushes, plus the
/// most recently observed address-field (volume / track / sector / checksum)
/// parsed from the live byte stream served to the CPU.
/// </para>
/// <para>
/// The extension is registered per-controller in
/// <see cref="Services.DebugWindowManager"/> when a status window is opened, so a
/// machine with multiple disk controllers (e.g. Disk II in slot 6 and SmartPort
/// in slot 5 later) will get one panel per controller, in slot order.
/// </para>
/// </remarks>
public sealed class DiskIIStatusExtension : IStatusWindowExtension
{
    private readonly IDiskController controller;
    private readonly int slotNumber;
    private TextBlock? counterDisplay;
    private TextBlock?[] driveDisplays = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIIStatusExtension"/> class.
    /// </summary>
    /// <param name="controller">The disk controller whose activity should be surfaced.</param>
    /// <param name="slotNumber">The slot the controller is installed in (1..7); used as the panel header label.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="controller"/> is <see langword="null"/>.</exception>
    public DiskIIStatusExtension(IDiskController controller, int slotNumber)
    {
        ArgumentNullException.ThrowIfNull(controller);
        this.controller = controller;
        this.slotNumber = slotNumber;
    }

    /// <inheritdoc/>
    public string Name => string.Format(CultureInfo.InvariantCulture, "Disk Controller (Slot {0})", slotNumber);

    /// <inheritdoc/>
    /// <remarks>
    /// Disk controllers sort after the PocketWatch (order 100) and before any future
    /// extensions; using 200 + slotNumber preserves ordering across multiple controllers.
    /// </remarks>
    public int Order => 200 + slotNumber;

    /// <inheritdoc/>
    public Control CreateControl()
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0, 5, 0, 0),
        };

        var header = new TextBlock
        {
            Text = string.Format(CultureInfo.InvariantCulture, "💾 Disk Controller (Slot {0})", slotNumber),
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.CornflowerBlue,
            Margin = new Thickness(0, 0, 0, 5),
        };
        panel.Children.Add(header);

        counterDisplay = new TextBlock
        {
            Text = "(no activity yet)",
            FontSize = 10,
            FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
            Foreground = Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 5),
        };
        panel.Children.Add(counterDisplay);

        driveDisplays = new TextBlock?[controller.DriveCount];
        for (var i = 0; i < controller.DriveCount; i++)
        {
            var driveBlock = new TextBlock
            {
                Text = string.Format(CultureInfo.InvariantCulture, "Drive {0}: (no activity yet)", i + 1),
                FontSize = 10,
                FontFamily = new FontFamily("Cascadia Code, Consolas, Courier New, monospace"),
                Foreground = Brushes.DarkGray,
                Margin = new Thickness(8, 0, 0, 2),
            };
            driveDisplays[i] = driveBlock;
            panel.Children.Add(driveBlock);
        }

        return panel;
    }

    /// <inheritdoc/>
    public void UpdateDisplay()
    {
        if (counterDisplay is null)
        {
            return;
        }

        DiskActivitySnapshot snapshot;
        try
        {
            snapshot = controller.GetActivitySnapshot();
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException)
        {
            counterDisplay.Text = string.Format(CultureInfo.InvariantCulture, "(activity unavailable: {0})", ex.Message);
            return;
        }

        counterDisplay.Text = string.Format(
            CultureInfo.InvariantCulture,
            "reads={0} fresh={1} stale={2} settle={3} writes={4}\nstep-pulses={5} track-changes={6} motor={7}/{8} cache-load={9} flush={10}",
            snapshot.DataReadCount,
            snapshot.FreshByteCount,
            snapshot.StaleByteCount,
            snapshot.SettleSuppressedReadCount,
            snapshot.DataWriteCount,
            snapshot.PhasePulseCount,
            snapshot.TrackChangeCount,
            snapshot.MotorOnCount,
            snapshot.MotorOffCount,
            snapshot.CacheLoadCount,
            snapshot.CacheFlushCount);

        for (var i = 0; i < driveDisplays.Length; i++)
        {
            var block = driveDisplays[i];
            if (block is null)
            {
                continue;
            }

            DriveSnapshot drive;
            try
            {
                drive = controller.GetDriveSnapshot(i);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
            {
                block.Text = string.Format(CultureInfo.InvariantCulture, "Drive {0}: <unreadable: {1}>", i + 1, ex.Message);
                continue;
            }

            var activity = i < snapshot.Drives.Count
                ? snapshot.Drives[i]
                : default;

            string addressLine;
            if (activity.LastObservedVolume is { } vol
                && activity.LastObservedTrack is { } trk
                && activity.LastObservedSector is { } sec)
            {
                var valid = activity.LastObservedChecksumValid ?? false;
                addressLine = string.Format(
                    CultureInfo.InvariantCulture,
                    "last-addr vol=${0:X2} trk=${1:X2} sec=${2:X2} {3}",
                    vol,
                    trk,
                    sec,
                    valid ? "ok" : "BAD");
            }
            else
            {
                addressLine = "last-addr: none";
            }

            string dataLine;
            if (activity.LastDataPrologueGapBytes is { } lastGap
                && activity.MaxDataPrologueGapBytes is { } maxGap)
            {
                dataLine = string.Format(
                    CultureInfo.InvariantCulture,
                    "data: prolog={0} ok={1} csum-err={2} dec-err={3} epi-mis={4} gap={5}(max {6})",
                    activity.ObservedDataPrologues,
                    activity.ObservedDataFieldDecodeSuccesses,
                    activity.ObservedDataFieldChecksumErrors,
                    activity.ObservedDataFieldDecodeErrors,
                    activity.ObservedDataFieldEpilogueMismatches,
                    lastGap,
                    maxGap);
            }
            else
            {
                dataLine = string.Format(
                    CultureInfo.InvariantCulture,
                    "data: prolog={0} ok={1} csum-err={2} dec-err={3} epi-mis={4} gap=-",
                    activity.ObservedDataPrologues,
                    activity.ObservedDataFieldDecodeSuccesses,
                    activity.ObservedDataFieldChecksumErrors,
                    activity.ObservedDataFieldDecodeErrors,
                    activity.ObservedDataFieldEpilogueMismatches);
            }

            block.Text = string.Format(
                CultureInfo.InvariantCulture,
                "Drive {0}: {1} motor={2} qt={3} on-track={4}b addr-fields={5} ({6} bad)\n  {7}\n  {8}",
                i + 1,
                drive.HasMedia ? "mounted" : "empty",
                drive.MotorOn ? "on" : "off",
                drive.QuarterTrack,
                activity.BytesServedOnCurrentTrack,
                activity.ObservedAddressFields,
                activity.ObservedAddressFieldChecksumErrors,
                addressLine,
                dataLine);
        }
    }
}