// <copyright file="DriveSnapshot.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Storage.Media;

/// <summary>
/// Immutable snapshot of a single Disk II / SmartPort drive's runtime state.
/// </summary>
/// <remarks>
/// Mirrors the per-drive debug surface required by PRD §6.2 FR-D10:
/// <c>motor</c>, <c>phase</c>, <c>quarterTrack</c>, <c>selected</c>, <c>writeProtect</c>,
/// <c>mountedImagePath</c>.
/// </remarks>
/// <param name="Selected">Whether this drive is the currently selected drive on the controller.</param>
/// <param name="MotorOn">Whether the motor is energized for this drive.</param>
/// <param name="PhaseLatch">Phase-magnet bit field (4-bit; bit <c>i</c> = phase <c>i</c> energized).</param>
/// <param name="QuarterTrack">Current head quarter-track position (0..<c>4 × trackCount − 1</c>).</param>
/// <param name="WriteProtect">Whether the inserted medium is reporting write-protect.</param>
/// <param name="HasMedia">Whether a medium is currently inserted.</param>
/// <param name="MountedImagePath">Path that produced the inserted medium, or <see langword="null"/> if unknown / empty.</param>
public readonly record struct DriveSnapshot(
    bool Selected,
    bool MotorOn,
    int PhaseLatch,
    int QuarterTrack,
    bool WriteProtect,
    bool HasMedia,
    string? MountedImagePath);