// <copyright file="DevicePageIdTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="DevicePageId"/> struct.
/// </summary>
[TestFixture]
public class DevicePageIdTests
{
    /// <summary>
    /// Verifies that default DevicePageId is invalid.
    /// </summary>
    [Test]
    public void Default_IsInvalid()
    {
        var pageId = DevicePageId.Default;

        Assert.Multiple(() =>
        {
            Assert.That(pageId.IsValid, Is.False);
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.Invalid));
        });
    }

    /// <summary>
    /// Verifies that constructor encodes values correctly.
    /// </summary>
    [Test]
    public void Constructor_EncodesValuesCorrectly()
    {
        var pageId = new DevicePageId(DevicePageClass.Storage, 0x123, 0x45);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.Storage));
            Assert.That(pageId.Instance, Is.EqualTo(0x123));
            Assert.That(pageId.Page, Is.EqualTo(0x45));
            Assert.That(pageId.IsValid, Is.True);
        });
    }

    /// <summary>
    /// Verifies that raw value constructor works.
    /// </summary>
    [Test]
    public void RawValueConstructor_DecodesCorrectly()
    {
        // Encode: class=3 (Storage), instance=0x123, page=0x45
        // = (3 << 20) | (0x123 << 8) | 0x45
        // = 0x312345
        var pageId = new DevicePageId(0x312345);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.Storage));
            Assert.That(pageId.Instance, Is.EqualTo(0x123));
            Assert.That(pageId.Page, Is.EqualTo(0x45));
        });
    }

    /// <summary>
    /// Verifies that RawValue property returns correct encoding.
    /// </summary>
    [Test]
    public void RawValue_ReturnsCorrectEncoding()
    {
        var pageId = new DevicePageId(DevicePageClass.Storage, 0x123, 0x45);

        Assert.That(pageId.RawValue, Is.EqualTo(0x312345u));
    }

    /// <summary>
    /// Verifies that CreateCompatIO creates correct page ID.
    /// </summary>
    [Test]
    public void CreateCompatIO_CreatesCorrectPageId()
    {
        var pageId = DevicePageId.CreateCompatIO(0x05, 0x02);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.CompatIO));
            Assert.That(pageId.Instance, Is.EqualTo(0x05));
            Assert.That(pageId.Page, Is.EqualTo(0x02));
        });
    }

    /// <summary>
    /// Verifies that CreateSlotROM creates correct page ID.
    /// </summary>
    [Test]
    public void CreateSlotROM_CreatesCorrectPageId()
    {
        var pageId = DevicePageId.CreateSlotROM(6, 0);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.SlotROM));
            Assert.That(pageId.Instance, Is.EqualTo(6));
            Assert.That(pageId.Page, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that CreateStorage creates correct page ID.
    /// </summary>
    [Test]
    public void CreateStorage_CreatesCorrectPageId()
    {
        var pageId = DevicePageId.CreateStorage(1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.Storage));
            Assert.That(pageId.Instance, Is.EqualTo(1));
            Assert.That(pageId.Page, Is.EqualTo(3));
        });
    }

    /// <summary>
    /// Verifies that CreateTimer creates correct page ID.
    /// </summary>
    [Test]
    public void CreateTimer_CreatesCorrectPageId()
    {
        var pageId = DevicePageId.CreateTimer(0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.Timer));
            Assert.That(pageId.Instance, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that CreateDebug creates correct page ID.
    /// </summary>
    [Test]
    public void CreateDebug_CreatesCorrectPageId()
    {
        var pageId = DevicePageId.CreateDebug(2, 1);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.Debug));
            Assert.That(pageId.Instance, Is.EqualTo(2));
            Assert.That(pageId.Page, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that Create factory method works.
    /// </summary>
    [Test]
    public void Create_FactoryMethod_CreatesCorrectPageId()
    {
        var pageId = DevicePageId.Create(DevicePageClass.Audio, 1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(pageId.Class, Is.EqualTo(DevicePageClass.Audio));
            Assert.That(pageId.Instance, Is.EqualTo(1));
            Assert.That(pageId.Page, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that equality works correctly.
    /// </summary>
    [Test]
    public void Equality_WorksCorrectly()
    {
        var pageId1 = new DevicePageId(DevicePageClass.Storage, 1, 0);
        var pageId2 = new DevicePageId(DevicePageClass.Storage, 1, 0);
        var pageId3 = new DevicePageId(DevicePageClass.Storage, 2, 0);

        Assert.Multiple(() =>
        {
            Assert.That(pageId1, Is.EqualTo(pageId2));
            Assert.That(pageId1 == pageId2, Is.True);
            Assert.That(pageId1 != pageId3, Is.True);
        });
    }

    /// <summary>
    /// Verifies that GetHashCode is consistent with equality.
    /// </summary>
    [Test]
    public void GetHashCode_IsConsistentWithEquality()
    {
        var pageId1 = new DevicePageId(DevicePageClass.Storage, 1, 0);
        var pageId2 = new DevicePageId(DevicePageClass.Storage, 1, 0);

        Assert.That(pageId1.GetHashCode(), Is.EqualTo(pageId2.GetHashCode()));
    }

    /// <summary>
    /// Verifies that ToString returns readable format.
    /// </summary>
    [Test]
    public void ToString_ReturnsReadableFormat()
    {
        var pageId = new DevicePageId(DevicePageClass.Storage, 6, 0);

        var result = pageId.ToString();

        Assert.That(result, Is.EqualTo("Storage:6:0"));
    }

    /// <summary>
    /// Verifies that instance is limited to 12 bits.
    /// </summary>
    [Test]
    public void Instance_IsLimitedTo12Bits()
    {
        // Trying to set instance > 4095 should be masked
        var pageId = new DevicePageId(DevicePageClass.Storage, 0x1FFF, 0);

        Assert.That(pageId.Instance, Is.EqualTo(0xFFF)); // Masked to 12 bits
    }
}