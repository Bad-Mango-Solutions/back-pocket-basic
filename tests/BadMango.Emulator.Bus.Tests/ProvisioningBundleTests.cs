// <copyright file="ProvisioningBundleTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="ProvisioningBundle"/> and related classes.
/// </summary>
[TestFixture]
public class ProvisioningBundleTests
{
    /// <summary>
    /// Verifies that a provisioning bundle can be created with basic settings.
    /// </summary>
    [Test]
    public void Constructor_CreatesValidBundle()
    {
        var bundle = new ProvisioningBundle(65536);

        Assert.Multiple(() =>
        {
            Assert.That(bundle.RequestedRamSize, Is.EqualTo(65536U));
            Assert.That(bundle.RomImages, Is.Empty);
            Assert.That(bundle.Devices, Is.Empty);
            Assert.That(bundle.LayoutOverrides, Is.Null);
            Assert.That(bundle.EnableDebugFeatures, Is.False);
        });
    }

    /// <summary>
    /// Verifies that the builder creates a valid bundle.
    /// </summary>
    [Test]
    public void Builder_CreatesValidBundle()
    {
        var romData = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        var bundle = ProvisioningBundle.CreateBuilder()
            .WithRamSize(131072)
            .WithRomImage("boot", romData)
            .WithDevice(new("keyboard", "kbd0"))
            .WithLayoutOverride("ram", 0x40000)
            .WithDebugFeatures()
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(bundle.RequestedRamSize, Is.EqualTo(131072U));
            Assert.That(bundle.RomImages, Has.Count.EqualTo(1));
            Assert.That(bundle.RomImages.ContainsKey("boot"), Is.True);
            Assert.That(bundle.Devices, Has.Count.EqualTo(1));
            Assert.That(bundle.LayoutOverrides, Is.Not.Null);
            Assert.That(bundle.LayoutOverrides!.ContainsKey("ram"), Is.True);
            Assert.That(bundle.EnableDebugFeatures, Is.True);
        });
    }

    /// <summary>
    /// Verifies that builder throws for null ROM id.
    /// </summary>
    [Test]
    public void Builder_WithRomImage_ThrowsForNullId()
    {
        var builder = ProvisioningBundle.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithRomImage(null!, new byte[1]));
    }

    /// <summary>
    /// Verifies that builder throws for empty ROM id.
    /// </summary>
    [Test]
    public void Builder_WithLayoutOverride_ThrowsForNullId()
    {
        var builder = ProvisioningBundle.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithLayoutOverride(null!, 0x1000));
    }

    /// <summary>
    /// Verifies that DeviceConfiguration stores properties correctly.
    /// </summary>
    [Test]
    public void DeviceConfiguration_StoresProperties()
    {
        var props = new Dictionary<string, object> { ["setting"] = 42 };
        var config = new DeviceConfiguration("disk", "disk0", 0xC600, props);

        Assert.Multiple(() =>
        {
            Assert.That(config.DeviceType, Is.EqualTo("disk"));
            Assert.That(config.DeviceId, Is.EqualTo("disk0"));
            Assert.That(config.BaseAddress, Is.EqualTo(0xC600U));
            Assert.That(config.Properties, Is.Not.Null);
            Assert.That(config.Properties!["setting"], Is.EqualTo(42));
        });
    }
}