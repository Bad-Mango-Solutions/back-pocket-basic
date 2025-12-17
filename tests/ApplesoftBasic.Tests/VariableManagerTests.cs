// <copyright file="VariableManagerTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Runtime;
using Microsoft.Extensions.Logging;
using Moq;

/// <summary>
/// Tests for <see cref="VariableManager"/>.
/// </summary>
[TestFixture]
public class VariableManagerTests
{
    /// <summary>
    /// Defaults for missing variables are type-specific.
    /// </summary>
    [Test]
    public void GetVariable_DefaultsByType()
    {
        var manager = CreateManager();

        Assert.That(manager.GetVariable("X").AsNumber(), Is.EqualTo(0));
        Assert.That(manager.GetVariable("A$").AsString(), Is.EqualTo(string.Empty));
    }

    /// <summary>
    /// Setting type mismatch throws.
    /// </summary>
    [Test]
    public void SetVariable_TypeMismatchThrows()
    {
        var manager = CreateManager();

        Assert.That(() => manager.SetVariable("A$", BasicValue.FromNumber(1)), Throws.TypeOf<BasicRuntimeException>());
    }

    /// <summary>
    /// Arrays auto-dimension and set/retrieve values.
    /// </summary>
    [Test]
    public void Arrays_AutoDimensionAndSet()
    {
        var manager = CreateManager();

        manager.SetArrayElement("A", [2], BasicValue.FromNumber(5));

        Assert.That(manager.GetArrayElement("A", [2]).AsNumber(), Is.EqualTo(5));
    }

    /// <summary>
    /// Helper to create a manager with mocked logger.
    /// </summary>
    private static VariableManager CreateManager()
    {
        var logger = new Mock<ILogger<VariableManager>>();
        return new VariableManager(logger.Object);
    }
}