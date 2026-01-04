// <copyright file="RomDescriptorTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for <see cref="RomDescriptor"/>.
/// </summary>
[TestFixture]
public class RomDescriptorTests
{
    /// <summary>
    /// Verifies that RomDescriptor can be created with required parameters.
    /// </summary>
    [Test]
    public void Constructor_WithRequiredParameters_SetsProperties()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };

        var descriptor = new RomDescriptor(data, 0xC000);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Data, Is.SameAs(data));
            Assert.That(descriptor.LoadAddress, Is.EqualTo(0xC000));
            Assert.That(descriptor.Name, Is.Null);
            Assert.That(descriptor.Description, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that RomDescriptor can be created with all parameters.
    /// </summary>
    [Test]
    public void Constructor_WithAllParameters_SetsProperties()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };

        var descriptor = new RomDescriptor(data, 0xC000, "Test ROM", "A test ROM");

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Data, Is.SameAs(data));
            Assert.That(descriptor.LoadAddress, Is.EqualTo(0xC000));
            Assert.That(descriptor.Name, Is.EqualTo("Test ROM"));
            Assert.That(descriptor.Description, Is.EqualTo("A test ROM"));
        });
    }

    /// <summary>
    /// Verifies PocketIIeFull factory method.
    /// </summary>
    [Test]
    public void PocketIIeFull_CreatesCorrectDescriptor()
    {
        var data = new byte[RomDescriptor.FullRomSize];

        var descriptor = RomDescriptor.PocketIIeFull(data);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.LoadAddress, Is.EqualTo(RomDescriptor.FullRomBase));
            Assert.That(descriptor.Name, Is.EqualTo("Pocket IIe Full ROM"));
            Assert.That(descriptor.Description, Is.Not.Null);
        });
    }

    /// <summary>
    /// Verifies Applesoft factory method.
    /// </summary>
    [Test]
    public void Applesoft_CreatesCorrectDescriptor()
    {
        var data = new byte[RomDescriptor.ApplesoftSize];

        var descriptor = RomDescriptor.Applesoft(data);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.LoadAddress, Is.EqualTo(RomDescriptor.ApplesoftBase));
            Assert.That(descriptor.Name, Is.EqualTo("Applesoft BASIC"));
        });
    }

    /// <summary>
    /// Verifies Monitor factory method.
    /// </summary>
    [Test]
    public void Monitor_CreatesCorrectDescriptor()
    {
        var data = new byte[RomDescriptor.MonitorSize];

        var descriptor = RomDescriptor.Monitor(data);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.LoadAddress, Is.EqualTo(RomDescriptor.MonitorBase));
            Assert.That(descriptor.Name, Is.EqualTo("Monitor"));
        });
    }

    /// <summary>
    /// Verifies Custom factory method with name.
    /// </summary>
    [Test]
    public void Custom_WithName_CreatesCorrectDescriptor()
    {
        var data = new byte[] { 0x01, 0x02 };

        var descriptor = RomDescriptor.Custom(data, 0x8000, "Custom ROM");

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.Data, Is.SameAs(data));
            Assert.That(descriptor.LoadAddress, Is.EqualTo(0x8000));
            Assert.That(descriptor.Name, Is.EqualTo("Custom ROM"));
        });
    }

    /// <summary>
    /// Verifies Custom factory method without name.
    /// </summary>
    [Test]
    public void Custom_WithoutName_CreatesCorrectDescriptor()
    {
        var data = new byte[] { 0x01, 0x02 };

        var descriptor = RomDescriptor.Custom(data, 0x8000);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.LoadAddress, Is.EqualTo(0x8000));
            Assert.That(descriptor.Name, Is.Null);
        });
    }

    /// <summary>
    /// Verifies constants have correct values.
    /// </summary>
    [Test]
    public void Constants_HaveCorrectValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RomDescriptor.FullRomSize, Is.EqualTo(16384));
            Assert.That(RomDescriptor.ApplesoftSize, Is.EqualTo(10240));
            Assert.That(RomDescriptor.MonitorSize, Is.EqualTo(2048));
            Assert.That(RomDescriptor.FullRomBase, Is.EqualTo(0xC000));
            Assert.That(RomDescriptor.ApplesoftBase, Is.EqualTo(0xD800));
            Assert.That(RomDescriptor.MonitorBase, Is.EqualTo(0xF800));
        });
    }

    /// <summary>
    /// Verifies RomDescriptor equality.
    /// </summary>
    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var data = new byte[] { 0x01, 0x02 };
        var descriptor1 = new RomDescriptor(data, 0x8000, "Test", "Description");
        var descriptor2 = new RomDescriptor(data, 0x8000, "Test", "Description");

        Assert.That(descriptor1, Is.EqualTo(descriptor2));
    }

    /// <summary>
    /// Verifies RomDescriptor inequality.
    /// </summary>
    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var data = new byte[] { 0x01, 0x02 };
        var descriptor1 = new RomDescriptor(data, 0x8000);
        var descriptor2 = new RomDescriptor(data, 0x9000);

        Assert.That(descriptor1, Is.Not.EqualTo(descriptor2));
    }
}