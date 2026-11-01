// <copyright file="StartupSlotBootAssist.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Configuration;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Debug.Infrastructure.Commands;
using BadMango.Emulator.Devices;
using BadMango.Emulator.Storage.Media;

/// <summary>
/// Applies profile <c>boot.startupSlot</c> by jumping directly to a slot boot ROM when drive 1 has media.
/// </summary>
internal static class StartupSlotBootAssist
{
    /// <summary>
    /// After <see cref="IMachine.Reset"/>, prepares a direct boot from the configured startup slot.
    /// </summary>
    /// <param name="debugContext">The active debug context.</param>
    /// <param name="profile">The machine profile (optional).</param>
    /// <returns><see langword="true"/> when PC was set to <c>$Cn00</c> for the startup slot.</returns>
    public static bool TryApplyDirectBoot(IDebugContext debugContext, MachineProfile? profile)
    {
        int? startupSlot = profile?.Boot?.StartupSlot;
        if (startupSlot is not (>= 1 and <= 7))
        {
            return false;
        }

        if (debugContext.Machine is null || debugContext.Bus is null || debugContext.Cpu is null)
        {
            return false;
        }

        var slotManager = debugContext.Machine.GetComponent<ISlotManager>();
        if (slotManager?.GetCard(startupSlot.Value) is not IDiskController disk)
        {
            return false;
        }

        if (!disk.GetDriveSnapshot(0).HasMedia)
        {
            return false;
        }

        int slot = startupSlot.Value;
        uint bootEntry = 0xC000 + (uint)(slot << 8);
        var bus = debugContext.Bus;
        var cycle = debugContext.Machine.Scheduler.Now;

        slotManager.SetInternalCxRom(false);
        slotManager.DeselectExpansionSlot();

        var busAccess = new BusAccess(
            Address: 0,
            Value: 0,
            WidthBits: 8,
            Mode: BusAccessMode.Decomposed,
            EmulationFlag: true,
            Intent: AccessIntent.DataWrite,
            SourceId: 0,
            Cycle: cycle,
            Flags: AccessFlags.None);
        busAccess = busAccess with { Address = 0xCFFF };
        bus.Read8(in busAccess);
        busAccess = busAccess with { Address = 0xC006 };
        bus.Write8(in busAccess, 0);
        busAccess = busAccess with { Address = bootEntry };
        bus.Read8(in busAccess);
        slotManager.SelectExpansionSlot(slot);

        if (slotManager.GetCard(slot) is DiskIIController diskII)
        {
            diskII.Reset();
        }

        busAccess = busAccess with { Intent = AccessIntent.DataWrite, Flags = AccessFlags.None };
        bus.Write8(busAccess with { Address = 0x03F0 }, (byte)(bootEntry & 0xFF));
        bus.Write8(busAccess with { Address = 0x03F1 }, (byte)((bootEntry >> 8) & 0xFF));
        bus.Write8(busAccess with { Address = 0x003C }, 0);
        bus.Write8(busAccess with { Address = 0x003D }, 0);

        ref var regs = ref debugContext.Cpu.Registers;
        regs.PC.SetAddr(bootEntry);
        debugContext.Cpu.HaltReason = HaltState.None;

        return true;
    }
}