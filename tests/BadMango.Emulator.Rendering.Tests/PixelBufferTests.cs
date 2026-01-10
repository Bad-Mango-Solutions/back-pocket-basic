// <copyright file="PixelBufferTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering.Tests;

/// <summary>
/// Unit tests for the <see cref="PixelBuffer"/> class.
/// Tests focus on memory safety, bounds checking, and proper resource disposal.
/// Note: Tests that require Avalonia platform are skipped in headless environments.
/// </summary>
[TestFixture]
public class PixelBufferTests
{
    /// <summary>
    /// Verifies that constructor throws when width is zero.
    /// </summary>
    [Test]
    public void Constructor_ZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PixelBuffer(0, 100));
    }

    /// <summary>
    /// Verifies that constructor throws when height is zero.
    /// </summary>
    [Test]
    public void Constructor_ZeroHeight_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PixelBuffer(100, 0));
    }

    /// <summary>
    /// Verifies that constructor throws when width is negative.
    /// </summary>
    [Test]
    public void Constructor_NegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PixelBuffer(-1, 100));
    }

    /// <summary>
    /// Verifies that constructor throws when height is negative.
    /// </summary>
    [Test]
    public void Constructor_NegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PixelBuffer(100, -1));
    }
}