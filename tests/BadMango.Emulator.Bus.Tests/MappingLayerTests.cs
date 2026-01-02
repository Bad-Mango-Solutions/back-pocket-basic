// <copyright file="MappingLayerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="MappingLayer"/> record struct.
/// </summary>
[TestFixture]
public class MappingLayerTests
{
    /// <summary>
    /// Verifies that MappingLayer is created with correct properties.
    /// </summary>
    [Test]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var layer = new MappingLayer("TestLayer", 42, true);

        Assert.Multiple(() =>
        {
            Assert.That(layer.Name, Is.EqualTo("TestLayer"));
            Assert.That(layer.Priority, Is.EqualTo(42));
            Assert.That(layer.IsActive, Is.True);
        });
    }

    /// <summary>
    /// Verifies that MappingLayer can be created with inactive state.
    /// </summary>
    [Test]
    public void Constructor_InactiveLayer_HasCorrectState()
    {
        var layer = new MappingLayer("InactiveLayer", 10, false);

        Assert.That(layer.IsActive, Is.False);
    }

    /// <summary>
    /// Verifies that WithActive creates new instance with changed state.
    /// </summary>
    [Test]
    public void WithActive_CreatesNewInstanceWithChangedState()
    {
        var original = new MappingLayer("Test", 5, false);

        var activated = original.WithActive(true);

        Assert.Multiple(() =>
        {
            Assert.That(activated.Name, Is.EqualTo("Test"), "Name should be preserved");
            Assert.That(activated.Priority, Is.EqualTo(5), "Priority should be preserved");
            Assert.That(activated.IsActive, Is.True, "Active state should be changed");
            Assert.That(original.IsActive, Is.False, "Original should be unchanged (immutable)");
        });
    }

    /// <summary>
    /// Verifies that WithActive(false) deactivates a layer.
    /// </summary>
    [Test]
    public void WithActive_False_DeactivatesLayer()
    {
        var original = new MappingLayer("Test", 5, true);

        var deactivated = original.WithActive(false);

        Assert.Multiple(() =>
        {
            Assert.That(deactivated.IsActive, Is.False);
            Assert.That(original.IsActive, Is.True, "Original should be unchanged");
        });
    }

    /// <summary>
    /// Verifies record equality for MappingLayer.
    /// </summary>
    [Test]
    public void Equality_SameValues_AreEqual()
    {
        var layer1 = new MappingLayer("Test", 10, true);
        var layer2 = new MappingLayer("Test", 10, true);

        Assert.That(layer1, Is.EqualTo(layer2));
    }

    /// <summary>
    /// Verifies record inequality for different values.
    /// </summary>
    [Test]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var layer1 = new MappingLayer("Test1", 10, true);
        var layer2 = new MappingLayer("Test2", 10, true);
        var layer3 = new MappingLayer("Test1", 20, true);
        var layer4 = new MappingLayer("Test1", 10, false);

        Assert.Multiple(() =>
        {
            Assert.That(layer1, Is.Not.EqualTo(layer2), "Different name");
            Assert.That(layer1, Is.Not.EqualTo(layer3), "Different priority");
            Assert.That(layer1, Is.Not.EqualTo(layer4), "Different active state");
        });
    }

    /// <summary>
    /// Verifies negative priority values are allowed.
    /// </summary>
    [Test]
    public void Constructor_NegativePriority_IsAllowed()
    {
        var layer = new MappingLayer("LowPriority", -100, false);

        Assert.That(layer.Priority, Is.EqualTo(-100));
    }

    /// <summary>
    /// Verifies empty name is allowed (validation at usage site).
    /// </summary>
    [Test]
    public void Constructor_EmptyName_IsAllowed()
    {
        var layer = new MappingLayer(string.Empty, 0, false);

        Assert.That(layer.Name, Is.EqualTo(string.Empty));
    }
}