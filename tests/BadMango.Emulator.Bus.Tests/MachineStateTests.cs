// <copyright file="MachineStateTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for <see cref="MachineState"/>.
/// </summary>
[TestFixture]
public class MachineStateTests
{
    /// <summary>
    /// Verifies that MachineState has expected values.
    /// </summary>
    [Test]
    public void MachineState_HasExpectedValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MachineState.Stopped, Is.EqualTo((MachineState)0));
            Assert.That(MachineState.Running, Is.EqualTo((MachineState)1));
            Assert.That(MachineState.Paused, Is.EqualTo((MachineState)2));
        });
    }

    /// <summary>
    /// Verifies that all enum values are distinct.
    /// </summary>
    [Test]
    public void MachineState_ValuesAreDistinct()
    {
        var values = Enum.GetValues<MachineState>();

        Assert.That(values.Distinct().Count(), Is.EqualTo(values.Length));
    }

    /// <summary>
    /// Verifies that MachineState can be converted to string.
    /// </summary>
    [Test]
    public void MachineState_ToStringWorks()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MachineState.Stopped.ToString(), Is.EqualTo("Stopped"));
            Assert.That(MachineState.Running.ToString(), Is.EqualTo("Running"));
            Assert.That(MachineState.Paused.ToString(), Is.EqualTo("Paused"));
        });
    }
}