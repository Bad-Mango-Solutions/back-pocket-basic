// <copyright file="DefaultCompositeTarget.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A default composite bus target that serves as a container for subpages and subregions.
/// </summary>
/// <remarks>
/// <para>
/// This is the standard implementation of <see cref="CompositeTargetBase"/> that provides
/// basic subregion management with no additional custom behavior. It is suitable for:
/// </para>
/// <list type="bullet">
/// <item><description>Testing and bring-up before custom handlers are implemented</description></item>
/// <item><description>Placeholder I/O regions that will be filled in later</description></item>
/// <item><description>Minimal configurations that don't need full I/O support</description></item>
/// <item><description>Simple composite regions without complex dispatch logic</description></item>
/// </list>
/// <para>
/// For more complex composite targets like the Apple II I/O page ($C000-$CFFF) that require
/// custom dispatch logic (e.g., slot ROM selection, expansion ROM banking), derive directly
/// from <see cref="CompositeTargetBase"/> and override the appropriate methods.
/// </para>
/// </remarks>
public sealed class DefaultCompositeTarget : CompositeTargetBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCompositeTarget"/> class.
    /// </summary>
    /// <param name="name">The name of this composite target, typically from the region mapping.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name"/> is null.</exception>
    public DefaultCompositeTarget(string name)
        : base(name)
    {
    }
}