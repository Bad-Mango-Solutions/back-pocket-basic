// <copyright file="DiskActivitySnapshot.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Media;

/// <summary>
/// Aggregated activity counters for a single disk controller, surfaced by
/// <see cref="IDiskController.GetActivitySnapshot"/> for diagnostic UIs
/// (the <c>diskmon</c> debug command and the Disk II status-window extension).
/// </summary>
/// <remarks>
/// <para>
/// All counters are monotonically increasing since the controller was instantiated.
/// They are updated on the bus / scheduler thread; consumers should snapshot the
/// entire record rather than reading individual properties piecemeal.
/// </para>
/// <para>
/// Counters are intentionally lightweight (simple integer adds) so they can be
/// updated on every <c>$C0nC</c> / <c>$C0nE</c> access without measurably degrading
/// emulation performance. No logging is emitted from the hot path; surfacing is
/// pull-based via this snapshot.
/// </para>
/// </remarks>
public sealed class DiskActivitySnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiskActivitySnapshot"/> class.
    /// </summary>
    /// <param name="dataReadCount">Total reads of the data-path soft switches in read mode.</param>
    /// <param name="freshByteCount">Reads where a new nibble was actually clocked off the head (byte-ready high).</param>
    /// <param name="settleSuppressedReadCount">Reads suppressed (bit 7 cleared) because the head was settling.</param>
    /// <param name="staleByteCount">Reads where the CPU polled faster than bytes arrive (bit 7 cleared, no fresh byte).</param>
    /// <param name="dataWriteCount">Write-shift operations that committed a byte to the cached track.</param>
    /// <param name="phasePulseCount">Phase-energize events that produced head movement.</param>
    /// <param name="phaseNoOpCount">Phase-energize events that produced no head movement (already in position, clamped, etc.).</param>
    /// <param name="trackChangeCount">Distinct quarter-track transitions.</param>
    /// <param name="motorOnCount">Soft-switch motor-on transitions.</param>
    /// <param name="motorOffCount">Soft-switch motor-off transitions.</param>
    /// <param name="driveSelectCount">Drive-select transitions where the active drive actually changed.</param>
    /// <param name="cacheLoadCount">Number of times a quarter-track was loaded into the controller's nibble cache.</param>
    /// <param name="cacheFlushCount">Number of times a dirty cached track was flushed back to the medium.</param>
    /// <param name="drives">Per-drive activity records, in drive-index order.</param>
    public DiskActivitySnapshot(
        long dataReadCount,
        long freshByteCount,
        long settleSuppressedReadCount,
        long staleByteCount,
        long dataWriteCount,
        long phasePulseCount,
        long phaseNoOpCount,
        long trackChangeCount,
        long motorOnCount,
        long motorOffCount,
        long driveSelectCount,
        long cacheLoadCount,
        long cacheFlushCount,
        IReadOnlyList<DiskDriveActivity> drives)
    {
        ArgumentNullException.ThrowIfNull(drives);
        this.DataReadCount = dataReadCount;
        this.FreshByteCount = freshByteCount;
        this.SettleSuppressedReadCount = settleSuppressedReadCount;
        this.StaleByteCount = staleByteCount;
        this.DataWriteCount = dataWriteCount;
        this.PhasePulseCount = phasePulseCount;
        this.PhaseNoOpCount = phaseNoOpCount;
        this.TrackChangeCount = trackChangeCount;
        this.MotorOnCount = motorOnCount;
        this.MotorOffCount = motorOffCount;
        this.DriveSelectCount = driveSelectCount;
        this.CacheLoadCount = cacheLoadCount;
        this.CacheFlushCount = cacheFlushCount;
        this.Drives = drives;
    }

    /// <summary>Gets the total number of data-path reads in read mode (Q6=0, Q7=0).</summary>
    public long DataReadCount { get; }

    /// <summary>Gets the number of reads that returned a freshly clocked nibble (bit 7 set).</summary>
    public long FreshByteCount { get; }

    /// <summary>Gets the number of reads suppressed by the head-settle window (bit 7 cleared).</summary>
    public long SettleSuppressedReadCount { get; }

    /// <summary>Gets the number of reads where the CPU polled before a fresh byte arrived (bit 7 cleared).</summary>
    public long StaleByteCount { get; }

    /// <summary>Gets the total number of byte-writes shifted onto the cached track.</summary>
    public long DataWriteCount { get; }

    /// <summary>Gets the number of phase pulses that moved the head.</summary>
    public long PhasePulseCount { get; }

    /// <summary>Gets the number of phase pulses that did not move the head.</summary>
    public long PhaseNoOpCount { get; }

    /// <summary>Gets the number of distinct quarter-track changes.</summary>
    public long TrackChangeCount { get; }

    /// <summary>Gets the number of motor-on transitions.</summary>
    public long MotorOnCount { get; }

    /// <summary>Gets the number of motor-off transitions.</summary>
    public long MotorOffCount { get; }

    /// <summary>Gets the number of drive-select transitions where the active drive actually changed.</summary>
    public long DriveSelectCount { get; }

    /// <summary>Gets the number of times a quarter-track nibble buffer was loaded.</summary>
    public long CacheLoadCount { get; }

    /// <summary>Gets the number of times a dirty cached track was flushed back to the medium.</summary>
    public long CacheFlushCount { get; }

    /// <summary>Gets the per-drive activity records, in drive-index order.</summary>
    public IReadOnlyList<DiskDriveActivity> Drives { get; }
}