// <copyright file="BusAccessTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="BusAccess"/> struct.
/// </summary>
[TestFixture]
public class BusAccessTests
{
    /// <summary>
    /// Verifies that BusAccess can be created with all properties.
    /// </summary>
    [Test]
    public void BusAccess_CanBeCreatedWithProperties()
    {
        var access = new BusAccess(
            Address: 0x1000u,
            Value: 0x42u,
            WidthBits: 8,
            Mode: CpuMode.Native,
            EmulationFlag: false,
            Intent: AccessIntent.DataRead,
            SourceId: 1,
            Cycle: 100ul,
            Flags: AccessFlags.None);

        Assert.Multiple(() =>
        {
            Assert.That(access.Address, Is.EqualTo(0x1000u));
            Assert.That(access.Value, Is.EqualTo(0x42u));
            Assert.That(access.WidthBits, Is.EqualTo(8));
            Assert.That(access.Mode, Is.EqualTo(CpuMode.Native));
            Assert.That(access.EmulationFlag, Is.False);
            Assert.That(access.Intent, Is.EqualTo(AccessIntent.DataRead));
            Assert.That(access.SourceId, Is.EqualTo(1));
            Assert.That(access.Cycle, Is.EqualTo(100ul));
            Assert.That(access.Flags, Is.EqualTo(AccessFlags.None));
        });
    }

    /// <summary>
    /// Verifies IsSideEffectFree returns true when NoSideEffects flag is set.
    /// </summary>
    [Test]
    public void BusAccess_IsSideEffectFree_TrueWhenFlagSet()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DebugRead, 0, 0, AccessFlags.NoSideEffects);
        Assert.That(access.IsSideEffectFree, Is.True);
    }

    /// <summary>
    /// Verifies IsSideEffectFree returns false when NoSideEffects flag is not set.
    /// </summary>
    [Test]
    public void BusAccess_IsSideEffectFree_FalseWhenFlagNotSet()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DataRead, 0, 0, AccessFlags.None);
        Assert.That(access.IsSideEffectFree, Is.False);
    }

    /// <summary>
    /// Verifies IsAtomicRequested returns true when Atomic flag is set.
    /// </summary>
    [Test]
    public void BusAccess_IsAtomicRequested_TrueWhenFlagSet()
    {
        var access = new BusAccess(0, 0, 16, CpuMode.Native, false, AccessIntent.DataRead, 0, 0, AccessFlags.Atomic);
        Assert.That(access.IsAtomicRequested, Is.True);
    }

    /// <summary>
    /// Verifies IsDecomposeForced returns true when Decompose flag is set.
    /// </summary>
    [Test]
    public void BusAccess_IsDecomposeForced_TrueWhenFlagSet()
    {
        var access = new BusAccess(0, 0, 16, CpuMode.Compat, true, AccessIntent.DataRead, 0, 0, AccessFlags.Decompose);
        Assert.That(access.IsDecomposeForced, Is.True);
    }

    /// <summary>
    /// Verifies IsDebugAccess returns true for DebugRead intent.
    /// </summary>
    [Test]
    public void BusAccess_IsDebugAccess_TrueForDebugRead()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DebugRead, 0, 0, AccessFlags.None);
        Assert.That(access.IsDebugAccess, Is.True);
    }

    /// <summary>
    /// Verifies IsDebugAccess returns true for DebugWrite intent.
    /// </summary>
    [Test]
    public void BusAccess_IsDebugAccess_TrueForDebugWrite()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DebugWrite, 0, 0, AccessFlags.None);
        Assert.That(access.IsDebugAccess, Is.True);
    }

    /// <summary>
    /// Verifies IsDebugAccess returns false for non-debug intents.
    /// </summary>
    [Test]
    public void BusAccess_IsDebugAccess_FalseForDataRead()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DataRead, 0, 0, AccessFlags.None);
        Assert.That(access.IsDebugAccess, Is.False);
    }

    /// <summary>
    /// Verifies IsDmaAccess returns true for DmaRead intent.
    /// </summary>
    [Test]
    public void BusAccess_IsDmaAccess_TrueForDmaRead()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DmaRead, 0, 0, AccessFlags.None);
        Assert.That(access.IsDmaAccess, Is.True);
    }

    /// <summary>
    /// Verifies IsDmaAccess returns true for DmaWrite intent.
    /// </summary>
    [Test]
    public void BusAccess_IsDmaAccess_TrueForDmaWrite()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DmaWrite, 0, 0, AccessFlags.None);
        Assert.That(access.IsDmaAccess, Is.True);
    }

    /// <summary>
    /// Verifies IsRead returns true for read intents.
    /// </summary>
    /// <param name="intent">The access intent to test.</param>
    [TestCase(AccessIntent.DataRead)]
    [TestCase(AccessIntent.InstructionFetch)]
    [TestCase(AccessIntent.DebugRead)]
    [TestCase(AccessIntent.DmaRead)]
    public void BusAccess_IsRead_TrueForReadIntents(AccessIntent intent)
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, intent, 0, 0, AccessFlags.None);
        Assert.That(access.IsRead, Is.True);
    }

    /// <summary>
    /// Verifies IsRead returns false for write intents.
    /// </summary>
    /// <param name="intent">The access intent to test.</param>
    [TestCase(AccessIntent.DataWrite)]
    [TestCase(AccessIntent.DebugWrite)]
    [TestCase(AccessIntent.DmaWrite)]
    public void BusAccess_IsRead_FalseForWriteIntents(AccessIntent intent)
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, intent, 0, 0, AccessFlags.None);
        Assert.That(access.IsRead, Is.False);
    }

    /// <summary>
    /// Verifies IsWrite returns true for write intents.
    /// </summary>
    /// <param name="intent">The access intent to test.</param>
    [TestCase(AccessIntent.DataWrite)]
    [TestCase(AccessIntent.DebugWrite)]
    [TestCase(AccessIntent.DmaWrite)]
    public void BusAccess_IsWrite_TrueForWriteIntents(AccessIntent intent)
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, intent, 0, 0, AccessFlags.None);
        Assert.That(access.IsWrite, Is.True);
    }

    /// <summary>
    /// Verifies IsWrite returns false for read intents.
    /// </summary>
    /// <param name="intent">The access intent to test.</param>
    [TestCase(AccessIntent.DataRead)]
    [TestCase(AccessIntent.InstructionFetch)]
    [TestCase(AccessIntent.DebugRead)]
    [TestCase(AccessIntent.DmaRead)]
    public void BusAccess_IsWrite_FalseForReadIntents(AccessIntent intent)
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, intent, 0, 0, AccessFlags.None);
        Assert.That(access.IsWrite, Is.False);
    }

    /// <summary>
    /// Verifies WithAddressOffset creates a new instance with updated address.
    /// </summary>
    [Test]
    public void BusAccess_WithAddressOffset_CreatesNewInstanceWithUpdatedAddress()
    {
        var original = new BusAccess(0x1000u, 0x42u, 8, CpuMode.Native, false, AccessIntent.DataRead, 1, 100, AccessFlags.None);
        var updated = original.WithAddressOffset(4);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Address, Is.EqualTo(0x1004u));
            Assert.That(updated.Value, Is.EqualTo(original.Value));
            Assert.That(updated.WidthBits, Is.EqualTo(original.WidthBits));
            Assert.That(updated.Mode, Is.EqualTo(original.Mode));
            Assert.That(updated.Intent, Is.EqualTo(original.Intent));
            Assert.That(updated.SourceId, Is.EqualTo(original.SourceId));
            Assert.That(updated.Cycle, Is.EqualTo(original.Cycle));
            Assert.That(updated.Flags, Is.EqualTo(original.Flags));
        });
    }

    /// <summary>
    /// Verifies that record equality works correctly.
    /// </summary>
    [Test]
    public void BusAccess_RecordEquality_Works()
    {
        var access1 = new BusAccess(0x1000u, 0x42u, 8, CpuMode.Native, false, AccessIntent.DataRead, 1, 100, AccessFlags.None);
        var access2 = new BusAccess(0x1000u, 0x42u, 8, CpuMode.Native, false, AccessIntent.DataRead, 1, 100, AccessFlags.None);
        var access3 = new BusAccess(0x2000u, 0x42u, 8, CpuMode.Native, false, AccessIntent.DataRead, 1, 100, AccessFlags.None);

        Assert.Multiple(() =>
        {
            Assert.That(access1, Is.EqualTo(access2));
            Assert.That(access1, Is.Not.EqualTo(access3));
        });
    }

    /// <summary>
    /// Verifies that PrivilegeLevel defaults to Ring0.
    /// </summary>
    [Test]
    public void BusAccess_PrivilegeLevel_DefaultsToRing0()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Compat, true, AccessIntent.DataRead, 0, 0, AccessFlags.None);

        Assert.That(access.PrivilegeLevel, Is.EqualTo(PrivilegeLevel.Ring0));
    }

    /// <summary>
    /// Verifies that PrivilegeLevel can be explicitly set.
    /// </summary>
    [Test]
    public void BusAccess_PrivilegeLevel_CanBeSet()
    {
        var access = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DataRead, 0, 0, AccessFlags.None, PrivilegeLevel.Ring3);

        Assert.That(access.PrivilegeLevel, Is.EqualTo(PrivilegeLevel.Ring3));
    }

    /// <summary>
    /// Verifies that different privilege levels result in different access instances.
    /// </summary>
    [Test]
    public void BusAccess_DifferentPrivilegeLevels_AreNotEqual()
    {
        var access1 = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DataRead, 0, 0, AccessFlags.None, PrivilegeLevel.Ring0);
        var access2 = new BusAccess(0, 0, 8, CpuMode.Native, false, AccessIntent.DataRead, 0, 0, AccessFlags.None, PrivilegeLevel.Ring3);

        Assert.That(access1, Is.Not.EqualTo(access2));
    }
}