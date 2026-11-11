// <copyright file="MachineVideoFrameSourceRegistry.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering;

using System.Runtime.CompilerServices;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Provides shared frame producers for machine instances.
/// </summary>
public static class MachineVideoFrameSourceRegistry
{
    private static readonly ConditionalWeakTable<IMachine, MachineVideoFrameProducer> Producers = new();

    /// <summary>
    /// Gets the shared frame source for a machine, creating and attaching one when needed.
    /// </summary>
    /// <param name="machine">The machine to resolve.</param>
    /// <returns>A machine-scoped frame source.</returns>
    public static IVideoFrameSource GetOrCreate(IMachine machine)
    {
        ArgumentNullException.ThrowIfNull(machine);
        return Producers.GetValue(machine, static attachedMachine =>
        {
            var producer = new MachineVideoFrameProducer();
            producer.AttachMachine(attachedMachine);
            return producer;
        });
    }
}