// <copyright file="Cycle.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.CompilerServices;

/// <summary>
/// Represents a count of machine cycles for type-safe cycle arithmetic.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Cycle"/> struct provides a type-safe wrapper around cycle counts,
/// preventing accidental mixing of cycles with other numeric values. This is critical
/// for the scheduler and timing subsystem where cycle accuracy is paramount.
/// </para>
/// <para>
/// All arithmetic operations are designed for high performance with aggressive inlining
/// to avoid any overhead in hot paths.
/// </para>
/// </remarks>
/// <param name="Value">The raw cycle count as an unsigned 64-bit integer.</param>
public readonly record struct Cycle(ulong Value) : IComparable<Cycle>
{
    /// <summary>
    /// Gets a <see cref="Cycle"/> instance representing zero cycles.
    /// </summary>
    public static Cycle Zero => new(0);

    /// <summary>
    /// Gets a <see cref="Cycle"/> instance representing one cycle.
    /// </summary>
    public static Cycle One => new(1);

    /// <summary>
    /// Implicitly converts a <see cref="ulong"/> to a <see cref="Cycle"/> instance.
    /// </summary>
    /// <param name="value">The raw cycle count.</param>
    /// <returns>A <see cref="Cycle"/> instance with the specified value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Cycle(ulong value) => new(value);

    /// <summary>
    /// Implicitly converts a <see cref="Cycle"/> instance to a <see cref="ulong"/>.
    /// </summary>
    /// <param name="cycle">The cycle instance.</param>
    /// <returns>The raw cycle count as a <see cref="ulong"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(Cycle cycle) => cycle.Value;

    /// <summary>
    /// Adds two cycle counts.
    /// </summary>
    /// <param name="left">The first cycle count.</param>
    /// <param name="right">The second cycle count.</param>
    /// <returns>The sum of the two cycle counts.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cycle operator +(Cycle left, Cycle right) => new(left.Value + right.Value);

    /// <summary>
    /// Subtracts one cycle count from another.
    /// </summary>
    /// <param name="left">The cycle count to subtract from.</param>
    /// <param name="right">The cycle count to subtract.</param>
    /// <returns>The difference between the two cycle counts.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cycle operator -(Cycle left, Cycle right) => new(left.Value - right.Value);

    /// <summary>
    /// Determines whether one cycle count is less than another.
    /// </summary>
    /// <param name="left">The first cycle count.</param>
    /// <param name="right">The second cycle count.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Cycle left, Cycle right) => left.Value < right.Value;

    /// <summary>
    /// Determines whether one cycle count is greater than another.
    /// </summary>
    /// <param name="left">The first cycle count.</param>
    /// <param name="right">The second cycle count.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Cycle left, Cycle right) => left.Value > right.Value;

    /// <summary>
    /// Determines whether one cycle count is less than or equal to another.
    /// </summary>
    /// <param name="left">The first cycle count.</param>
    /// <param name="right">The second cycle count.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Cycle left, Cycle right) => left.Value <= right.Value;

    /// <summary>
    /// Determines whether one cycle count is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first cycle count.</param>
    /// <param name="right">The second cycle count.</param>
    /// <returns><see langword="true"/> if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Cycle left, Cycle right) => left.Value >= right.Value;

    /// <summary>
    /// Increments a cycle count by one.
    /// </summary>
    /// <param name="cycle">The cycle count to increment.</param>
    /// <returns>A new <see cref="Cycle"/> instance with the value incremented by one.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Cycle operator ++(Cycle cycle) => new(cycle.Value + 1);

    /// <summary>
    /// Compares this instance to another <see cref="Cycle"/> instance.
    /// </summary>
    /// <param name="other">The other instance to compare to.</param>
    /// <returns>A value indicating the relative order of the instances.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Cycle other) => Value.CompareTo(other.Value);

    /// <summary>
    /// Returns a string representation of the cycle count.
    /// </summary>
    /// <returns>A string containing the cycle count.</returns>
    public override string ToString() => $"{Value} cycles";
}