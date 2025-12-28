// <copyright file="SignalLineTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="SignalLine"/> enum.
/// </summary>
[TestFixture]
public class SignalLineTests
{
    /// <summary>
    /// Verifies that all expected signal lines exist.
    /// </summary>
    [Test]
    public void SignalLine_ContainsExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.IsDefined(SignalLine.IRQ), Is.True);
            Assert.That(Enum.IsDefined(SignalLine.NMI), Is.True);
            Assert.That(Enum.IsDefined(SignalLine.Reset), Is.True);
            Assert.That(Enum.IsDefined(SignalLine.RDY), Is.True);
            Assert.That(Enum.IsDefined(SignalLine.DmaReq), Is.True);
        });
    }

    /// <summary>
    /// Verifies signal lines have expected values.
    /// </summary>
    [Test]
    public void SignalLine_ValuesAreSequential()
    {
        Assert.Multiple(() =>
        {
            Assert.That((byte)SignalLine.IRQ, Is.EqualTo(0));
            Assert.That((byte)SignalLine.NMI, Is.EqualTo(1));
            Assert.That((byte)SignalLine.Reset, Is.EqualTo(2));
            Assert.That((byte)SignalLine.RDY, Is.EqualTo(3));
            Assert.That((byte)SignalLine.DmaReq, Is.EqualTo(4));
        });
    }

    /// <summary>
    /// Verifies the enum has exactly five values.
    /// </summary>
    [Test]
    public void SignalLine_HasFiveValues()
    {
        var values = Enum.GetValues<SignalLine>();
        Assert.That(values, Has.Length.EqualTo(5));
    }
}