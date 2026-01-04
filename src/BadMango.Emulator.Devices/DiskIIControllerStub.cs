// <copyright file="DiskIIControllerStub.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Disk II controller stub - demonstrates the peripheral card pattern.
/// </summary>
/// <remarks>
/// <para>
/// This is a minimal implementation for testing slot infrastructure.
/// Full disk emulation is a separate issue.
/// </para>
/// <para>
/// The Disk II controller uses 16 soft switches for disk operations:
/// </para>
/// <list type="bullet">
/// <item><description>$C0n0-$C0n7: Stepper motor phase control (0-3 off/on)</description></item>
/// <item><description>$C0n8-$C0n9: Motor off/on</description></item>
/// <item><description>$C0nA-$C0nB: Drive 1/2 select</description></item>
/// <item><description>$C0nC-$C0nF: Q6/Q7 data latch control</description></item>
/// </list>
/// </remarks>
public sealed class DiskIIControllerStub : ISlotCard
{
    private readonly SlotIOHandlers handlers = new();
    private readonly IBusTarget romRegion;
    private readonly IBusTarget expansionRomRegion;

    // Internal state
    private int currentPhase;
    private bool motorOn;
    private int selectedDrive = 1;
    private bool q6High;
    private bool q7High;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskIIControllerStub"/> class.
    /// </summary>
    public DiskIIControllerStub()
    {
        // Set up all 16 I/O handlers
        handlers.Set(0x00, PhaseRead, PhaseWrite);   // Phase 0 off
        handlers.Set(0x01, PhaseRead, PhaseWrite);   // Phase 0 on
        handlers.Set(0x02, PhaseRead, PhaseWrite);   // Phase 1 off
        handlers.Set(0x03, PhaseRead, PhaseWrite);   // Phase 1 on
        handlers.Set(0x04, PhaseRead, PhaseWrite);   // Phase 2 off
        handlers.Set(0x05, PhaseRead, PhaseWrite);   // Phase 2 on
        handlers.Set(0x06, PhaseRead, PhaseWrite);   // Phase 3 off
        handlers.Set(0x07, PhaseRead, PhaseWrite);   // Phase 3 on
        handlers.Set(0x08, MotorOffRead, null);      // Motor off
        handlers.Set(0x09, MotorOnRead, null);       // Motor on
        handlers.Set(0x0A, SelectDrive1, null);      // Select drive 1
        handlers.Set(0x0B, SelectDrive2, null);      // Select drive 2
        handlers.Set(0x0C, Q6LRead, null);           // Q6L
        handlers.Set(0x0D, Q6HRead, null);           // Q6H
        handlers.Set(0x0E, Q7LRead, null);           // Q7L
        handlers.Set(0x0F, Q7HRead, Q7HWrite);       // Q7H

        // Create stub ROMs
        romRegion = new DiskIIBootRomStub();
        expansionRomRegion = new DiskIIExpansionRomStub();
    }

    // ─── IPeripheral ────────────────────────────────────────────────────

    /// <inheritdoc />
    public string Name => "Disk II Controller (Stub)";

    /// <inheritdoc />
    public string DeviceType => "DiskII";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.SlotCard;

    // ─── ISlotCard ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public int SlotNumber { get; set; }

    /// <inheritdoc />
    public SlotIOHandlers? IOHandlers => handlers;

    /// <inheritdoc />
    public IBusTarget? ROMRegion => romRegion;

    /// <inheritdoc />
    public IBusTarget? ExpansionROMRegion => expansionRomRegion;

    // ─── State Properties (for testing) ─────────────────────────────────

    /// <summary>
    /// Gets the current stepper motor phase.
    /// </summary>
    /// <value>The current phase (0-3).</value>
    public int CurrentPhase => currentPhase;

    /// <summary>
    /// Gets a value indicating whether the motor is on.
    /// </summary>
    /// <value><see langword="true"/> if the motor is running; otherwise, <see langword="false"/>.</value>
    public bool IsMotorOn => motorOn;

    /// <summary>
    /// Gets the currently selected drive number.
    /// </summary>
    /// <value>1 for drive 1, 2 for drive 2.</value>
    public int SelectedDrive => selectedDrive;

    /// <summary>
    /// Gets a value indicating whether Q6 is high.
    /// </summary>
    /// <value><see langword="true"/> if Q6 is high; otherwise, <see langword="false"/>.</value>
    public bool IsQ6High => q6High;

    /// <summary>
    /// Gets a value indicating whether Q7 is high.
    /// </summary>
    /// <value><see langword="true"/> if Q7 is high; otherwise, <see langword="false"/>.</value>
    public bool IsQ7High => q7High;

    // ─── IScheduledDevice ───────────────────────────────────────────────

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        // Disk II stub doesn't need scheduler access
    }

    /// <inheritdoc />
    public void OnExpansionROMSelected()
    {
        // Nothing to do in stub
    }

    /// <inheritdoc />
    public void OnExpansionROMDeselected()
    {
        // Nothing to do in stub
    }

    /// <inheritdoc />
    public void Reset()
    {
        currentPhase = 0;
        motorOn = false;
        selectedDrive = 1;
        q6High = false;
        q7High = false;
    }

    private byte PhaseRead(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // Get the relative offset within the slot (0-15)
            byte relativeOffset = (byte)(offset & 0x0F);

            // Update phase based on offset
            // Even offsets turn phase off, odd offsets turn phase on
            int phase = relativeOffset >> 1;
            bool turnOn = (relativeOffset & 1) != 0;

            if (turnOn)
            {
                currentPhase = phase;
            }
        }

        return 0xFF;
    }

    private void PhaseWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            // Get the relative offset within the slot (0-15)
            byte relativeOffset = (byte)(offset & 0x0F);

            // Same logic as read - Disk II responds to both read and write
            int phase = relativeOffset >> 1;
            bool turnOn = (relativeOffset & 1) != 0;

            if (turnOn)
            {
                currentPhase = phase;
            }
        }
    }

    private byte MotorOffRead(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            motorOn = false;
        }

        return 0xFF;
    }

    private byte MotorOnRead(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            motorOn = true;
        }

        return 0xFF;
    }

    private byte SelectDrive1(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            selectedDrive = 1;
        }

        return 0xFF;
    }

    private byte SelectDrive2(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            selectedDrive = 2;
        }

        return 0xFF;
    }

    private byte Q6LRead(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q6High = false;
        }

        // Q6L with Q7L = read data; returns disk data byte
        // In stub, just return 0xFF (no disk)
        return 0xFF;
    }

    private byte Q6HRead(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q6High = true;
        }

        return 0xFF;
    }

    private byte Q7LRead(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q7High = false;
        }

        return 0xFF;
    }

    private byte Q7HRead(byte offset, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q7High = true;
        }

        return 0xFF;
    }

    private void Q7HWrite(byte offset, byte value, in BusAccess ctx)
    {
        if (!ctx.IsSideEffectFree)
        {
            q7High = true;

            // Q6L + Q7H write = write data byte
            // In stub, we ignore the write
        }
    }
}