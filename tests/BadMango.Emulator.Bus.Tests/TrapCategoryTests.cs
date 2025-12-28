// <copyright file="TrapCategoryTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="TrapCategory"/> enum.
/// </summary>
[TestFixture]
public class TrapCategoryTests
{
    /// <summary>
    /// Verifies that the enum contains all expected trap categories.
    /// </summary>
    [Test]
    public void TrapCategory_ContainsExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.Firmware), Is.True);
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.Monitor), Is.True);
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.BasicInterp), Is.True);
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.BasicRuntime), Is.True);
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.Dos), Is.True);
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.PrinterDriver), Is.True);
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.DiskDriver), Is.True);
            Assert.That(Enum.IsDefined(typeof(TrapCategory), TrapCategory.Custom), Is.True);
        });
    }

    /// <summary>
    /// Verifies that the enum has the expected number of values.
    /// </summary>
    [Test]
    public void TrapCategory_HasExpectedCount()
    {
        var values = Enum.GetValues<TrapCategory>();
        Assert.That(values, Has.Length.EqualTo(8));
    }
}