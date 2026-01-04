// <copyright file="IMotherboardDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// A motherboard-integrated peripheral that registers soft switch handlers.
/// </summary>
/// <remarks>
/// <para>
/// Motherboard devices are built into the Apple II and have no slot number.
/// They register their soft switch handlers directly with the
/// <see cref="IOPageDispatcher"/> during initialization.
/// </para>
/// </remarks>
public interface IMotherboardDevice : IPeripheral
{
    /// <summary>
    /// Registers this device's soft switch handlers with the dispatcher.
    /// </summary>
    /// <param name="dispatcher">The I/O page dispatcher.</param>
    void RegisterHandlers(IOPageDispatcher dispatcher);
}