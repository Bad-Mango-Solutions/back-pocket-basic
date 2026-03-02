// <copyright file="StorageAbstractionInterfaceTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Tests;

using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices.Storage;

/// <summary>
/// Unit tests for host-side storage abstraction interface contracts.
/// </summary>
[TestFixture]
public class StorageAbstractionInterfaceTests
{
    /// <summary>
    /// Verifies that IStorageControllerCard inherits from both IStorageController and ISlotCard.
    /// </summary>
    [Test]
    public void IStorageControllerCard_InheritsFromIStorageControllerAndISlotCard()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(IStorageController).IsAssignableFrom(typeof(IStorageControllerCard)), Is.True);
            Assert.That(typeof(ISlotCard).IsAssignableFrom(typeof(IStorageControllerCard)), Is.True);
        });
    }

    /// <summary>
    /// Verifies that IStorageMedia exposes expected metrics and eventing members.
    /// </summary>
    [Test]
    public void IStorageMedia_HasMetricsDictionaryAndMediaChangedEvent()
    {
        Assert.Multiple(() =>
        {
            var metadataProperty = typeof(IStorageMedia).GetProperty(nameof(IStorageMedia.Metadata));
            Assert.That(metadataProperty, Is.Not.Null);
            Assert.That(metadataProperty!.PropertyType, Is.EqualTo(typeof(IReadOnlyDictionary<string, string>)));

            var metricsProperty = typeof(IStorageMedia).GetProperty(nameof(IStorageMedia.Metrics));
            Assert.That(metricsProperty, Is.Not.Null);
            Assert.That(metricsProperty!.PropertyType, Is.EqualTo(typeof(MediaMetrics)));

            var dictionaryMethod = typeof(IStorageMedia).GetMethod(nameof(IStorageMedia.GetMetricsDictionary));
            Assert.That(dictionaryMethod, Is.Not.Null);
            Assert.That(dictionaryMethod!.ReturnType, Is.EqualTo(typeof(Dictionary<string, object>)));

            var mediaChangedEvent = typeof(IStorageMedia).GetEvent(nameof(IStorageMedia.MediaChanged));
            Assert.That(mediaChangedEvent, Is.Not.Null);
            Assert.That(mediaChangedEvent!.EventHandlerType, Is.EqualTo(typeof(EventHandler)));
        });
    }

    /// <summary>
    /// Verifies that IStorageDrive exposes media state, metrics, and media change eventing.
    /// </summary>
    [Test]
    public void IStorageDrive_HasCurrentMediaMetricsAndMediaChangedEvent()
    {
        Assert.Multiple(() =>
        {
            var currentMediaProperty = typeof(IStorageDrive).GetProperty(nameof(IStorageDrive.CurrentMedia));
            Assert.That(currentMediaProperty, Is.Not.Null);
            Assert.That(currentMediaProperty!.PropertyType, Is.EqualTo(typeof(IStorageMedia)));

            var metricsProperty = typeof(IStorageDrive).GetProperty(nameof(IStorageDrive.Metrics));
            Assert.That(metricsProperty, Is.Not.Null);
            Assert.That(metricsProperty!.PropertyType, Is.EqualTo(typeof(DriveMetrics)));

            var mediaChangedEvent = typeof(IStorageDrive).GetEvent(nameof(IStorageDrive.MediaChanged));
            Assert.That(mediaChangedEvent, Is.Not.Null);
            Assert.That(mediaChangedEvent!.EventHandlerType, Is.EqualTo(typeof(EventHandler)));
        });
    }

    /// <summary>
    /// Verifies that IStorageController exposes drive collection, metrics, and drive change eventing.
    /// </summary>
    [Test]
    public void IStorageController_HasDrivesMetricsAndDriveChangedEvent()
    {
        Assert.Multiple(() =>
        {
            var drivesProperty = typeof(IStorageController).GetProperty(nameof(IStorageController.Drives));
            Assert.That(drivesProperty, Is.Not.Null);
            Assert.That(drivesProperty!.PropertyType, Is.EqualTo(typeof(IReadOnlyList<IStorageDrive>)));

            var metricsProperty = typeof(IStorageController).GetProperty(nameof(IStorageController.Metrics));
            Assert.That(metricsProperty, Is.Not.Null);
            Assert.That(metricsProperty!.PropertyType, Is.EqualTo(typeof(ControllerMetrics)));

            var driveChangedEvent = typeof(IStorageController).GetEvent(nameof(IStorageController.DriveChanged));
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
            Assert.That(createMethod!.ReturnType, Is.EqualTo(typeof(IStorageMedia)));
            Assert.That(createMethod.GetParameters(), Has.Length.EqualTo(1));
            Assert.That(createMethod.GetParameters()[0].ParameterType, Is.EqualTo(typeof(DiskImageCreationOptions)));

            var convertMethod = typeof(IDiskImageTooling).GetMethod(nameof(IDiskImageTooling.ConvertImage));
            Assert.That(convertMethod, Is.Not.Null);
            Assert.That(convertMethod!.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(convertMethod.GetParameters(), Has.Length.EqualTo(3));
            Assert.Multiple(() =>
            {
                Assert.That(convertMethod.GetParameters()[0].ParameterType, Is.EqualTo(typeof(IStorageMedia)));
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
            Assert.That(driveProperty!.PropertyType, Is.EqualTo(typeof(IStorageDrive)));
        });
    }
}