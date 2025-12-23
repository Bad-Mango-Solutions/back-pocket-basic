// <copyright file="BusResultTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="BusResult{T}"/> and <see cref="BusResult"/> structs.
/// </summary>
[TestFixture]
public class BusResultTests
{
    /// <summary>
    /// Verifies that BusResult{T} can be created with value, fault, and cycles.
    /// </summary>
    [Test]
    public void BusResultT_CanBeCreatedWithValueFaultAndCycles()
    {
        var fault = BusFault.Success();
        var result = new BusResult<byte>(0x42, fault, 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(0x42));
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.None));
            Assert.That(result.Cycles, Is.EqualTo(5ul));
        });
    }

    /// <summary>
    /// Verifies Ok returns true when fault kind is None.
    /// </summary>
    [Test]
    public void BusResultT_Ok_TrueWhenFaultKindIsNone()
    {
        var result = BusResult<byte>.Success(0x42);
        Assert.That(result.Ok, Is.True);
    }

    /// <summary>
    /// Verifies Ok returns false when fault kind is not None.
    /// </summary>
    [Test]
    public void BusResultT_Ok_FalseWhenFaultKindIsNotNone()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Unmapped(in access);
        var result = BusResult<byte>.FromFault(fault);

        Assert.That(result.Ok, Is.False);
    }

    /// <summary>
    /// Verifies Failed returns true when fault kind is not None.
    /// </summary>
    [Test]
    public void BusResultT_Failed_TrueWhenFaultKindIsNotNone()
    {
        var access = CreateTestAccess();
        var fault = BusFault.PermissionDenied(in access, 1, RegionTag.Ram);
        var result = BusResult<ushort>.FromFault(fault);

        Assert.That(result.Failed, Is.True);
    }

    /// <summary>
    /// Verifies Failed returns false when fault kind is None.
    /// </summary>
    [Test]
    public void BusResultT_Failed_FalseWhenFaultKindIsNone()
    {
        var result = BusResult<uint>.Success(0x12345678);
        Assert.That(result.Failed, Is.False);
    }

    /// <summary>
    /// Verifies Success factory creates successful result with cycles.
    /// </summary>
    [Test]
    public void BusResultT_Success_CreatesSuccessfulResultWithCycles()
    {
        var result = BusResult<byte>.Success(0xAB, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(0xAB));
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.None));
            Assert.That(result.Cycles, Is.EqualTo(10ul));
        });
    }

    /// <summary>
    /// Verifies Success with context creates successful result with context and cycles.
    /// </summary>
    [Test]
    public void BusResultT_SuccessWithContext_HasCorrectValues()
    {
        var access = CreateTestAccess();
        var result = BusResult<byte>.Success(0xCD, in access, 5, RegionTag.Ram, 15);

        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(0xCD));
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Fault.DeviceId, Is.EqualTo(5));
            Assert.That(result.Fault.RegionTag, Is.EqualTo(RegionTag.Ram));
            Assert.That(result.Cycles, Is.EqualTo(15ul));
        });
    }

    /// <summary>
    /// Verifies FromFault factory creates faulted result with cycles.
    /// </summary>
    [Test]
    public void BusResultT_FromFault_CreatesFaultedResultWithCycles()
    {
        var access = CreateTestAccess(AccessIntent.InstructionFetch);
        var fault = BusFault.NoExecute(in access, 1, RegionTag.Rom);
        var result = BusResult<byte>.FromFault(fault, 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Nx));
            Assert.That(result.Cycles, Is.EqualTo(3ul));
        });
    }

    /// <summary>
    /// Verifies implicit conversion from BusFault works.
    /// </summary>
    [Test]
    public void BusResultT_ImplicitConversionFromFault_Works()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Unmapped(in access);
        BusResult<byte> result = fault;

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Unmapped));
            Assert.That(result.Cycles, Is.EqualTo(0ul));
        });
    }

    /// <summary>
    /// Verifies BusResult{T} works with different value types.
    /// </summary>
    [Test]
    public void BusResultT_WorksWithDifferentTypes()
    {
        var byteResult = BusResult<byte>.Success(0xFF);
        var wordResult = BusResult<ushort>.Success(0xFFFF);
        var dwordResult = BusResult<uint>.Success(0xFFFFFFFFu);

        Assert.Multiple(() =>
        {
            Assert.That(byteResult.Value, Is.EqualTo(0xFF));
            Assert.That(wordResult.Value, Is.EqualTo(0xFFFF));
            Assert.That(dwordResult.Value, Is.EqualTo(0xFFFFFFFFu));
        });
    }

    /// <summary>
    /// Verifies non-generic BusResult can be created with fault and cycles.
    /// </summary>
    [Test]
    public void BusResult_CanBeCreatedWithFaultAndCycles()
    {
        var fault = BusFault.Success();
        var result = new BusResult(fault, 7);

        Assert.Multiple(() =>
        {
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.None));
            Assert.That(result.Cycles, Is.EqualTo(7ul));
        });
    }

    /// <summary>
    /// Verifies non-generic BusResult Ok property.
    /// </summary>
    [Test]
    public void BusResult_Ok_TrueWhenFaultKindIsNone()
    {
        var result = BusResult.Success(5);
        Assert.That(result.Ok, Is.True);
    }

    /// <summary>
    /// Verifies non-generic BusResult Failed property.
    /// </summary>
    [Test]
    public void BusResult_Failed_TrueWhenFaultKindIsNotNone()
    {
        var access = CreateTestAccess();
        var fault = BusFault.PermissionDenied(in access, 1, RegionTag.Ram);
        var result = BusResult.FromFault(fault, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Cycles, Is.EqualTo(2ul));
        });
    }

    /// <summary>
    /// Verifies non-generic BusResult Success factory with cycles.
    /// </summary>
    [Test]
    public void BusResult_Success_CreatesSuccessfulResultWithCycles()
    {
        var result = BusResult.Success(12);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Cycles, Is.EqualTo(12ul));
        });
    }

    /// <summary>
    /// Verifies non-generic BusResult Success with context.
    /// </summary>
    [Test]
    public void BusResult_SuccessWithContext_HasCorrectValues()
    {
        var access = CreateTestAccess();
        var result = BusResult.Success(in access, 5, RegionTag.Ram, 8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Ok, Is.True);
            Assert.That(result.Fault.DeviceId, Is.EqualTo(5));
            Assert.That(result.Fault.RegionTag, Is.EqualTo(RegionTag.Ram));
            Assert.That(result.Cycles, Is.EqualTo(8ul));
        });
    }

    /// <summary>
    /// Verifies non-generic BusResult implicit conversion from fault.
    /// </summary>
    [Test]
    public void BusResult_ImplicitConversionFromFault_Works()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Unmapped(in access);
        BusResult result = fault;

        Assert.Multiple(() =>
        {
            Assert.That(result.Failed, Is.True);
            Assert.That(result.Fault.Kind, Is.EqualTo(FaultKind.Unmapped));
            Assert.That(result.Cycles, Is.EqualTo(0ul));
        });
    }

    private static BusAccess CreateTestAccess(AccessIntent intent = AccessIntent.DataRead)
    {
        return new BusAccess(
            Address: 0x1000u,
            Value: 0,
            WidthBits: 8,
            Mode: CpuMode.Native,
            EmulationFlag: false,
            Intent: intent,
            SourceId: 1,
            Cycle: 100ul,
            Flags: AccessFlags.None);
    }
}