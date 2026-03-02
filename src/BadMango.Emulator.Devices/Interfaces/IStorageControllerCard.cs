// <copyright file="IStorageControllerCard.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Composite interface that combines host-side controller behavior with guest-visible slot-card behavior.
/// </summary>
/// <remarks>
/// <para>
/// This interface composes <see cref="IStorageController"/> and <see cref="ISlotCard"/> so implementations can expose
/// host-facing drive management while still participating in guest slot protocols such as
/// ROM region exposure and $C0n0-$C0nF I/O handlers.
/// </para>
/// <para>
/// Related bus abstractions:
/// <see cref="ISlotCard"/>,
/// <see cref="IPeripheral"/>.
/// </para>
/// </remarks>
public interface IStorageControllerCard : IStorageController, ISlotCard
{
}