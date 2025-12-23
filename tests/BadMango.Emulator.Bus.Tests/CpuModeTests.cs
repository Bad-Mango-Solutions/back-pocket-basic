// <copyright file="CpuModeTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="CpuMode"/> enum.
/// </summary>
[TestFixture]
public class CpuModeTests
{
    /// <summary>
    /// Verifies that CpuMode.Native has value 0.
    /// </summary>
    [Test]
    public void CpuMode_Native_HasValueZero()
    {
        Assert.That((int)CpuMode.Native, Is.EqualTo(0));
    }

    /// <summary>
    /// Verifies that CpuMode.Compat has value 1.
    /// </summary>
    [Test]
    public void CpuMode_Compat_HasValueOne()
    {
        Assert.That((int)CpuMode.Compat, Is.EqualTo(1));
    }

    /// <summary>
    /// Verifies that the enum has exactly two values.
    /// </summary>
    [Test]
    public void CpuMode_HasTwoValues()
    {
        var values = Enum.GetValues<CpuMode>();
        Assert.That(values, Has.Length.EqualTo(2));
    }
}