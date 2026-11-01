// <copyright file="ProcessorStatusFlagsTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using Core.Cpu;

/// <summary>
/// Unit tests for <see cref="ProcessorStatusFlagsHelpers.SetZeroAndNegative(ref ProcessorStatusFlags, byte)"/>
/// and related overloads, validating the branchless bitmask optimization.
/// </summary>
[TestFixture]
public class ProcessorStatusFlagsTests
{
    /// <summary>
    /// Verifies that SetZeroAndNegative sets Z flag and clears N flag for byte value 0.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_Zero_SetsZeroClearsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((byte)0);

        Assert.That(p.IsZeroSet(), Is.True, "Z flag should be set for value 0");
        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be clear for value 0");
    }

    /// <summary>
    /// Verifies that SetZeroAndNegative clears Z flag and sets N flag for byte value 0x80.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_HighBit_ClearsZeroSetsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((byte)0x80);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for value 0x80");
        Assert.That(p.IsNegativeSet(), Is.True, "N flag should be set for value 0x80");
    }

    /// <summary>
    /// Verifies that SetZeroAndNegative clears both Z and N flags for a positive non-zero byte.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_PositiveNonZero_ClearsBothFlags()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((byte)0x42);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for value 0x42");
        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be clear for value 0x42");
    }

    /// <summary>
    /// Verifies that SetZeroAndNegative sets N flag for byte value 0xFF.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_MaxValue_SetsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((byte)0xFF);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for value 0xFF");
        Assert.That(p.IsNegativeSet(), Is.True, "N flag should be set for value 0xFF");
    }

    /// <summary>
    /// Verifies that SetZeroAndNegative clears both flags for byte value 1.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_One_ClearsBothFlags()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((byte)1);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for value 1");
        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be clear for value 1");
    }

    /// <summary>
    /// Verifies that SetZeroAndNegative preserves other flags (C, I, D, V).
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_PreservesOtherFlags()
    {
        var p = ProcessorStatusFlags.C | ProcessorStatusFlags.I | ProcessorStatusFlags.D | ProcessorStatusFlags.V;

        p.SetZeroAndNegative((byte)0);

        Assert.That(p.IsCarrySet(), Is.True, "C flag should be preserved");
        Assert.That(p.IsInterruptDisabled(), Is.True, "I flag should be preserved");
        Assert.That(p.IsDecimalModeEnabled(), Is.True, "D flag should be preserved");
        Assert.That(p.IsOverflowSet(), Is.True, "V flag should be preserved");
        Assert.That(p.IsZeroSet(), Is.True, "Z flag should be set for value 0");
    }

    /// <summary>
    /// Verifies that SetZeroAndNegative clears previously set Z flag.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_ClearsPreviousZFlag()
    {
        var p = ProcessorStatusFlags.Z;

        p.SetZeroAndNegative((byte)0x42);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be cleared for non-zero value");
    }

    /// <summary>
    /// Verifies that SetZeroAndNegative clears previously set N flag.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_ClearsPreviousNFlag()
    {
        var p = ProcessorStatusFlags.N;

        p.SetZeroAndNegative((byte)0x42);

        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be cleared for value with bit 7 clear");
    }

    /// <summary>
    /// Verifies SetZeroAndNegative for 16-bit zero value.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Word_Zero_SetsZeroClearsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((Word)0);

        Assert.That(p.IsZeroSet(), Is.True, "Z flag should be set for Word value 0");
        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be clear for Word value 0");
    }

    /// <summary>
    /// Verifies SetZeroAndNegative for 16-bit value with bit 15 set.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Word_HighBit_ClearsZeroSetsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((Word)0x8000);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for Word value 0x8000");
        Assert.That(p.IsNegativeSet(), Is.True, "N flag should be set for Word value 0x8000");
    }

    /// <summary>
    /// Verifies SetZeroAndNegative for 16-bit max value.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Word_MaxValue_SetsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((Word)0xFFFF);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for Word value 0xFFFF");
        Assert.That(p.IsNegativeSet(), Is.True, "N flag should be set for Word value 0xFFFF");
    }

    /// <summary>
    /// Verifies SetZeroAndNegative for 32-bit zero value.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_UInt_Zero_SetsZeroClearsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative(0u);

        Assert.That(p.IsZeroSet(), Is.True, "Z flag should be set for uint value 0");
        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be clear for uint value 0");
    }

    /// <summary>
    /// Verifies SetZeroAndNegative for 32-bit value with bit 31 set.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_UInt_HighBit_ClearsZeroSetsNegative()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative(0x80000000u);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for uint value 0x80000000");
        Assert.That(p.IsNegativeSet(), Is.True, "N flag should be set for uint value 0x80000000");
    }

    /// <summary>
    /// Verifies SetZeroAndNegative for size-aware 8-bit dispatch.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_SizeAware_8Bit_CorrectDispatch()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((DWord)0x80, 8);

        Assert.That(p.IsNegativeSet(), Is.True, "N flag should be set via 8-bit dispatch for 0x80");
        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear via 8-bit dispatch for 0x80");
    }

    /// <summary>
    /// Verifies SetZeroAndNegative for size-aware 16-bit dispatch.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_SizeAware_16Bit_CorrectDispatch()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((DWord)0x0080, 16);

        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be clear via 16-bit dispatch for 0x0080");
        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear via 16-bit dispatch for 0x0080");
    }

    /// <summary>
    /// Verifies byte value 0x7F (largest positive) clears N and Z.
    /// </summary>
    [Test]
    public void SetZeroAndNegative_Byte_LargestPositive_ClearsBothFlags()
    {
        var p = (ProcessorStatusFlags)0;

        p.SetZeroAndNegative((byte)0x7F);

        Assert.That(p.IsZeroSet(), Is.False, "Z flag should be clear for value 0x7F");
        Assert.That(p.IsNegativeSet(), Is.False, "N flag should be clear for value 0x7F");
    }

    /// <summary>
    /// Verifies that every single-flag Set* helper writes the result back through the ref receiver.
    /// </summary>
    /// <param name="mask">The flag bit mask under test.</param>
    [TestCase((byte)0x01)]
    [TestCase((byte)0x02)]
    [TestCase((byte)0x04)]
    [TestCase((byte)0x08)]
    [TestCase((byte)0x10)]
    [TestCase((byte)0x20)]
    [TestCase((byte)0x40)]
    [TestCase((byte)0x80)]
    public void SetFlagHelpers_WriteThroughRef(byte mask)
    {
        static void Apply(ref ProcessorStatusFlags p, byte m, bool v)
        {
            switch (m)
            {
                case 0x01: p.SetCarry(v); break;
                case 0x02: p.SetZero(v); break;
                case 0x04: p.SetInterruptDisable(v); break;
                case 0x08: p.SetDecimalMode(v); break;
                case 0x10: p.SetIndexSize8Bit(v); break;
                case 0x20: p.SetMemorySize8Bit(v); break;
                case 0x40: p.SetOverflow(v); break;
                case 0x80: p.SetNegative(v); break;
            }
        }

        var p = (ProcessorStatusFlags)0;
        Apply(ref p, mask, true);
        Assert.That((byte)p, Is.EqualTo(mask), "Setting the flag should modify the receiver");

        p = (ProcessorStatusFlags)0xFF;
        Apply(ref p, mask, false);
        Assert.That((byte)p, Is.EqualTo((byte)(0xFF & ~mask)), "Clearing the flag should modify the receiver");
    }
}