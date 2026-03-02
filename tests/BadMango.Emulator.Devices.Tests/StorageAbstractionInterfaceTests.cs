// <copyright file="StorageAbstractionInterfaceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Unit tests for host-side storage abstraction interface contracts.
/// </summary>
[TestFixture]
public class StorageAbstractionInterfaceTests
{
    /// <summary>
    /// Verifies that IControllerCard inherits from both IController and ISlotCard.
    /// </summary>
    [Test]
    public void IControllerCard_InheritsFromIControllerAndISlotCard()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(IController).IsAssignableFrom(typeof(IControllerCard)), Is.True);
            Assert.That(typeof(ISlotCard).IsAssignableFrom(typeof(IControllerCard)), Is.True);
        });
    }

    /// <summary>
    /// Verifies that IMedia exposes expected metrics and eventing members.
    /// </summary>
    [Test]
    public void IMedia_HasMetricsDictionaryAndMediaChangedEvent()
    {
        Assert.Multiple(() =>
        {
            var metadataProperty = typeof(IMedia).GetProperty(nameof(IMedia.Metadata));
            Assert.That(metadataProperty, Is.Not.Null);
            Assert.That(metadataProperty!.PropertyType, Is.EqualTo(typeof(IReadOnlyDictionary<string, string>)));

            var metricsProperty = typeof(IMedia).GetProperty(nameof(IMedia.Metrics));
            Assert.That(metricsProperty, Is.Not.Null);
            Assert.That(metricsProperty!.PropertyType, Is.EqualTo(typeof(MediaMetrics)));

            var dictionaryMethod = typeof(IMedia).GetMethod(nameof(IMedia.GetMetricsDictionary));
            Assert.That(dictionaryMethod, Is.Not.Null);
            Assert.That(dictionaryMethod!.ReturnType, Is.EqualTo(typeof(Dictionary<string, object>)));

            var mediaChangedEvent = typeof(IMedia).GetEvent(nameof(IMedia.MediaChanged));
            Assert.That(mediaChangedEvent, Is.Not.Null);
            Assert.That(mediaChangedEvent!.EventHandlerType, Is.EqualTo(typeof(EventHandler)));
        });
    }

    /// <summary>
    /// Verifies that IDrive exposes media state, metrics, and media change eventing.
    /// </summary>
    [Test]
    public void IDrive_HasCurrentMediaMetricsAndMediaChangedEvent()
    {
        Assert.Multiple(() =>
        {
            var currentMediaProperty = typeof(IDrive).GetProperty(nameof(IDrive.CurrentMedia));
            Assert.That(currentMediaProperty, Is.Not.Null);
            Assert.That(currentMediaProperty!.PropertyType, Is.EqualTo(typeof(IMedia)));

            var metricsProperty = typeof(IDrive).GetProperty(nameof(IDrive.Metrics));
            Assert.That(metricsProperty, Is.Not.Null);
            Assert.That(metricsProperty!.PropertyType, Is.EqualTo(typeof(DriveMetrics)));

            var mediaChangedEvent = typeof(IDrive).GetEvent(nameof(IDrive.MediaChanged));
            Assert.That(mediaChangedEvent, Is.Not.Null);
            Assert.That(mediaChangedEvent!.EventHandlerType, Is.EqualTo(typeof(EventHandler)));
        });
    }

    /// <summary>
    /// Verifies that IController exposes drive collection, metrics, and drive change eventing.
    /// </summary>
    [Test]
    public void IController_HasDrivesMetricsAndDriveChangedEvent()
    {
        Assert.Multiple(() =>
        {
            var drivesProperty = typeof(IController).GetProperty(nameof(IController.Drives));
            Assert.That(drivesProperty, Is.Not.Null);
            Assert.That(drivesProperty!.PropertyType, Is.EqualTo(typeof(IReadOnlyList<IDrive>)));

            var metricsProperty = typeof(IController).GetProperty(nameof(IController.Metrics));
            Assert.That(metricsProperty, Is.Not.Null);
            Assert.That(metricsProperty!.PropertyType, Is.EqualTo(typeof(ControllerMetrics)));

            var driveChangedEvent = typeof(IController).GetEvent(nameof(IController.DriveChanged));
            Assert.That(driveChangedEvent, Is.Not.Null);
            Assert.That(driveChangedEvent!.EventHandlerType, Is.EqualTo(typeof(EventHandler<ControllerEventArgs>)));
        });
    }

    /// <summary>
    /// Verifies that IDiskImageTooling exposes image creation and conversion methods.
    /// </summary>
    [Test]
    public void IDiskImageTooling_HasCreateAndConvertMethods()
    {
        Assert.Multiple(() =>
        {
            var createMethod = typeof(IDiskImageTooling).GetMethod(nameof(IDiskImageTooling.CreateBlankImage));
            Assert.That(createMethod, Is.Not.Null);
            Assert.That(createMethod!.ReturnType, Is.EqualTo(typeof(IMedia)));
            Assert.That(createMethod.GetParameters(), Has.Length.EqualTo(1));
            Assert.That(createMethod.GetParameters()[0].ParameterType, Is.EqualTo(typeof(DiskImageCreationOptions)));

            var convertMethod = typeof(IDiskImageTooling).GetMethod(nameof(IDiskImageTooling.ConvertImage));
            Assert.That(convertMethod, Is.Not.Null);
            Assert.That(convertMethod!.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(convertMethod.GetParameters(), Has.Length.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(convertMethod.GetParameters()[0].ParameterType, Is.EqualTo(typeof(IMedia)));
                Assert.That(convertMethod.GetParameters()[1].ParameterType, Is.EqualTo(typeof(string)));
                Assert.That(convertMethod.GetParameters()[2].ParameterType, Is.EqualTo(typeof(Stream)));
            });
        });
    }

    /// <summary>
    /// Verifies metrics and event argument types expose dictionary conversion and expected properties.
    /// </summary>
    [Test]
    public void MetricsAndEventTypes_HaveExpectedMembers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(MediaMetrics).GetMethod(nameof(MediaMetrics.ToDictionary)), Is.Not.Null);
            Assert.That(typeof(DriveMetrics).GetMethod(nameof(DriveMetrics.ToDictionary)), Is.Not.Null);
            Assert.That(typeof(ControllerMetrics).GetMethod(nameof(ControllerMetrics.ToDictionary)), Is.Not.Null);

            var changeKindProperty = typeof(ControllerEventArgs).GetProperty(nameof(ControllerEventArgs.ChangeKind));
            Assert.That(changeKindProperty, Is.Not.Null);
            Assert.That(changeKindProperty!.PropertyType, Is.EqualTo(typeof(ControllerDriveChangeKind)));

            var driveProperty = typeof(ControllerEventArgs).GetProperty(nameof(ControllerEventArgs.Drive));
            Assert.That(driveProperty, Is.Not.Null);
            Assert.That(driveProperty!.PropertyType, Is.EqualTo(typeof(IDrive)));
        });
    }
}