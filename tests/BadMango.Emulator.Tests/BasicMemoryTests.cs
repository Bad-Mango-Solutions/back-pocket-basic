// <copyright file="BasicMemoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Tests;

using BadMango.Emulator.Core;
using BadMango.Emulator.Emulation.Memory;

/// <summary>
/// Unit tests for the <see cref="BasicMemory"/> class.
/// </summary>
[TestFixture]
public class BasicMemoryTests
{
    /// <summary>
    /// Verifies that BasicMemory can be created with MemorySizes constants.
    /// </summary>
    [Test]
    public void BasicMemory_CanBeCreatedWithMemorySizesConstants()
    {
        // Arrange & Act
        var mem64K = new BasicMemory(MemorySizes.Size64KB);
        var mem128K = new BasicMemory(MemorySizes.Size128KB);
        var mem256K = new BasicMemory(MemorySizes.Size256KB);

        // Assert
        Assert.That(mem64K.Size, Is.EqualTo(65536u));
        Assert.That(mem128K.Size, Is.EqualTo(131072u));
        Assert.That(mem256K.Size, Is.EqualTo(262144u));
    }

    /// <summary>
    /// Verifies that BasicMemory uses Size64KB as default size.
    /// </summary>
    [Test]
    public void BasicMemory_DefaultSizeIs64KB()
    {
        // Arrange & Act
        var memory = new BasicMemory();

        // Assert
        Assert.That(memory.Size, Is.EqualTo(MemorySizes.Size64KB));
        Assert.That(memory.Size, Is.EqualTo(65536u));
    }

    /// <summary>
    /// Verifies that AsReadOnlyMemory returns a view over the backing array.
    /// </summary>
    [Test]
    public void AsReadOnlyMemory_ReturnsViewOverBackingArray()
    {
        // Arrange
        var memory = new BasicMemory(MemorySizes.Size64KB);
        memory.Write(0x100, 0x42);
        memory.Write(0x200, 0xFF);

        // Act
        var view = memory.AsReadOnlyMemory();

        // Assert
        Assert.That(view.Length, Is.EqualTo(65536));
        Assert.That(view.Span[0x100], Is.EqualTo(0x42));
        Assert.That(view.Span[0x200], Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that AsMemory returns a mutable view over the backing array.
    /// </summary>
    [Test]
    public void AsMemory_ReturnsMutableView()
    {
        // Arrange
        var memory = new BasicMemory(MemorySizes.Size64KB);
        memory.Write(0x100, 0x42);

        // Act
        var view = memory.AsMemory();
        view.Span[0x200] = 0xAA;

        // Assert
        Assert.That(view.Length, Is.EqualTo(65536));
        Assert.That(view.Span[0x100], Is.EqualTo(0x42));
        Assert.That(view.Span[0x200], Is.EqualTo(0xAA));
        Assert.That(memory.Read(0x200), Is.EqualTo(0xAA));
    }

    /// <summary>
    /// Verifies that AsReadOnlyMemory returns a view that reflects changes to the underlying memory.
    /// </summary>
    [Test]
    public void AsReadOnlyMemory_ReflectsUnderlyingMemoryChanges()
    {
        // Arrange
        var memory = new BasicMemory(MemorySizes.Size64KB);
        memory.Write(0x100, 0x42);

        var view1 = memory.AsReadOnlyMemory();

        // Act - modify memory after obtaining view
        memory.Write(0x100, 0xFF);
        var view2 = memory.AsReadOnlyMemory();

        // Assert - both views reflect the current state of the backing array
        Assert.That(view1.Span[0x100], Is.EqualTo(0xFF));
        Assert.That(view2.Span[0x100], Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that MemorySizes constants have expected values.
    /// </summary>
    [Test]
    public void MemorySizes_ConstantsHaveExpectedValues()
    {
        // Assert standard sizes (64KB and above only)
        Assert.That(MemorySizes.Size64KB, Is.EqualTo(65536u));
        Assert.That(MemorySizes.Size128KB, Is.EqualTo(131072u));
        Assert.That(MemorySizes.Size256KB, Is.EqualTo(262144u));
        Assert.That(MemorySizes.Size512KB, Is.EqualTo(524288u));
        Assert.That(MemorySizes.Size1MB, Is.EqualTo(1048576u));
        Assert.That(MemorySizes.Size8MB, Is.EqualTo(8388608u));
        Assert.That(MemorySizes.Size16MB, Is.EqualTo(16777216u));
    }
}