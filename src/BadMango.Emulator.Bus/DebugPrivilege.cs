// <copyright file="DebugPrivilege.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using Interfaces;

/// <summary>
/// A marker object that gates access to debug-only memory operations.
/// </summary>
/// <remarks>
/// <para>
/// This class exists to prevent accidental use of direct physical memory writes
/// in normal emulation code. Only code with access to a <see cref="DebugPrivilege"/>
/// instance can invoke debug write methods on <see cref="IPhysicalMemory"/>.
/// </para>
/// <para>
/// The internal constructor restricts creation of instances to this assembly
/// and any assemblies granted internal access via <c>InternalsVisibleTo</c>
/// (for example, dedicated test assemblies).
/// </para>
/// </remarks>
public sealed class DebugPrivilege
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DebugPrivilege"/> class.
    /// </summary>
    internal DebugPrivilege()
    {
    }
}