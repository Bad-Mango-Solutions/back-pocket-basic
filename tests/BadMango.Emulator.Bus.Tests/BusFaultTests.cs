// <copyright file="BusFaultTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="BusFault"/> struct.
/// </summary>
[TestFixture]
public class BusFaultTests
{
    /// <summary>
    /// Verifies that BusFault can be created with all properties.
    /// </summary>
    [Test]
    public void BusFault_CanBeCreatedWithProperties()
    {
        var fault = new BusFault(
            Kind: FaultKind.Permission,
            Address: 0x1000u,
            WidthBits: 8,
            Intent: AccessIntent.DataWrite,
            Mode: CpuMode.Native,
            SourceId: 1,
            DeviceId: 5,
            RegionTag: RegionTag.Io,
            Cycle: 12345ul);

        Assert.Multiple(() =>
        {
            Assert.That(fault.Kind, Is.EqualTo(FaultKind.Permission));
            Assert.That(fault.Address, Is.EqualTo(0x1000u));
            Assert.That(fault.WidthBits, Is.EqualTo(8));
            Assert.That(fault.Intent, Is.EqualTo(AccessIntent.DataWrite));
            Assert.That(fault.Mode, Is.EqualTo(CpuMode.Native));
            Assert.That(fault.SourceId, Is.EqualTo(1));
            Assert.That(fault.DeviceId, Is.EqualTo(5));
            Assert.That(fault.RegionTag, Is.EqualTo(RegionTag.Io));
            Assert.That(fault.Cycle, Is.EqualTo(12345ul));
        });
    }

    /// <summary>
    /// Verifies IsSuccess returns true when Kind is None.
    /// </summary>
    [Test]
    public void BusFault_IsSuccess_TrueWhenKindIsNone()
    {
        var fault = BusFault.Success();
        Assert.That(fault.IsSuccess, Is.True);
    }

    /// <summary>
    /// Verifies IsSuccess returns false when Kind is not None.
    /// </summary>
    [Test]
    public void BusFault_IsSuccess_FalseWhenKindIsNotNone()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Unmapped(in access);
        Assert.That(fault.IsSuccess, Is.False);
    }

    /// <summary>
    /// Verifies IsFault returns true when Kind is not None.
    /// </summary>
    [Test]
    public void BusFault_IsFault_TrueWhenKindIsNotNone()
    {
        var access = CreateTestAccess();
        var fault = BusFault.PermissionDenied(in access, 1, RegionTag.Ram);
        Assert.That(fault.IsFault, Is.True);
    }

    /// <summary>
    /// Verifies IsFault returns false when Kind is None.
    /// </summary>
    [Test]
    public void BusFault_IsFault_FalseWhenKindIsNone()
    {
        var fault = BusFault.Success();
        Assert.That(fault.IsFault, Is.False);
    }

    /// <summary>
    /// Verifies IsNxFault returns true for Nx fault kind.
    /// </summary>
    [Test]
    public void BusFault_IsNxFault_TrueForNxKind()
    {
        var access = CreateTestAccess(AccessIntent.InstructionFetch);
        var fault = BusFault.NoExecute(in access, 1, RegionTag.Ram);
        Assert.That(fault.IsNxFault, Is.True);
    }

    /// <summary>
    /// Verifies IsPermissionFault returns true for Permission fault kind.
    /// </summary>
    [Test]
    public void BusFault_IsPermissionFault_TrueForPermissionKind()
    {
        var access = CreateTestAccess();
        var fault = BusFault.PermissionDenied(in access, 1, RegionTag.Ram);
        Assert.That(fault.IsPermissionFault, Is.True);
    }

    /// <summary>
    /// Verifies IsUnmappedFault returns true for Unmapped fault kind.
    /// </summary>
    [Test]
    public void BusFault_IsUnmappedFault_TrueForUnmappedKind()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Unmapped(in access);
        Assert.That(fault.IsUnmappedFault, Is.True);
    }

    /// <summary>
    /// Verifies IsDeviceFault returns true for DeviceFault fault kind.
    /// </summary>
    [Test]
    public void BusFault_IsDeviceFault_TrueForDeviceFaultKind()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Device(in access, 1, RegionTag.Io);
        Assert.That(fault.IsDeviceFault, Is.True);
    }

    /// <summary>
    /// Verifies Success factory creates a success fault with context.
    /// </summary>
    [Test]
    public void BusFault_SuccessWithContext_HasCorrectValues()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Success(in access, 5, RegionTag.Ram);

        Assert.Multiple(() =>
        {
            Assert.That(fault.Kind, Is.EqualTo(FaultKind.None));
            Assert.That(fault.Address, Is.EqualTo(access.Address));
            Assert.That(fault.DeviceId, Is.EqualTo(5));
            Assert.That(fault.RegionTag, Is.EqualTo(RegionTag.Ram));
        });
    }

    /// <summary>
    /// Verifies Unmapped factory sets DeviceId to -1.
    /// </summary>
    [Test]
    public void BusFault_Unmapped_SetsDeviceIdToMinusOne()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Unmapped(in access);

        Assert.Multiple(() =>
        {
            Assert.That(fault.Kind, Is.EqualTo(FaultKind.Unmapped));
            Assert.That(fault.DeviceId, Is.EqualTo(-1));
            Assert.That(fault.RegionTag, Is.EqualTo(RegionTag.Unmapped));
        });
    }

    /// <summary>
    /// Verifies Misaligned factory creates correct fault.
    /// </summary>
    [Test]
    public void BusFault_Misaligned_CreatesCorrectFault()
    {
        var access = CreateTestAccess();
        var fault = BusFault.Misaligned(in access, 1, RegionTag.Ram);

        Assert.Multiple(() =>
        {
            Assert.That(fault.Kind, Is.EqualTo(FaultKind.Misaligned));
            Assert.That(fault.DeviceId, Is.EqualTo(1));
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