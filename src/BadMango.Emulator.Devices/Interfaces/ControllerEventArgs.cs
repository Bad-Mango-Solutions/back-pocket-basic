// <copyright file="ControllerEventArgs.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Event payload for controller drive topology or drive-state transitions.
/// </summary>
/// <param name="changeKind">The kind of drive transition being reported.</param>
/// <param name="drive">The drive associated with the transition, when applicable.</param>
public sealed class ControllerEventArgs(ControllerDriveChangeKind changeKind, IDrive? drive = null)
    : EventArgs
{
    /// <summary>
    /// Gets the kind of drive transition being reported.
    /// </summary>
    public ControllerDriveChangeKind ChangeKind { get; } = changeKind;

    /// <summary>
    /// Gets the drive associated with the transition, when one exists.
    /// </summary>
    public IDrive? Drive { get; } = drive;
}