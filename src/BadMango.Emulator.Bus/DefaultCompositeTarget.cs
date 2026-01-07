// <copyright file="DefaultCompositeTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using System.Runtime.CompilerServices;

using Interfaces;

/// <summary>
/// A no-op bus target that simulates open-bus behavior for unmapped I/O regions.
/// </summary>
/// <remarks>
/// <para>
/// This target is used as the default handler for composite regions that have no
/// specific handler configured (handler is null, empty, or "default"). It provides
/// the expected behavior of an unmapped Apple II I/O region:
/// </para>
/// <list type="bullet">
/// <item><description>Reads return $FF (floating bus value)</description></item>
/// <item><description>Writes are silently ignored (no-op)</description></item>
/// </list>
/// <para>
/// This allows profiles to define composite regions without requiring a handler,
/// which is useful for:
/// </para>
/// <list type="bullet">
/// <item><description>Testing and bring-up before handlers are implemented</description></item>
/// <item><description>Placeholder I/O regions that will be filled in later</description></item>
/// <item><description>Minimal configurations that don't need full I/O support</description></item>
/// </list>
/// </remarks>
public sealed class DefaultCompositeTarget : IBusTarget
{
    /// <summary>
    /// The singleton instance of the default composite target.
    /// </summary>
    public static readonly DefaultCompositeTarget Instance = new();

    /// <summary>
    /// The value returned for all reads (floating bus).
    /// </summary>
    private const byte FloatingBusValue = 0xFF;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCompositeTarget"/> class.
    /// </summary>
    private DefaultCompositeTarget()
    {
    }

    /// <inheritdoc />
    public string Name => "DefaultComposite";

    /// <inheritdoc />
    public TargetCaps Capabilities => TargetCaps.SupportsPeek;

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte Read8(Addr physicalAddress, in BusAccess access)
    {
        // Return floating bus value (open bus behavior)
        return FloatingBusValue;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write8(Addr physicalAddress, byte value, in BusAccess access)
    {
        // No-op: writes to unmapped I/O are silently ignored
    }
}
