// <copyright file="IOPageDispatcherTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus.Tests;

/// <summary>
/// Unit tests for the <see cref="IOPageDispatcher"/> class.
/// </summary>
[TestFixture]
public class IOPageDispatcherTests
{
    /// <summary>
    /// Verifies that Read returns $FF for unregistered offset.
    /// </summary>
    [Test]
    public void Read_UnregisteredOffset_ReturnsFloatingBusValue()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();

        byte result = dispatcher.Read(0x00, in context);

        Assert.That(result, Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that Read invokes registered handler and returns its result.
    /// </summary>
    [Test]
    public void Read_RegisteredHandler_ReturnsHandlerResult()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        dispatcher.RegisterRead(0x00, (offset, in ctx) => 0x42);

        byte result = dispatcher.Read(0x00, in context);

        Assert.That(result, Is.EqualTo(0x42));
    }

    /// <summary>
    /// Verifies that Read passes correct offset to handler.
    /// </summary>
    [Test]
    public void Read_RegisteredHandler_PassesCorrectOffset()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        byte receivedOffset = 0;
        dispatcher.RegisterRead(0x30, (offset, in ctx) =>
        {
            receivedOffset = offset;
            return 0;
        });

        dispatcher.Read(0x30, in context);

        Assert.That(receivedOffset, Is.EqualTo(0x30));
    }

    /// <summary>
    /// Verifies that Write does nothing for unregistered offset.
    /// </summary>
    [Test]
    public void Write_UnregisteredOffset_DoesNotThrow()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();

        Assert.DoesNotThrow(() => dispatcher.Write(0x00, 0x42, in context));
    }

    /// <summary>
    /// Verifies that Write invokes registered handler.
    /// </summary>
    [Test]
    public void Write_RegisteredHandler_InvokesHandler()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        bool handlerCalled = false;
        dispatcher.RegisterWrite(0x10, (offset, value, in ctx) => handlerCalled = true);

        dispatcher.Write(0x10, 0x55, in context);

        Assert.That(handlerCalled, Is.True);
    }

    /// <summary>
    /// Verifies that Write passes correct offset and value to handler.
    /// </summary>
    [Test]
    public void Write_RegisteredHandler_PassesCorrectOffsetAndValue()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        byte receivedOffset = 0;
        byte receivedValue = 0;
        dispatcher.RegisterWrite(0x30, (offset, value, in ctx) =>
        {
            receivedOffset = offset;
            receivedValue = value;
        });

        dispatcher.Write(0x30, 0xAB, in context);

        Assert.Multiple(() =>
        {
            Assert.That(receivedOffset, Is.EqualTo(0x30));
            Assert.That(receivedValue, Is.EqualTo(0xAB));
        });
    }

    /// <summary>
    /// Verifies that RegisterRead throws ArgumentNullException for null handler.
    /// </summary>
    [Test]
    public void RegisterRead_NullHandler_ThrowsArgumentNullException()
    {
        var dispatcher = new IOPageDispatcher();

        Assert.Throws<ArgumentNullException>(() => dispatcher.RegisterRead(0x00, null!));
    }

    /// <summary>
    /// Verifies that RegisterWrite throws ArgumentNullException for null handler.
    /// </summary>
    [Test]
    public void RegisterWrite_NullHandler_ThrowsArgumentNullException()
    {
        var dispatcher = new IOPageDispatcher();

        Assert.Throws<ArgumentNullException>(() => dispatcher.RegisterWrite(0x00, null!));
    }

    /// <summary>
    /// Verifies that Register can set both read and write handlers.
    /// </summary>
    [Test]
    public void Register_BothHandlers_SetsReadAndWrite()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        bool readCalled = false;
        bool writeCalled = false;
        dispatcher.Register(
            0x10,
            (offset, in ctx) =>
            {
                readCalled = true;
                return 0x99;
            },
            (offset, value, in ctx) => writeCalled = true);

        byte readResult = dispatcher.Read(0x10, in context);
        dispatcher.Write(0x10, 0x00, in context);

        Assert.Multiple(() =>
        {
            Assert.That(readCalled, Is.True);
            Assert.That(writeCalled, Is.True);
            Assert.That(readResult, Is.EqualTo(0x99));
        });
    }

    /// <summary>
    /// Verifies that Register can set null handlers to clear registration.
    /// </summary>
    [Test]
    public void Register_NullHandlers_ClearsRegistration()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        dispatcher.RegisterRead(0x00, (offset, in ctx) => 0x42);
        dispatcher.RegisterWrite(0x00, (offset, value, in ctx) => { });

        dispatcher.Register(0x00, null, null);

        Assert.That(dispatcher.Read(0x00, in context), Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that multiple handlers can be registered at different offsets.
    /// </summary>
    [Test]
    public void Register_MultipleOffsets_AllHandlersWork()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        dispatcher.RegisterRead(0x00, (offset, in ctx) => 0x11);
        dispatcher.RegisterRead(0x10, (offset, in ctx) => 0x22);
        dispatcher.RegisterRead(0x30, (offset, in ctx) => 0x33);

        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.Read(0x00, in context), Is.EqualTo(0x11));
            Assert.That(dispatcher.Read(0x10, in context), Is.EqualTo(0x22));
            Assert.That(dispatcher.Read(0x30, in context), Is.EqualTo(0x33));
        });
    }

    /// <summary>
    /// Verifies that InstallSlotHandlers installs handlers at correct offsets for slot 0.
    /// </summary>
    [Test]
    public void InstallSlotHandlers_Slot0_InstallsAt0x80()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        var slotHandlers = new SlotIOHandlers();
        slotHandlers.Set(0x00, (offset, in ctx) => 0xA0, null);

        dispatcher.InstallSlotHandlers(0, slotHandlers);

        Assert.That(dispatcher.Read(0x80, in context), Is.EqualTo(0xA0));
    }

    /// <summary>
    /// Verifies that InstallSlotHandlers installs handlers at correct offsets for slot 6.
    /// </summary>
    [Test]
    public void InstallSlotHandlers_Slot6_InstallsAt0xE0()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        var slotHandlers = new SlotIOHandlers();
        slotHandlers.Set(0x00, (offset, in ctx) => 0x66, null);
        slotHandlers.Set(0x0F, (offset, in ctx) => 0x6F, null);

        dispatcher.InstallSlotHandlers(6, slotHandlers);

        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.Read(0xE0, in context), Is.EqualTo(0x66));
            Assert.That(dispatcher.Read(0xEF, in context), Is.EqualTo(0x6F));
        });
    }

    /// <summary>
    /// Verifies that InstallSlotHandlers installs all 16 handlers for a slot.
    /// </summary>
    [Test]
    public void InstallSlotHandlers_AllHandlers_InstalledCorrectly()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        var slotHandlers = new SlotIOHandlers();

        // Set handlers at all 16 offsets
        // Note: Handler receives the full dispatcher offset (0xD0-0xDF for slot 5)
        for (byte i = 0; i < 16; i++)
        {
            // Handler returns a value based on slot offset (0-15)
            byte slotOffset = i;
            slotHandlers.Set(i, (offset, in ctx) => (byte)(0x50 + slotOffset), null);
        }

        dispatcher.InstallSlotHandlers(5, slotHandlers);

        // Slot 5 is at offset 0xD0
        Assert.Multiple(() =>
        {
            for (byte i = 0; i < 16; i++)
            {
                byte result = dispatcher.Read((byte)(0xD0 + i), in context);
                int expectedValue = 0x50 + i;
                Assert.That(result, Is.EqualTo(expectedValue), $"Offset 0x{0xD0 + i:X2}");
            }
        });
    }

    /// <summary>
    /// Verifies that InstallSlotHandlers throws for invalid slot number (negative).
    /// </summary>
    [Test]
    public void InstallSlotHandlers_NegativeSlot_ThrowsArgumentOutOfRangeException()
    {
        var dispatcher = new IOPageDispatcher();
        var slotHandlers = new SlotIOHandlers();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            dispatcher.InstallSlotHandlers(-1, slotHandlers));
    }

    /// <summary>
    /// Verifies that InstallSlotHandlers throws for invalid slot number (too high).
    /// </summary>
    [Test]
    public void InstallSlotHandlers_Slot8_ThrowsArgumentOutOfRangeException()
    {
        var dispatcher = new IOPageDispatcher();
        var slotHandlers = new SlotIOHandlers();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            dispatcher.InstallSlotHandlers(8, slotHandlers));
    }

    /// <summary>
    /// Verifies that InstallSlotHandlers throws for null handlers.
    /// </summary>
    [Test]
    public void InstallSlotHandlers_NullHandlers_ThrowsArgumentNullException()
    {
        var dispatcher = new IOPageDispatcher();

        Assert.Throws<ArgumentNullException>(() =>
            dispatcher.InstallSlotHandlers(0, null!));
    }

    /// <summary>
    /// Verifies that RemoveSlotHandlers clears all handlers for a slot.
    /// </summary>
    [Test]
    public void RemoveSlotHandlers_ClearsSlotHandlers()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        var slotHandlers = new SlotIOHandlers();
        slotHandlers.Set(0x00, (offset, in ctx) => 0x77, null);
        dispatcher.InstallSlotHandlers(7, slotHandlers);

        // Verify handler is installed
        Assert.That(dispatcher.Read(0xF0, in context), Is.EqualTo(0x77));

        dispatcher.RemoveSlotHandlers(7);

        // Verify handler is removed (returns floating bus value)
        Assert.That(dispatcher.Read(0xF0, in context), Is.EqualTo(0xFF));
    }

    /// <summary>
    /// Verifies that RemoveSlotHandlers clears write handlers too.
    /// </summary>
    [Test]
    public void RemoveSlotHandlers_ClearsWriteHandlers()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        bool writeCalled = false;
        var slotHandlers = new SlotIOHandlers();
        slotHandlers.Set(0x00, null, (offset, value, in ctx) => writeCalled = true);
        dispatcher.InstallSlotHandlers(1, slotHandlers);

        dispatcher.Write(0x90, 0x00, in context);
        Assert.That(writeCalled, Is.True);

        writeCalled = false;
        dispatcher.RemoveSlotHandlers(1);

        dispatcher.Write(0x90, 0x00, in context);
        Assert.That(writeCalled, Is.False);
    }

    /// <summary>
    /// Verifies that RemoveSlotHandlers throws for invalid slot number.
    /// </summary>
    [Test]
    public void RemoveSlotHandlers_InvalidSlot_ThrowsArgumentOutOfRangeException()
    {
        var dispatcher = new IOPageDispatcher();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            dispatcher.RemoveSlotHandlers(8));
    }

    /// <summary>
    /// Verifies that slot handlers do not affect non-slot region handlers.
    /// </summary>
    [Test]
    public void InstallSlotHandlers_DoesNotAffectNonSlotRegion()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();
        dispatcher.RegisterRead(0x00, (offset, in ctx) => 0x11); // Keyboard
        dispatcher.RegisterRead(0x30, (offset, in ctx) => 0x33); // Speaker

        var slotHandlers = new SlotIOHandlers();
        slotHandlers.Set(0x00, (offset, in ctx) => 0x66, null);
        dispatcher.InstallSlotHandlers(6, slotHandlers);

        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.Read(0x00, in context), Is.EqualTo(0x11));
            Assert.That(dispatcher.Read(0x30, in context), Is.EqualTo(0x33));
            Assert.That(dispatcher.Read(0xE0, in context), Is.EqualTo(0x66));
        });
    }

    /// <summary>
    /// Verifies that handlers at all slot boundaries work correctly.
    /// </summary>
    [Test]
    public void InstallSlotHandlers_AllSlots_CorrectOffsets()
    {
        var dispatcher = new IOPageDispatcher();
        var context = CreateTestContext();

        // Install handlers for all slots
        for (int slot = 0; slot < 8; slot++)
        {
            var slotHandlers = new SlotIOHandlers();
            byte slotValue = (byte)(0x80 + (slot * 0x10));
            slotHandlers.Set(0x00, (offset, in ctx) => slotValue, null);
            dispatcher.InstallSlotHandlers(slot, slotHandlers);
        }

        // Verify each slot's first handler
        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.Read(0x80, in context), Is.EqualTo(0x80), "Slot 0");
            Assert.That(dispatcher.Read(0x90, in context), Is.EqualTo(0x90), "Slot 1");
            Assert.That(dispatcher.Read(0xA0, in context), Is.EqualTo(0xA0), "Slot 2");
            Assert.That(dispatcher.Read(0xB0, in context), Is.EqualTo(0xB0), "Slot 3");
            Assert.That(dispatcher.Read(0xC0, in context), Is.EqualTo(0xC0), "Slot 4");
            Assert.That(dispatcher.Read(0xD0, in context), Is.EqualTo(0xD0), "Slot 5");
            Assert.That(dispatcher.Read(0xE0, in context), Is.EqualTo(0xE0), "Slot 6");
            Assert.That(dispatcher.Read(0xF0, in context), Is.EqualTo(0xF0), "Slot 7");
        });
    }

    /// <summary>
    /// Helper method to create a test bus access context.
    /// </summary>
    private static BusAccess CreateTestContext()
    {
        return new(
            Address: 0xC000,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataRead,
            SourceId: 0,
            Cycle: 0,
            Flags: AccessFlags.None);
    }
}