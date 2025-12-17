// <copyright file="ForLoopManagerTests.cs" company="Josh Pactor">
// Copyright (c) Josh Pactor. All rights reserved.
// </copyright>

namespace ApplesoftBasic.Tests;

using Interpreter.Runtime;

/// <summary>
/// Tests for <see cref="ForLoopManager"/>.
/// </summary>
[TestFixture]
public class ForLoopManagerTests
{
    /// <summary>
    /// Pushing replaces prior loop for same variable.
    /// </summary>
    [Test]
    public void PushFor_ReplacesExistingLoopForVariable()
    {
        var manager = new ForLoopManager();
        manager.PushFor(new ForLoopState("I", 3, 1, 0, 0));
        manager.PushFor(new ForLoopState("J", 2, 1, 0, 0));
        manager.PushFor(new ForLoopState("I", 5, 1, 1, 1));

        var state = manager.GetForLoop("I");
        Assert.That(state?.ReturnLineIndex, Is.EqualTo(1));
    }

    /// <summary>
    /// PopFor with unknown variable throws and restores stack.
    /// </summary>
    [Test]
    public void PopFor_WithVariableRestoresStackAndThrowsWhenMissing()
    {
        var manager = new ForLoopManager();
        manager.PushFor(new ForLoopState("I", 3, 1, 0, 0));
        manager.PushFor(new ForLoopState("J", 2, 1, 0, 0));

        Assert.That(() => manager.PopFor("K"), Throws.TypeOf<BasicRuntimeException>());
        Assert.That(manager.GetForLoop("I"), Is.Not.Null);
    }

    /// <summary>
    /// PopFor without variable returns top item.
    /// </summary>
    [Test]
    public void PopFor_WithoutVariableReturnsTop()
    {
        var manager = new ForLoopManager();
        manager.PushFor(new ForLoopState("I", 3, 1, 0, 0));

        var state = manager.PopFor(null);

        Assert.That(state?.Variable, Is.EqualTo("I"));
    }
}