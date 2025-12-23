// <copyright file="FaultKindTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="FaultKind"/> enum.
/// </summary>
[TestFixture]
public class FaultKindTests
{
    /// <summary>
    /// Verifies that None has value 0.
    /// </summary>
    [Test]
    public void FaultKind_None_HasValueZero()
    {
        Assert.That((byte)FaultKind.None, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that all expected fault kinds exist.
    /// </summary>
    [Test]
    public void FaultKind_ContainsExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.IsDefined(FaultKind.None), Is.True);
            Assert.That(Enum.IsDefined(FaultKind.Unmapped), Is.True);
            Assert.That(Enum.IsDefined(FaultKind.Permission), Is.True);
            Assert.That(Enum.IsDefined(FaultKind.Nx), Is.True);
            Assert.That(Enum.IsDefined(FaultKind.Misaligned), Is.True);
            Assert.That(Enum.IsDefined(FaultKind.DeviceFault), Is.True);
        });
    }

    /// <summary>
    /// Verifies the enum has exactly six values.
    /// </summary>
    [Test]
    public void FaultKind_HasSixValues()
    {
        var values = Enum.GetValues<FaultKind>();
        Assert.That(values, Has.Length.EqualTo(6));
    }
}