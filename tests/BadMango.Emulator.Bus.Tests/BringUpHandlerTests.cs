// <copyright file="BringUpHandlerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

using Interfaces;

/// <summary>
/// Unit tests for the <see cref="BringUpHandlerBase"/> and related classes.
/// </summary>
[TestFixture]
public class BringUpHandlerTests
{
    /// <summary>
    /// Verifies that validation fails for RAM below minimum.
    /// </summary>
    [Test]
    public void Validate_FailsForRamBelowMinimum()
    {
        var handler = new TestBringUpHandler();
        var bundle = new ProvisioningBundle(1024); // Below minimum

        var result = handler.Validate(bundle);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.GreaterThan(0));
            Assert.That(result.Errors.Any(e => e.Contains("below minimum")), Is.True);
        });
    }

    /// <summary>
    /// Verifies that validation fails for RAM above maximum.
    /// </summary>
    [Test]
    public void Validate_FailsForRamAboveMaximum()
    {
        var handler = new TestBringUpHandler();
        var bundle = new ProvisioningBundle(100_000_000); // Above maximum

        var result = handler.Validate(bundle);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.GreaterThan(0));
            Assert.That(result.Errors.Any(e => e.Contains("exceeds maximum")), Is.True);
        });
    }

    /// <summary>
    /// Verifies that validation warns for missing boot ROM.
    /// </summary>
    [Test]
    public void Validate_WarnsForMissingBootRom()
    {
        var handler = new TestBringUpHandler();
        var bundle = new ProvisioningBundle(65536); // Valid RAM size, no ROM

        var result = handler.Validate(bundle);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Warnings, Has.Count.GreaterThan(0));
            Assert.That(result.Warnings.Any(w => w.Contains("boot ROM")), Is.True);
        });
    }

    /// <summary>
    /// Verifies that bring-up succeeds with valid configuration.
    /// </summary>
    [Test]
    public void BringUp_SucceedsWithValidConfiguration()
    {
        var handler = new TestBringUpHandler();
        var romData = new byte[8192]; // Match expected boot ROM size
        var bundle = ProvisioningBundle.CreateBuilder()
            .WithRamSize(65536)
            .WithRomImage("boot", romData)
            .Build();

        var result = handler.BringUp(bundle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.RegionManager, Is.Not.Null);
            Assert.That(result.PhysicalMemoryPools, Is.Not.Null);
            Assert.That(result.PhysicalMemoryPools!.ContainsKey("main_ram"), Is.True);
            Assert.That(result.PhysicalMemoryPools.ContainsKey("boot_rom"), Is.True);
            Assert.That(result.DeviceRegistry, Is.Not.Null);
            Assert.That(result.Constants, Is.Not.Null);
        });
    }

    /// <summary>
    /// Verifies that bring-up allocates correct RAM size.
    /// </summary>
    [Test]
    public void BringUp_AllocatesCorrectRamSize()
    {
        var handler = new TestBringUpHandler();
        var bundle = new ProvisioningBundle(131072);

        var result = handler.BringUp(bundle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PhysicalMemoryPools!["main_ram"].Size, Is.EqualTo(131072U));
        });
    }

    /// <summary>
    /// Verifies that bring-up uses default RAM size when not specified.
    /// </summary>
    [Test]
    public void BringUp_UsesDefaultRamSizeWhenNotSpecified()
    {
        var handler = new TestBringUpHandler();
        var bundle = new ProvisioningBundle(0);

        var result = handler.BringUp(bundle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PhysicalMemoryPools!["main_ram"].Size, Is.EqualTo(handler.Constants.DefaultRamSize));
        });
    }

    /// <summary>
    /// Verifies that BringUpValidationResult.Valid returns valid result.
    /// </summary>
    [Test]
    public void BringUpValidationResult_Valid_ReturnsValidResult()
    {
        var result = BringUpValidationResult.Valid();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        });
    }

    /// <summary>
    /// Verifies that BringUpValidationResult.Invalid returns invalid result.
    /// </summary>
    [Test]
    public void BringUpValidationResult_Invalid_ReturnsInvalidResult()
    {
        var result = BringUpValidationResult.Invalid("Test error");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors.First(), Is.EqualTo("Test error"));
        });
    }

    /// <summary>
    /// Verifies that BringUpResult.Failed creates failed result.
    /// </summary>
    [Test]
    public void BringUpResult_Failed_CreatesFailedResult()
    {
        var result = BringUpResult.Failed("Test failure");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Test failure"));
            Assert.That(result.RegionManager, Is.Null);
            Assert.That(result.PhysicalMemoryPools, Is.Null);
            Assert.That(result.DeviceRegistry, Is.Null);
            Assert.That(result.Constants, Is.Null);
        });
    }

    /// <summary>
    /// Test implementation of BringUpHandlerBase for testing.
    /// </summary>
    private sealed class TestBringUpHandler : BringUpHandlerBase
    {
        public TestBringUpHandler()
            : base(new TestMachineConstants())
        {
        }

        protected override void CreateAndMapRegions(
            IProvisioningBundle bundle,
            IRegionManager regionManager,
            IReadOnlyDictionary<string, IPhysicalMemory> pools)
        {
            // Create and map RAM region
            if (pools.TryGetValue("main_ram", out var mainRam))
            {
                var ramRegion = CreateRamRegion("ram", "Main RAM", Constants.RamBase, mainRam);
                regionManager.MapRegionAtPreferred(ramRegion);
            }

            // Create and map ROM region
            if (pools.TryGetValue("boot_rom", out var bootRom))
            {
                var romRegion = CreateRomRegion("rom", "Boot ROM", Constants.BootRomBase, bootRom);
                regionManager.MapRegionAtPreferred(romRegion);
            }
        }
    }

    /// <summary>
    /// Test implementation of IMachineConstants for testing.
    /// </summary>
    private sealed class TestMachineConstants : IMachineConstants
    {
        public uint MinRamSize => 4096;

        public uint MaxRamSize => 8_388_608;

        public uint DefaultRamSize => 65536;

        public uint PageSize => 4096;

        public uint BootRomBase => 0x00000000;

        public uint BootRomSize => 8192;

        public uint RamBase => 0x00040000;

        public string MachineName => "Test Machine";

        public string MachineTypeId => "test";
    }
}