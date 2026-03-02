// <copyright file="DriveMetrics.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Storage;

/// <summary>
/// Captures host-observable metrics for a drive mechanism.
/// </summary>
/// <param name="IsRemovable">A value indicating whether this drive accepts removable media.</param>
/// <param name="IsMediaPresent">A value indicating whether media is currently attached or inserted.</param>
/// <param name="MotorOn">A value indicating whether the drive motor is currently active.</param>
/// <param name="HeadPosition">The current drive head position.</param>
/// <param name="SlotNumber">The host slot number associated with the drive.</param>
/// <param name="IsDoorOpen">A value indicating whether the media door is open.</param>
/// <param name="IsWriteProtected">A value indicating whether current media state blocks writes.</param>
public readonly record struct DriveMetrics(
    bool IsRemovable,
    bool IsMediaPresent,
    bool MotorOn,
    int HeadPosition,
    int SlotNumber,
    bool IsDoorOpen,
    bool IsWriteProtected)
{
    /// <summary>
    /// Converts metrics to a serializable key/value representation.
    /// </summary>
    /// <returns>A dictionary containing all <see cref="DriveMetrics"/> values.</returns>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            [nameof(this.IsRemovable)] = this.IsRemovable,
            [nameof(this.IsMediaPresent)] = this.IsMediaPresent,
            [nameof(this.MotorOn)] = this.MotorOn,
            [nameof(this.HeadPosition)] = this.HeadPosition,
            [nameof(this.SlotNumber)] = this.SlotNumber,
            [nameof(this.IsDoorOpen)] = this.IsDoorOpen,
            [nameof(this.IsWriteProtected)] = this.IsWriteProtected,
        };
    }
}