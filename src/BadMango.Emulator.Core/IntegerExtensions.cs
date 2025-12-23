// <copyright file="IntegerExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core;

using System.Runtime.CompilerServices;

/// <summary>
/// Provides extension methods for extracting high and low bytes from a 16-bit unsigned integer.
/// </summary>
public static class IntegerExtensions
{
    /// <param name="w">The 16-bit unsigned integer.</param>
    extension(Word w)
    {
        /// <summary>
        /// Gets the high byte of the specified 16-bit unsigned integer.
        /// </summary>
        /// <returns>The high byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte HighByte() => (byte)(w >> 8);

        /// <summary>
        /// Gets the low byte of the specified 16-bit unsigned integer.
        /// </summary>
        /// <returns>The low byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte LowByte() => (byte)(w & 0xFF);
    }

    extension(DWord dw)
    {
        /// <summary>
        /// Gets the highest byte of the specified 32-bit unsigned integer.
        /// </summary>
        /// <returns>The highest byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte HighestByte() => (byte)(dw >> 24);

        /// <summary>
        /// Gets the high byte of the specified 32-bit unsigned integer.
        /// </summary>
        /// <returns>The high byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte HighByte() => (byte)((dw >> 16) & 0xFF);

        /// <summary>
        /// Gets the low byte of the specified 32-bit unsigned integer.
        /// </summary>
        /// <returns>The low byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte LowByte() => (byte)((dw >> 8) & 0xFF);

        /// <summary>
        /// Gets the lowest byte of the specified 32-bit unsigned integer.
        /// </summary>
        /// <returns>The lowest byte.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte LowestByte() => (byte)(dw & 0xFF);

        /// <summary>
        /// Retrieves the high-order word (the upper 16 bits) of the 32-bit value.
        /// </summary>
        /// <returns>A <see cref="Word"/> representing the high-order 16 bits of the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Word HighWord() => (Word)((dw >> 16) & 0xFFFF);

        /// <summary>
        /// Retrieves the low-order word (the lower 16 bits) of the 32-bit value.
        /// </summary>
        /// <returns>A <see cref="Word"/> representing the low-order 16 bits of the value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Word LowWord() => (Word)(dw & 0xFFFF);
    }
}