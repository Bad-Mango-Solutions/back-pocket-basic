// <copyright file="MachineVideoFrameProducerTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Rendering.Tests;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Devices.Interfaces;

using Moq;

using EmulatorKeyboardDevice = BadMango.Emulator.Devices.Interfaces.IKeyboardDevice;

/// <summary>
/// Tests for <see cref="MachineVideoFrameProducer"/>.
/// </summary>
[TestFixture]
public class MachineVideoFrameProducerTests
{
    /// <summary>
    /// Verifies VBlank production reads main display memory from physical RAM when available.
    /// </summary>
    [Test]
    public void VBlank_WithPhysicalMainRam_UsesPhysicalRamInsteadOfBus()
    {
        var producer = new MachineVideoFrameProducer();
        var machine = CreateMachine(
            out var mainMemory,
            out var mockVideoDevice,
            out var mockBus);
        mainMemory.AsSpan()[0x0400] = 0xC1;

        producer.AttachMachine(machine.Object);
        mockVideoDevice.Raise(video => video.VBlankOccurred += null);

        var destination = new uint[producer.Width * producer.Height];
        var copied = producer.TryCopyLatestFrame(destination, out _);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.True);
            mockBus.Verify(bus => bus.Read8(It.IsAny<BusAccess>()), Times.Never);
        });
    }

    /// <summary>
    /// Verifies mode and PAGE2 are captured with the produced frame, not at consume time.
    /// </summary>
    [Test]
    public void VBlank_CapturesModeAndPageAtProductionTime()
    {
        var producer = new MachineVideoFrameProducer();
        var machine = CreateMachine(
            out _,
            out var mockVideoDevice,
            out _);
        var mode = VideoMode.HiRes;
        var isPage2 = false;
        mockVideoDevice.Setup(video => video.CurrentMode).Returns(() => mode);
        mockVideoDevice.Setup(video => video.IsPage2).Returns(() => isPage2);

        producer.AttachMachine(machine.Object);
        mockVideoDevice.Raise(video => video.VBlankOccurred += null);

        mode = VideoMode.Text40;
        isPage2 = true;

        var destination = new uint[producer.Width * producer.Height];
        var copied = producer.TryCopyLatestFrame(destination, out var metadata);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.True);
            Assert.That(metadata.Mode, Is.EqualTo(VideoMode.HiRes));
            Assert.That(metadata.IsPage2, Is.False);
        });
    }

    /// <summary>
    /// Verifies all frame consumers are notified when a VBlank frame is produced.
    /// </summary>
    [Test]
    public void FrameReady_NotifiesAllSubscribers()
    {
        var producer = new MachineVideoFrameProducer();
        var machine = CreateMachine(
            out _,
            out var mockVideoDevice,
            out _);
        var subscriber1 = 0;
        var subscriber2 = 0;
        producer.FrameReady += () => subscriber1++;
        producer.FrameReady += () => subscriber2++;

        producer.AttachMachine(machine.Object);
        mockVideoDevice.Raise(video => video.VBlankOccurred += null);

        Assert.Multiple(() =>
        {
            Assert.That(subscriber1, Is.EqualTo(1));
            Assert.That(subscriber2, Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies forced renders can refresh the latest frame without dispatching VBlank notifications.
    /// </summary>
    [Test]
    public void ForceRender_ProducesFrameWithoutFrameReadyEvent()
    {
        var producer = new MachineVideoFrameProducer();
        var machine = CreateMachine(
            out _,
            out _,
            out _);
        var notifications = 0;
        producer.FrameReady += () => notifications++;

        producer.AttachMachine(machine.Object);
        producer.ForceRender();

        var destination = new uint[producer.Width * producer.Height];
        var copied = producer.TryCopyLatestFrame(destination, out _);
        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.True);
            Assert.That(notifications, Is.EqualTo(0));
        });
    }

    private static Mock<IMachine> CreateMachine(
        out PhysicalMemory mainMemory,
        out Mock<IVideoDevice> mockVideoDevice,
        out Mock<IMemoryBus> mockBus)
    {
        mainMemory = new PhysicalMemory(65536, "Main");
        var mainMemoryProvider = new Mock<IMainMemoryProvider>();
        mainMemoryProvider.Setup(provider => provider.MainRam).Returns(mainMemory.Memory);

        mockVideoDevice = new Mock<IVideoDevice>();
        mockVideoDevice.Setup(video => video.CurrentMode).Returns(VideoMode.Text40);
        mockVideoDevice.Setup(video => video.IsPage2).Returns(false);

        mockBus = new Mock<IMemoryBus>();
        mockBus.Setup(bus => bus.Read8(It.IsAny<BusAccess>())).Returns(0xFF);

        var machine = new Mock<IMachine>();
        machine.Setup(m => m.Bus).Returns(mockBus.Object);
        machine.Setup(m => m.GetComponent<IVideoDevice>()).Returns(mockVideoDevice.Object);
        machine.Setup(m => m.GetComponent<IMainMemoryProvider>()).Returns(mainMemoryProvider.Object);
        machine.Setup(m => m.GetComponent<IExtended80ColumnDevice>()).Returns((IExtended80ColumnDevice?)null);
        machine.Setup(m => m.GetComponent<ICharacterDevice>()).Returns((ICharacterDevice?)null);
        machine.Setup(m => m.GetComponent<ICharacterRomProvider>()).Returns((ICharacterRomProvider?)null);
        machine.Setup(m => m.GetComponent<EmulatorKeyboardDevice>()).Returns((EmulatorKeyboardDevice?)null);
        return machine;
    }
}