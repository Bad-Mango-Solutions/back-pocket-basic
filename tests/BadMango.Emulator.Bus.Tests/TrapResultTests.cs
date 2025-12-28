// <copyright file="TrapResultTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="TrapResult"/> struct.
/// </summary>
[TestFixture]
public class TrapResultTests
{
    /// <summary>
    /// Verifies that NotHandled creates an unhandled result.
    /// </summary>
    [Test]
    public void NotHandled_CreatesUnhandledResult()
    {
        var result = TrapResult.NotHandled();

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.CyclesConsumed, Is.EqualTo(0ul));
            Assert.That(result.ReturnAddress, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that Success creates a handled result with cycles.
    /// </summary>
    [Test]
    public void Success_CreatesHandledResultWithCycles()
    {
        var result = TrapResult.Success(100);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.CyclesConsumed, Is.EqualTo(100ul));
            Assert.That(result.ReturnAddress, Is.Null);
        });
    }

    /// <summary>
    /// Verifies that SuccessWithReturn creates a handled result with cycles and return address.
    /// </summary>
    [Test]
    public void SuccessWithReturn_CreatesHandledResultWithReturnAddress()
    {
        var result = TrapResult.SuccessWithReturn(50, 0x1234);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.CyclesConsumed, Is.EqualTo(50ul));
            Assert.That(result.ReturnAddress, Is.EqualTo(0x1234u));
        });
    }

    /// <summary>
    /// Verifies that the constructor creates a result with specified values.
    /// </summary>
    [Test]
    public void Constructor_SetsAllProperties()
    {
        var result = new TrapResult(true, 200, 0xFC58);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.True);
            Assert.That(result.CyclesConsumed, Is.EqualTo(200ul));
            Assert.That(result.ReturnAddress, Is.EqualTo(0xFC58u));
        });
    }

    /// <summary>
    /// Verifies that default constructor creates unhandled result.
    /// </summary>
    [Test]
    public void DefaultConstructor_CreatesUnhandledResult()
    {
        var result = default(TrapResult);

        Assert.Multiple(() =>
        {
            Assert.That(result.Handled, Is.False);
            Assert.That(result.CyclesConsumed, Is.EqualTo(0ul));
            Assert.That(result.ReturnAddress, Is.Null);
        });
    }
}