// <copyright file="IBusFaultRecorder.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// A sink that captures <see cref="BusFault"/> events emitted by an
/// <see cref="IMemoryBus"/> implementation for later inspection.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are expected to be cheap on the hot path because the bus
/// calls <see cref="Record(in BusFault)"/> from inside <c>TryRead8</c> /
/// <c>TryWrite8</c> and from the silent failure branches of the fast-path
/// <c>Read8</c> / <c>Write8</c> methods. Implementations that allocate or
/// take long-held locks on every call will hurt emulator throughput.
/// </para>
/// <para>
/// The canonical implementation is <see cref="BusFaultRing"/>, a fixed-size
/// ring buffer that retains only the most recent faults. Debug tooling reads
/// from the same instance via <see cref="IBusFaultRing"/>.
/// </para>
/// </remarks>
public interface IBusFaultRecorder
{
    /// <summary>
    /// Records a bus fault that has just occurred.
    /// </summary>
    /// <param name="fault">The fault to record. Callers must only invoke
    /// this when <see cref="BusFault.IsFault"/> is <see langword="true"/>.</param>
    void Record(in BusFault fault);
}