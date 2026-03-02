// <copyright file="ControllerMetrics.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Captures host-observable metrics for a disk controller.
/// </summary>
/// <param name="DriveCount">The number of drives currently connected to the controller.</param>
/// <param name="ActiveDriveCount">The number of connected drives with media currently present.</param>
public readonly record struct ControllerMetrics(int DriveCount, int ActiveDriveCount)
{
    /// <summary>
    /// Converts metrics to a serializable key/value representation.
    /// </summary>
    /// <returns>A dictionary containing all <see cref="ControllerMetrics"/> values.</returns>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            [nameof(this.DriveCount)] = this.DriveCount,
            [nameof(this.ActiveDriveCount)] = this.ActiveDriveCount,
        };
    }
}