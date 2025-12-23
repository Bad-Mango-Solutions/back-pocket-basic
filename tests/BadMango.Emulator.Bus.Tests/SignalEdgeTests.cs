// <copyright file="SignalEdgeTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="SignalEdge"/> struct.
/// </summary>
[TestFixture]
public class SignalEdgeTests
{
    /// <summary>
    /// Verifies that SignalEdge can be created with all properties.
    /// </summary>
    [Test]
    public void SignalEdge_CanBeCreatedWithProperties()
    {
        var edge = new SignalEdge(
            Line: SignalLine.Irq,
            NewState: SignalState.Asserted,
            DeviceId: 5,
            Cycle: 12345ul);

        Assert.Multiple(() =>
        {
            Assert.That(edge.Line, Is.EqualTo(SignalLine.Irq));
            Assert.That(edge.NewState, Is.EqualTo(SignalState.Asserted));
            Assert.That(edge.DeviceId, Is.EqualTo(5));
            Assert.That(edge.Cycle, Is.EqualTo(12345ul));
        });
    }

    /// <summary>
    /// Verifies IsRisingEdge returns true when new state is Asserted.
    /// </summary>
    [Test]
    public void SignalEdge_IsRisingEdge_TrueWhenAsserted()
    {
        var edge = new SignalEdge(SignalLine.Irq, SignalState.Asserted, 1, 100);
        Assert.That(edge.IsRisingEdge, Is.True);
    }

    /// <summary>
    /// Verifies IsRisingEdge returns false when new state is Clear.
    /// </summary>
    [Test]
    public void SignalEdge_IsRisingEdge_FalseWhenClear()
    {
        var edge = new SignalEdge(SignalLine.Irq, SignalState.Clear, 1, 100);
        Assert.That(edge.IsRisingEdge, Is.False);
    }

    /// <summary>
    /// Verifies IsFallingEdge returns true when new state is Clear.
    /// </summary>
    [Test]
    public void SignalEdge_IsFallingEdge_TrueWhenClear()
    {
        var edge = new SignalEdge(SignalLine.Nmi, SignalState.Clear, 2, 200);
        Assert.That(edge.IsFallingEdge, Is.True);
    }

    /// <summary>
    /// Verifies IsFallingEdge returns false when new state is Asserted.
    /// </summary>
    [Test]
    public void SignalEdge_IsFallingEdge_FalseWhenAsserted()
    {
        var edge = new SignalEdge(SignalLine.Nmi, SignalState.Asserted, 2, 200);
        Assert.That(edge.IsFallingEdge, Is.False);
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Test]
    public void SignalEdge_RecordEquality_Works()
    {
        var edge1 = new SignalEdge(SignalLine.Irq, SignalState.Asserted, 1, 100);
        var edge2 = new SignalEdge(SignalLine.Irq, SignalState.Asserted, 1, 100);
        var edge3 = new SignalEdge(SignalLine.Nmi, SignalState.Asserted, 1, 100);

        Assert.Multiple(() =>
        {
            Assert.That(edge1, Is.EqualTo(edge2));
            Assert.That(edge1, Is.Not.EqualTo(edge3));
        });
    }
}