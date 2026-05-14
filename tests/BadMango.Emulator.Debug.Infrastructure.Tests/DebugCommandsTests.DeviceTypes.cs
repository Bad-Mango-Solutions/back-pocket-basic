// <copyright file="DebugCommandsTests.DeviceTypes.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

using BadMango.Emulator.Devices;

/// <summary>
/// Unit tests for <see cref="DeviceTypesCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Verifies that <see cref="DeviceTypesCommand"/> exposes the expected name.
    /// </summary>
    [Test]
    public void DeviceTypesCommand_HasCorrectName()
    {
        var command = new DeviceTypesCommand();
        Assert.That(command.Name, Is.EqualTo("devicetypes"));
    }

    /// <summary>
    /// Verifies that <see cref="DeviceTypesCommand"/> exposes the expected aliases.
    /// </summary>
    [Test]
    public void DeviceTypesCommand_HasCorrectAliases()
    {
        var command = new DeviceTypesCommand();
        Assert.That(command.Aliases, Is.EquivalentTo(new[] { "devtypes", "devicecatalog" }));
    }

    /// <summary>
    /// Verifies that <see cref="DeviceTypesCommand"/> lists the expected sections and
    /// includes the auto-discovered slot card and motherboard device type ids that
    /// machine profiles can reference, regardless of whether the active context has
    /// a loaded machine.
    /// </summary>
    [Test]
    public void DeviceTypesCommand_ListsAvailableTypes()
    {
        // Make sure the registry has been populated so the command sees the well-known ids.
        DeviceFactoryRegistry.EnsureInitialized();

        var command = new DeviceTypesCommand();
        var result = command.Execute(debugContext, []);
        var output = outputWriter.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(output, Does.Contain("Available Device Types:"));
            Assert.That(output, Does.Contain("Motherboard devices"));
            Assert.That(output, Does.Contain("Slot cards"));
            Assert.That(output, Does.Contain("Skipped"));

            // Slot card the registry must always discover (Disk II is constructed via
            // Serilog.ILogger injection through DeviceFactoryRegistry.LoggerFactory).
            Assert.That(output, Does.Contain("disk-ii-compatible"));
            Assert.That(output, Does.Contain("pocketwatch"));

            // Motherboard device that recently gained [DeviceType] must appear.
            Assert.That(output, Does.Contain("gameio"));
        });
    }
}