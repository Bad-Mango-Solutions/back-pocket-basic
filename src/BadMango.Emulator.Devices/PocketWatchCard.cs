// <copyright file="PocketWatchCard.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using System.IO;
using System.Text.Json;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Interfaces.Cpu; // ICpu for trap handler sig
// Trap types (TrapHandler, TrapResult, TrapCategory, TrapReturnMethod) are in BadMango.Emulator.Bus


using BadMango.Emulator.Core.Configuration;

using Interfaces;

/// <summary>
/// PocketWatch slot card - a Thunderclock-compatible real-time clock.
/// </summary>
/// <remarks>
/// <para>
/// The PocketWatch card provides ProDOS-compatible time services to the emulated system.
/// It is compatible with the Thunderclock Plus protocol, allowing existing software
/// to access the current date and time.
/// </para>
/// <para>
/// The card supports multiple time sources:
/// <list type="bullet">
/// <item><description>"host" - Uses the host system's clock with timezone offset</description></item>
/// <item><description>"frozen" - Uses a static datetime from configuration</description></item>
/// <item><description>"ntp" - Uses NTP server polling (not yet implemented)</description></item>
/// </list>
/// </para>
/// </remarks>
[DeviceType("pocketwatch")]
public sealed class PocketWatchCard : IClockDevice
{
    /// <summary>
    /// The default frozen time when no valid frozen time is specified.
    /// </summary>
    private static readonly DateTime DefaultFrozenTime = new(1986, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly SlotIOHandlers handlers = new();
    private BadMango.Emulator.Bus.Interfaces.IBusTarget romRegion;
    private BadMango.Emulator.Bus.Interfaces.IBusTarget? expansionRomRegion;
    private PhysicalMemory? realRomPhysical;   // keep backing for real ROM case
    private PhysicalMemory? realExpPhysical;
    private readonly PocketWatchConfig config;
    private readonly byte[] timeData = new byte[8];
    private DateTime fixedTime;
    private bool useHostTime = true;
    private int timezoneOffsetMinutes;

    // Watch state
    private int readIndex;

    // uPD1990AC serial clock chip emulation state (for real Thunderclock ROM firmware)
    private byte control;
    private ulong shiftReg;
    private bool dataOutBit;
    private byte lastControl;
    private bool irqFlag;

    // For deferred trap registration (SlotNumber may be set after or before Initialize)
    private ITrapRegistry? storedTrapRegistry;
    private bool trapsRegistered;

    private bool usingRealThunderclockRom;

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketWatchCard"/> class with default configuration.
    /// </summary>
    public PocketWatchCard()
        : this(new PocketWatchConfig())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketWatchCard"/> class with specified configuration.
    /// </summary>
    /// <param name="config">The PocketWatch configuration.</param>
    public PocketWatchCard(PocketWatchConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        this.config = config;

        InitializeCommon();
    }

    /// <summary>
    /// Creates a <see cref="PocketWatchCard"/> from the optional per-card configuration in a
    /// machine profile (e.g. <c>{ "rom": "thunderclock-plus" }</c>). The actual ROM bytes are
    /// supplied later via <c>LoadBootRom</c> when the profile's <c>rom</c> entry references a
    /// declared rom-image.
    /// </summary>
    /// <param name="cardConfig">The per-card JSON configuration from the profile, if any.</param>
    /// <returns>A configured <see cref="PocketWatchCard"/> instance.</returns>
    public static PocketWatchCard CreateFromProfileConfig(JsonElement? cardConfig = null)
    {
        var card = new PocketWatchCard();
        card._cardConfig = cardConfig;
        return card;
    }

    private JsonElement? _cardConfig;

    private void InitializeCommon()
    {
        // Configure time source
        ConfigureTimeSource();

        // Set up I/O handlers (Thunderclock protocol)
        handlers.Set(0x00, ReadClockData, WriteClockControl);
        handlers.Set(0x01, ReadClockStatus, null);

        // ProDOS only needs Thunderclock ID bytes at $Cn00/$02/$04/$06 and trapped
        // $Cn08/$Cn0B entries. Real firmware runs into C8 expansion tails and stalls boot;
        // trap handlers write the time string at $0200 directly (see RegisterTrapsIfNeeded).
        romRegion = new PocketWatchRom();
        expansionRomRegion = null;
        usingRealThunderclockRom = false;
        System.Console.Error.WriteLine("[POCKET] PocketWatchCard: trap+stub Cn ROM (ProDOS-safe)");
    }

    // ─── IPeripheral ────────────────────────────────────────────────────

    /// <inheritdoc />
    public string Name => "PocketWatch";

    /// <inheritdoc />
    public string DeviceType => "PocketWatch";

    /// <inheritdoc />
    public PeripheralKind Kind => PeripheralKind.SlotCard;

    // ─── ISlotCard ──────────────────────────────────────────────────────

    /// <inheritdoc />
    public int SlotNumber { get; set; }

    /// <inheritdoc />
    public SlotIOHandlers? IOHandlers => handlers;

    /// <inheritdoc />
    public BadMango.Emulator.Bus.Interfaces.IBusTarget? ROMRegion => romRegion;

    /// <inheritdoc />
    public BadMango.Emulator.Bus.Interfaces.IBusTarget? ExpansionROMRegion => expansionRomRegion;

    /// <summary>
    /// Loads firmware bytes for the real Thunderclock ROM (2KB image).
    /// Invoked automatically by profile processing (MachineBuilder.FromProfile) when the
    /// card config in the profile contains "rom": "&lt;name&gt;" referencing a declared
    /// entry in memory.rom-images. This mirrors how disk-ii cards load their P5/P6 ROMs
    /// (see slot 6 in pocket2e-a2-enh.json).
    /// </summary>
    public void LoadBootRom(byte[] firmwareBytes)
    {
        ArgumentNullException.ThrowIfNull(firmwareBytes);

        // Profile may supply a Thunderclock image for verification; execution uses stub+traps.
        romRegion = new PocketWatchRom();
        expansionRomRegion = null;
        realRomPhysical = null;
        realExpPhysical = null;
        usingRealThunderclockRom = false;
        System.Console.Error.WriteLine(
            $"[POCKET] LoadBootRom: trap+stub Cn ROM ({firmwareBytes.Length} byte image not executed)");
    }

    // ─── IClockDevice ───────────────────────────────────────────────────

    /// <inheritdoc />
    public DateTime CurrentTime
    {
        get
        {
            if (useHostTime)
            {
                return DateTimeOffset.UtcNow.AddMinutes(timezoneOffsetMinutes).DateTime;
            }

            return fixedTime;
        }
    }

    /// <inheritdoc />
    public bool UseHostTime
    {
        get => useHostTime;
        set => useHostTime = value;
    }

    /// <inheritdoc />
    public void SetFixedTime(DateTime time)
    {
        fixedTime = time;
        useHostTime = false;
    }

    // ─── IScheduledDevice ───────────────────────────────────────────────

    /// <inheritdoc />
    public void Initialize(IEventContext context)
    {
        // PocketWatch doesn't need scheduler access for I/O, but register trapped execution
        // for its ROM area so that when PC reaches trapped addresses in the slot ROM
        // (e.g. during ProDOS/Thunderclock driver calls or reset scan), C# portal code
        // executes to provide host clock. This makes "trapped execution" work for the device.

        storedTrapRegistry = context.GetComponent<ITrapRegistry>();
        // For slot 4 (Thunderclock in standard profile). Registration uses the configured slot.
        if (SlotNumber == 0) SlotNumber = 4;
        RegisterTrapsIfNeeded();
    }

    private void RegisterTrapsIfNeeded()
    {
        if (trapsRegistered || storedTrapRegistry == null)
            return;
        int slot = SlotNumber > 0 ? SlotNumber : 4;
        if (slot < 1 || slot > 7)
            return;

        System.Console.Error.WriteLine($"[POCKET INIT] registering traps for slot {slot}");
        var trapRegistry = storedTrapRegistry;
        Addr baseAddr = (Addr)(0xC000 + (slot * 0x100));

        // Per https://prodos8.com/docs/techref/adding-routines-to-prodos :
        // - ProDOS recognizes via signature $Cn00=$08, $Cn02=$28, $Cn04=$58, $Cn06=$70.
        // - Cn08 = READ entry (card firmware deposits "mo,da,dt,hr,mn" at $0200).
        // - Cn0B = WRITE entry; ProDOS does LDA #$A3 ; JSR Cn0B then JSR Cn08.
        // Trap both entries for stub and real ROM so ProDOS gets a time string without
        // running Thunderclock firmware that JSRs into C800 and can WAI-stall autostart.

        // Slot-dependent traps so TrapRegistry can suppress Cnxx handlers while INTCXROM
        // is active, except $Cn08 (READ) which 80-col glue still reaches via fall-through.
        Addr readEntry = baseAddr + 0x08;
        trapRegistry.RegisterSlotDependent(readEntry, slot, $"PocketWatch ROM ${readEntry:X4} (read)", TrapCategory.SlotFirmware, (cpu, bus, evt) =>
            CompleteClockReadTrap(cpu), "Thunderclock READ entry (Cn08): ProDOS expects ASCII mo,da,dt,hr,mn at $0200");

        Addr writeEntry = baseAddr + 0x0B;
        trapRegistry.RegisterSlotDependent(writeEntry, slot, $"PocketWatch ROM ${writeEntry:X4} (write)", TrapCategory.SlotFirmware, (cpu, bus, evt) => {
            LatchTime();
            return TrapResult.Success(20);
        }, "Thunderclock WRITE entry (Cn0B): latch for mode A3 and RTS");

        if (usingRealThunderclockRom)
        {
            // Real firmware: LDA $CFFF (deselect) then JSR $C81A at $Cn17 (slot 4: $C417).
            // After CFFF the active ROM is the motherboard default (80-column), not this card's
            // C8 image — that call can WAI and stall IIe autostart.
            Addr jsrC81a = baseAddr + 0x17;
            trapRegistry.RegisterSlotDependent(
                jsrC81a,
                slot,
                $"PocketWatch ROM ${jsrC81a:X4} (skip JSR C81A)",
                TrapCategory.SlotFirmware,
                (cpu, bus, evt) =>
                {
                    cpu.Registers.PC.Advance(3);
                    return new TrapResult { Handled = true, ReturnMethod = TrapReturnMethod.None, CyclesConsumed = 6 };
                },
                "Skip JSR $C81A after CFFF deselect during monitor slot scan.");
        }
        // Stub ROM only: skip data bytes at $CnFB-$CnFF that are not safe entry points.
        if (!usingRealThunderclockRom)
        {
            int[] rtsOffsets = { 0xFB, 0xFC, 0xFD, 0xFE, 0xFF };
            foreach (int i in rtsOffsets)
            {
                Addr addr = baseAddr + (uint)i;
                trapRegistry.Register(addr, $"PocketWatch ROM ${addr:X4} (rts-skip)", TrapCategory.SlotFirmware, (cpu, bus, evt) => {
                    LatchTime();
                    cpu.Registers.PC.Advance(1);
                    return new TrapResult { Handled = true, ReturnMethod = TrapReturnMethod.None, CyclesConsumed = 2 };
                }, "Skip data at end of stub to avoid corruption on JMP-to-ROM");
            }
        }

        trapsRegistered = true;
    }

    /// <summary>
    /// Patches the Cn ROM page so IIe autostart can scan the slot without stalling in
    /// expansion ROM or treating the card as bootable.
    /// </summary>
    /// <param name="slotPage">The 256-byte Cnxx ROM page buffer.</param>
    private static void ApplyAutostartSafeSlotRomPatches(byte[] slotPage)
    {
        // Not an autoboot device: $CnFF must be $00.
        slotPage[0xFF] = 0x00;

        // Firmware does LDA $CFFF / JSR $C81A; after CFFF the JSR lands in motherboard
        // expansion ROM (80-column), which can WAI-stall autostart. NOP out the JSR.
        NopAbsoluteJump(slotPage, 0x17);

        // Command dispatch in the Cn page JMPs into C9xx expansion ROM (e.g. $C4C7 → $C92F),
        // which can WAI-stall IIe autostart before $FA56. ProDOS uses trapped $Cn08/$Cn0B.
        NopAbsoluteJump(slotPage, 0x4D);
        NopAbsoluteJump(slotPage, 0xC7);
        NopAbsoluteJump(slotPage, 0xD9);
    }

    /// <summary>
    /// Replaces unused <c>$FF</c> expansion-ROM padding with <c>RTS</c> so accidental
    /// dispatch into the tail (common on Thunderclock images) returns instead of spinning.
    /// </summary>
    private static void SanitizeExpansionRomPadding(byte[] expansionPage)
    {
        // Thunderclock images often leave $FF tails. Only rewrite padding runs (2+ adjacent
        // $FF bytes) so operands like $FF58 in BIT $FF58 stay intact.
        for (int i = 0; i < expansionPage.Length; i++)
        {
            if (expansionPage[i] != 0xFF)
            {
                continue;
            }

            bool prevFf = i > 0 && expansionPage[i - 1] == 0xFF;
            bool nextFf = i < expansionPage.Length - 1 && expansionPage[i + 1] == 0xFF;
            if (prevFf || nextFf)
            {
                expansionPage[i] = 0x60;
            }
        }
    }

    /// <summary>NOPs a 3-byte <c>JMP abs</c> if it targets the C8xx/C9xx expansion ROM page.</summary>
    private static void NopAbsoluteJump(byte[] slotPage, int offset)
    {
        if (slotPage.Length > offset + 2 &&
            slotPage[offset] == 0x4C &&
            slotPage[offset + 2] is 0xC8 or 0xC9)
        {
            slotPage[offset] = 0xEA;
            slotPage[offset + 1] = 0xEA;
            slotPage[offset + 2] = 0xEA;
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        readIndex = 0;
        control = 0;
        shiftReg = 0;
        dataOutBit = false;
        lastControl = 0;
        irqFlag = false;
    }

    /// <inheritdoc />
    public void OnExpansionROMSelected()
    {
        // Expansion ROM (C800+) now visible for this slot. Real Thunderclock firmware uses C8 space
        // (C81A etc) after the Cnxx stub does LDA $CFFF + JSR $C81A (CFFF deselects prior owner; Cn access + select re-owns).
    }

    /// <inheritdoc />
    public void OnExpansionROMDeselected()
    {
        // C800 space released (on $CFFF access typically). Other cards (e.g. DiskII) may now use shared C8 ROM space.
    }

    private void ConfigureTimeSource()
    {
        timezoneOffsetMinutes = config.TimezoneOffset;

        switch (config.TimeSource.ToLowerInvariant())
        {
            case "host":
                useHostTime = true;
                break;

            case "frozen":
                useHostTime = false;
                fixedTime = !string.IsNullOrEmpty(config.FrozenTime) &&
                    DateTime.TryParse(config.FrozenTime, out var parsedTime)
                        ? parsedTime
                        : DefaultFrozenTime;
                break;

            case "ntp":
                // NTP support is a future enhancement
                // For now, fall back to host time
                useHostTime = true;
                break;

            default:
                useHostTime = true;
                break;
        }
    }

    private byte ReadClockData(byte offset, in BusAccess context)
    {
        // For real Thunderclock ROM (uPD1990AC protocol):
        // Returns DATA OUT in bit 0, IRQ flag in bit 7.
        // The firmware at C408/C40B (and C8xx subs) drives COMMAND/SHIFT/STROBE/DATA via writes to C0n0,
        // and clocks bits out by toggling SHIFT CLOCK (bit 1) and reading DATA OUT.
        // We emulate the shift register here; higher offsets mirror for compatibility.
        if (offset == 0)
        {
            byte val = (byte)((dataOutBit ? 1 : 0) | (irqFlag ? 0x80 : 0));
            return val;
        }

        // Mirror C0n1+ to main data reg (as described in hardware docs for this card)
        return (byte)((dataOutBit ? 1 : 0) | (irqFlag ? 0x80 : 0));
    }

    private byte ReadClockStatus(byte offset, in BusAccess context)
    {
        // Legacy/status: bit 7 often used as "ready". For real ROM path we return DATA OUT | IRQ in main read,
        // but keep 0x80 here for any stub/legacy paths that check offset 1.
        return (byte)(0x80 | (dataOutBit ? 1 : 0));
    }

    private void WriteClockControl(byte offset, byte value, in BusAccess context)
    {
        // Main control register write for uPD1990AC.
        // bits: 0=DATA IN, 1=SHIFT CLOCK, 2=STROBE, 5:3=COMMAND, others unused/IRQ control.
        UpdateUPD1990Control(value);

        // For compatibility with old stub paths that reset "index"
        readIndex = 0;
    }

    private void UpdateUPD1990Control(byte value)
    {
        int cmd = (value >> 3) & 7;
        bool strobe = (value & 4) != 0;
        bool shiftClk = (value & 2) != 0;
        bool dataIn = (value & 1) != 0;
        bool prevShift = (lastControl & 2) != 0;

        lastControl = value;
        control = value;

        // On "time read" command or strobe with appropriate cmd, load current time into shift register (40 bits BCD).
        // Command values observed/derived: $20 (bit5) toggled in firmware; treat cmd 4 (or strobe) as time read trigger.
        if (cmd == 4 || (strobe && (cmd == 0 || cmd == 1 || cmd == 4)))
        {
            LoadShiftRegisterFromCurrentTime();
        }

        // Rising edge of SHIFT CLOCK: shift out one bit (DATA OUT), shift in DATA IN at MSB.
        if (shiftClk && !prevShift)
        {
            dataOutBit = (shiftReg & 1UL) != 0;
            shiftReg = (shiftReg >> 1) | (dataIn ? (1UL << 39) : 0UL);
        }

        if (strobe)
        {
            // On strobe, for TIME SET cmd the shiftReg would be written to counter.
            // We deliberately do *not* implement host clock modification here.
            // Per the ProDOS doc, Cn0B WRITE with certain values can be used for setting,
            // but the guest must not alter the host (Windows) system clock.
            // IRQ enable/disable bits may be in other values; we expose irqFlag simply for bit7 reads.
            irqFlag = (value & 0x40) != 0; // example mapping, firmware uses reads of control too
        }
    }

    private void LoadShiftRegisterFromCurrentTime()
    {
        var t = CurrentTime;
        int mo = t.Month;   // 1-12
        int dow = (int)t.DayOfWeek; // 0=Sun ..6
        int dom = t.Day;    // 1-31
        int hr = t.Hour;    // 0-23
        int mn = t.Minute;  // 0-59
        int sc = t.Second;  // 0-59

        // Pack 40-bit BCD (10 nibbles) for the uPD1990AC as used by Thunderclock firmware.
        // Per ProDOS TechRef, the firmware (after mode WRITE #$A3 to Cn0B) performs the serial
        // protocol on READ (Cn08) and deposits exactly "mo,da,dt,hr,mn" (with commas) at $0200.
        // We pack the documented fields first. The firmware will clock the bits and build the ASCII.
        // Shift is LSB-first out (dataOutBit = reg & 1; reg >>= 1).
        ulong reg = 0;
        int bit = 0;

        void Pack(int v)
        {
            int ones = v % 10;
            int tens = v / 10;
            reg |= ((ulong)(ones & 0xF) << bit); bit += 4;
            reg |= ((ulong)(tens & 0xF) << bit); bit += 4;
        }

        // Order chosen to align with what the real firmware shifts out for the ProDOS string.
        Pack(mo);
        Pack(dow);   // da in the string
        Pack(dom);   // dt
        Pack(hr);
        Pack(mn);
        Pack(sc);    // extra field the firmware may clock (40 bits total)

        shiftReg = reg;
    }

    private void LatchTime()
    {
        var time = CurrentTime;

        // Thunderclock format (ProDOS compatible):
        // Byte 0: Month (1-12)
        // Byte 1: Day of week (0=Sunday)
        // Byte 2: Day of month (1-31)
        // Byte 3: Hour (0-23)
        // Byte 4: Minute (0-59)
        // Byte 5: Second (0-59)
        // Byte 6: Year low byte (since 1900)
        // Byte 7: Year high byte
        int year = time.Year - 1900;
        timeData[0] = (byte)time.Month;
        timeData[1] = (byte)time.DayOfWeek;
        timeData[2] = (byte)time.Day;
        timeData[3] = (byte)time.Hour;
        timeData[4] = (byte)time.Minute;
        timeData[5] = (byte)time.Second;
        timeData[6] = (byte)(year & 0xFF);
        timeData[7] = (byte)((year >> 8) & 0xFF);

        // Also prepare the 40-bit shift register for uPD1990AC when real ROM is driving the protocol.
        LoadShiftRegisterFromCurrentTime();
    }

    /// <summary>
    /// Writes the ProDOS-expected ASCII time string "mo,da,dt,hr,mn" to the GETLN buffer at $0200.
    /// Exactly as documented in the ProDOS Technical Reference "Adding Routines to ProDOS":
    /// mo=month(01-12), da=dow(00=Sun..06=Sat), dt=day(00-31), hr(00-23), mn(00-59).
    /// Called from the Cn08 trap handler (stub path) to simulate the Thunderclock firmware's READ entry.
    /// </summary>
    private void WriteProDosTimeString(ICpu cpu)
    {
        // mo = month(01-12), da=dow(00-06), dt=day(01-31), hr(00-23), mn(00-59)
        var mo = timeData[0];
        var dow = timeData[1];
        var dom = timeData[2];
        var hr = timeData[3];
        var mn = timeData[4];

        Addr buf = 0x0200;
        WriteTwoDigits(cpu, buf + 0, mo);
        cpu.Write8(buf + 2, (byte)',');
        WriteTwoDigits(cpu, buf + 3, dow);
        cpu.Write8(buf + 5, (byte)',');
        WriteTwoDigits(cpu, buf + 6, dom);
        cpu.Write8(buf + 8, (byte)',');
        WriteTwoDigits(cpu, buf + 9, hr);
        cpu.Write8(buf + 11, (byte)',');
        WriteTwoDigits(cpu, buf + 12, mn);
        // ProDOS parser reads fixed fields; trailing content in buffer is ok.
    }

    private void WriteTwoDigits(ICpu cpu, Addr addr, int value)
    {
        int v = value;
        if (v < 0) v = 0;
        if (v > 99) v = 99;
        byte tens = (byte)('0' + (v / 10));
        byte ones = (byte)('0' + (v % 10));
        cpu.Write8(addr, tens);
        cpu.Write8(addr + 1, ones);
    }

    /// <summary>
    /// Handles the trapped Thunderclock READ entry at $Cn08.
    /// </summary>
    /// <remarks>
    /// ProDOS reaches this via <c>JSR $Cn08</c> and expects an RTS return. The Enhanced IIe
    /// 80-column internal ROM (INTCXROM) can also fall through three <c>PHA</c> saves into
    /// $Cn08; an RTS there would pop those bytes as a return address (e.g. $8788) and BRK-loop.
    /// </remarks>
    private TrapResult CompleteClockReadTrap(ICpu cpu)
    {
        if (!IsJsrlClockEntry(cpu))
        {
            // INTCXROM maps internal ROM at $C408 (TXA). Fall-through is not a JSR entry.
            return TrapResult.NotHandled;
        }

        LatchTime();
        WriteProDosTimeString(cpu);
        return TrapResult.Success(150);
    }

    /// <summary>
    /// Returns <see langword="true"/> when $Cn08/$Cn0B was reached via <c>JSR</c> (ProDOS path).
    /// </summary>
    private static bool IsJsrlClockEntry(ICpu cpu)
    {
        int sp = cpu.Registers.SP.GetByte();
        byte lo = cpu.Read8((Addr)(0x0101 + sp));
        byte hi = cpu.Read8((Addr)(0x0102 + sp));
        byte a = cpu.Registers.A.GetByte();

        // Internal 80-col glue: PHA/PHA/PHA of A, then fall-through into Cn08.
        return lo != a || hi != a;
    }

    /// <summary>
    /// Thin wrapper around RomTarget for the real Thunderclock ROM.
    /// Logs the first few reads from the Cnxx and C8xx regions so we can see
    /// whether the 6502 is actually fetching real bytes (e.g. at C408 and C81A)
    /// instead of 00/EA. This is invaluable for diagnosing the "single C408 read then EA loop".
    /// </summary>
    private sealed class LoggingThunderclockRomTarget : IBusTarget
    {
        private readonly RomTarget _inner;
        private readonly string _area;
        private readonly PocketWatchCard _owner;
        private int _readCount;

        public LoggingThunderclockRomTarget(RomTarget inner, string area, PocketWatchCard owner)
        {
            _inner = inner;
            _area = area;
            _owner = owner;
        }

        public TargetCaps Capabilities => _inner.Capabilities;

        public byte Read8(Addr physicalAddress, in BusAccess context)
        {
            byte value = _inner.Read8(physicalAddress, in context);
            if (_readCount < 8)   // log the first handful of reads (covers sig + C408 entry + early C8)
            {
                int slot = _owner.SlotNumber > 0 ? _owner.SlotNumber : 4;
                Addr baseAddr = (Addr)(0xC000 + slot * 0x100);
                Addr effective = _area == "Cn" ? baseAddr + (physicalAddress & 0xFF) : (Addr)(0xC800 + physicalAddress);
                System.Console.Error.WriteLine($"[POCKET-ROM-READ] { _area } @ ${effective:X4} = ${value:X2} (raw off {physicalAddress})");
            }
            _readCount++;
            return value;
        }

        public void Write8(Addr physicalAddress, byte value, in BusAccess context)
        {
            _inner.Write8(physicalAddress, value, in context);
        }

        public ushort Read16(Addr physicalAddress, in BusAccess context)
            => _inner.Read16(physicalAddress, in context);

        public void Write16(Addr physicalAddress, ushort value, in BusAccess context)
            => _inner.Write16(physicalAddress, value, in context);
    }
}