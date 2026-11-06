// <copyright file="WatchpointManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using System.Text.Json;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core.Cpu;
using BadMango.Emulator.Core.Debugger;
using BadMango.Emulator.Core.Interfaces.Cpu;
using BadMango.Emulator.Core.Interfaces.Debugging;

/// <summary>
/// Specifies the access kinds a watchpoint reacts to.
/// </summary>
[Flags]
public enum WatchAccess
{
    /// <summary>No accesses.</summary>
    None = 0,

    /// <summary>Read accesses (loads).</summary>
    Read = 1,

    /// <summary>Write accesses (stores).</summary>
    Write = 2,

    /// <summary>Both reads and writes.</summary>
    ReadWrite = Read | Write,
}

/// <summary>
/// Tracks memory watchpoints by inspecting the effective address of each
/// executed instruction.
/// </summary>
/// <remarks>
/// <para>
/// The 65C02 CPU does not currently route read/write operations through the trap
/// registry, so this manager is implemented as an <see cref="IDebugStepListener"/>
/// that examines <see cref="DebugStepEventArgs.EffectiveAddress"/> after each step.
/// This covers operand-derived memory access (loads, stores, RMW, JSR/JMP) which is
/// sufficient for diagnosing typical boot-stage corruption.
/// </para>
/// <para>
/// Implicit accesses (interrupt vector reads, stack push/pop, opcode fetch) are
/// <b>not</b> observed and will not trigger watchpoints.
/// </para>
/// </remarks>
public sealed class WatchpointManager : IActivatableDebugStepListener
{
    private readonly Lock syncLock = new();
    private readonly Dictionary<uint, WatchpointEntry> entries = [];
    private ICpu? cpu;
    private IMemoryBus? bus;
    private TextWriter? log;

    /// <inheritdoc/>
    /// <remarks>Raised whenever the set of watchpoints changes (add, remove, clear, load).</remarks>
    public event EventHandler? IsActiveChanged;

    /// <summary>
    /// Gets the number of registered watchpoints.
    /// </summary>
    public int Count
    {
        get
        {
            lock (syncLock)
            {
                return entries.Count;
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The manager is active while at least one watchpoint is registered (enabled or not),
    /// so the step listener stays attached while watchpoints are merely toggled.
    /// </remarks>
    public bool IsActive => Count > 0;

    /// <summary>
    /// Gets the address of the watchpoint that most recently fired,
    /// or <see langword="null"/> if no watchpoint has fired.
    /// </summary>
    public uint? LastHitAddress { get; private set; }

    /// <summary>
    /// Gets the access kind of the most recent watchpoint hit.
    /// </summary>
    public WatchAccess LastHitAccess { get; private set; }

    /// <summary>
    /// Gets the value that was read from or written to the watched address on the most
    /// recent hit, or <see langword="null"/> if no watchpoint has fired or the bus was
    /// not attached at the time of the hit.
    /// </summary>
    public byte? LastHitValue { get; private set; }

    /// <summary>
    /// Attaches the manager to a CPU so hits can request a stop.
    /// </summary>
    /// <param name="cpu">The CPU to halt when a stop-on-hit watchpoint fires.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cpu"/> is <see langword="null"/>.
    /// </exception>
    public void Attach(ICpu cpu)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        this.cpu = cpu;
    }

    /// <summary>
    /// Attaches the manager to a CPU and memory bus for watchpoint hit logging with value emission.
    /// </summary>
    /// <param name="cpu">The CPU to halt when a stop-on-hit watchpoint fires.</param>
    /// <param name="bus">The memory bus for reading values at watched addresses.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cpu"/> or <paramref name="bus"/> is <see langword="null"/>.
    /// </exception>
    public void AttachWithBus(ICpu cpu, IMemoryBus bus)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        ArgumentNullException.ThrowIfNull(bus);
        this.cpu = cpu;
        this.bus = bus;
    }

    /// <summary>
    /// Detaches the manager from the CPU and bus. Watchpoint definitions are retained.
    /// </summary>
    public void Detach()
    {
        cpu = null;
        bus = null;
    }

    /// <summary>
    /// Sets the writer used for watchpoint hit log lines.
    /// </summary>
    /// <param name="writer">The writer, or <see langword="null"/> to disable logging.</param>
    public void SetLogOutput(TextWriter? writer)
    {
        lock (syncLock)
        {
            log = writer;
        }
    }

    /// <summary>
    /// Adds a watchpoint at the specified address.
    /// </summary>
    /// <param name="address">The memory address to watch.</param>
    /// <param name="access">Which access kinds should trigger the watchpoint.</param>
    /// <param name="stopOnHit">Whether to halt the CPU when the watchpoint fires.</param>
    /// <param name="label">Optional human-readable label.</param>
    /// <returns>
    /// <see langword="true"/> if a new watchpoint was added;
    /// <see langword="false"/> if one already existed at the address.
    /// </returns>
    public bool Add(uint address, WatchAccess access, bool stopOnHit, string? label = null)
    {
        lock (syncLock)
        {
            if (entries.ContainsKey(address))
            {
                return false;
            }

            entries[address] = new WatchpointEntry(address, access, stopOnHit, label, true);
        }

        OnWatchpointsChanged();
        return true;
    }

    /// <summary>
    /// Removes the watchpoint at the specified address.
    /// </summary>
    /// <param name="address">The address of the watchpoint to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the watchpoint existed and was removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Remove(uint address)
    {
        bool removed;
        lock (syncLock)
        {
            removed = entries.Remove(address);
        }

        if (removed)
        {
            OnWatchpointsChanged();
        }

        return removed;
    }

    /// <summary>
    /// Removes every watchpoint.
    /// </summary>
    public void Clear()
    {
        lock (syncLock)
        {
            entries.Clear();
        }

        OnWatchpointsChanged();
    }

    /// <summary>
    /// Enables or disables a watchpoint without removing it.
    /// </summary>
    /// <param name="address">The watchpoint address.</param>
    /// <param name="enabled">Whether the watchpoint should be enabled.</param>
    /// <returns>
    /// <see langword="true"/> if the watchpoint exists and was updated;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool SetEnabled(uint address, bool enabled)
    {
        lock (syncLock)
        {
            if (!entries.TryGetValue(address, out var entry))
            {
                return false;
            }

            entry.Enabled = enabled;
            return true;
        }
    }

    /// <summary>
    /// Gets a snapshot of every registered watchpoint.
    /// </summary>
    /// <returns>A copy of the current watchpoint entries.</returns>
    public IReadOnlyList<WatchpointEntry> GetAll()
    {
        lock (syncLock)
        {
            return [.. entries.Values];
        }
    }

    /// <summary>
    /// Clears the recorded last-hit address.
    /// </summary>
    public void ResetLastHit()
    {
        LastHitAddress = null;
        LastHitAccess = WatchAccess.None;
        LastHitValue = null;
    }

    /// <summary>
    /// Saves the current watchpoint configuration to a JSON file.
    /// </summary>
    /// <param name="filePath">The path where the configuration should be saved.</param>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    public void SaveToFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        lock (syncLock)
        {
            var data = new
            {
                watchpoints = entries.Values.Select(wp => new
                {
                    address = $"0x{wp.Address:X4}",
                    access = wp.Access.ToString(),
                    stopOnHit = wp.StopOnHit,
                    label = wp.Label,
                    enabled = wp.Enabled,
                }).ToList(),
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Loads watchpoint configuration from a JSON file and adds them to the manager.
    /// </summary>
    /// <param name="filePath">The path to the configuration file to load.</param>
    /// <exception cref="IOException">Thrown if the file cannot be read.</exception>
    /// <exception cref="JsonException">Thrown if the file is not valid JSON.</exception>
    public void LoadFromFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Watchpoint configuration file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("watchpoints", out var wpsElement))
        {
            return;
        }

        lock (syncLock)
        {
            foreach (var wpElement in wpsElement.EnumerateArray())
            {
                if (!wpElement.TryGetProperty("address", out var addrElement))
                {
                    continue;
                }

                var addrStr = addrElement.GetString();
                if (string.IsNullOrEmpty(addrStr) || !uint.TryParse(addrStr.StartsWith("0x") ? addrStr[2..] : addrStr, System.Globalization.NumberStyles.HexNumber, null, out uint address))
                {
                    continue;
                }

                WatchAccess access = WatchAccess.ReadWrite;
                if (wpElement.TryGetProperty("access", out var accessElement))
                {
                    var accessStr = accessElement.GetString();
                    if (!string.IsNullOrEmpty(accessStr) && Enum.TryParse<WatchAccess>(accessStr, out var parsed))
                    {
                        access = parsed;
                    }
                }

                bool stopOnHit = false;
                if (wpElement.TryGetProperty("stopOnHit", out var stopElement))
                {
                    stopOnHit = stopElement.GetBoolean();
                }

                string? label = null;
                if (wpElement.TryGetProperty("label", out var labelElement) && labelElement.ValueKind != JsonValueKind.Null)
                {
                    label = labelElement.GetString();
                }

                bool enabled = true;
                if (wpElement.TryGetProperty("enabled", out var enabledElement))
                {
                    enabled = enabledElement.GetBoolean();
                }

                // Add the watchpoint
                if (!entries.ContainsKey(address))
                {
                    var entry = new WatchpointEntry(address, access, stopOnHit, label, enabled);
                    entries[address] = entry;
                }
            }
        }

        OnWatchpointsChanged();
    }

    /// <inheritdoc/>
    public void OnBeforeStep(in DebugStepEventArgs eventData)
    {
        // No-op; effective address is only meaningful after execution.
    }

    /// <inheritdoc/>
    public void OnAfterStep(in DebugStepEventArgs eventData)
    {
        // Cheap early-out before locking.
        if (entries.Count == 0)
        {
            return;
        }

        WatchAccess access = ClassifyAccess(eventData.Instruction);
        if (access == WatchAccess.None)
        {
            return;
        }

        uint addr = eventData.EffectiveAddress;

        WatchpointEntry? hit = null;
        TextWriter? logSnapshot;
        lock (syncLock)
        {
            if (entries.TryGetValue(addr, out var entry) &&
                entry.Enabled &&
                (entry.Access & access) != 0)
            {
                entry.IncrementHits();
                hit = entry;
            }

            logSnapshot = log;
        }

        if (hit is null)
        {
            return;
        }

        LastHitAddress = addr;
        LastHitAccess = access;

        // Attempt to read the value at the effective address for emit logging
        byte value = 0;
        bool hasValue = false;
        if (bus is not null)
        {
            try
            {
                var busAccess = new BusAccess(
                    Address: addr,
                    Value: 0,
                    WidthBits: 8,
                    Mode: BusAccessMode.Atomic,
                    EmulationFlag: false,
                    Intent: AccessIntent.DebugRead,
                    SourceId: 0,
                    Cycle: 0,
                    Flags: AccessFlags.NoSideEffects,
                    PrivilegeLevel: Bus.PrivilegeLevel.Ring0);
                value = bus.Read8(in busAccess);
                hasValue = true;
            }
            catch
            {
                // If we can't read the value, continue without it
            }
        }

        LastHitValue = hasValue ? value : null;

        logSnapshot?.WriteLine(
            $"[watch] {(access == WatchAccess.Read ? "R" : "W")} ${addr:X4}" +
            (hasValue ? $" Val=${value:X2}" : string.Empty) +
            $" by ${eventData.PC:X4} ({eventData.Instruction}) " +
            $"A={eventData.Registers.A.GetByte():X2} " +
            $"X={eventData.Registers.X.GetByte():X2} " +
            $"Y={eventData.Registers.Y.GetByte():X2}" +
            (hit.Label is null ? string.Empty : $"  ; {hit.Label}"));

        if (hit.StopOnHit)
        {
            cpu?.RequestStop();
        }
    }

    private static WatchAccess ClassifyAccess(CpuInstructions instruction)
    {
        return instruction switch
        {
            // Stores
            CpuInstructions.STA => WatchAccess.Write,
            CpuInstructions.STX => WatchAccess.Write,
            CpuInstructions.STY => WatchAccess.Write,
            CpuInstructions.STZ => WatchAccess.Write,

            // Loads
            CpuInstructions.LDA => WatchAccess.Read,
            CpuInstructions.LDX => WatchAccess.Read,
            CpuInstructions.LDY => WatchAccess.Read,
            CpuInstructions.BIT => WatchAccess.Read,
            CpuInstructions.CMP => WatchAccess.Read,
            CpuInstructions.CPX => WatchAccess.Read,
            CpuInstructions.CPY => WatchAccess.Read,
            CpuInstructions.AND => WatchAccess.Read,
            CpuInstructions.ORA => WatchAccess.Read,
            CpuInstructions.EOR => WatchAccess.Read,
            CpuInstructions.ADC => WatchAccess.Read,
            CpuInstructions.SBC => WatchAccess.Read,

            // Read-modify-write
            CpuInstructions.ASL => WatchAccess.ReadWrite,
            CpuInstructions.LSR => WatchAccess.ReadWrite,
            CpuInstructions.ROL => WatchAccess.ReadWrite,
            CpuInstructions.ROR => WatchAccess.ReadWrite,
            CpuInstructions.INC => WatchAccess.ReadWrite,
            CpuInstructions.DEC => WatchAccess.ReadWrite,
            CpuInstructions.TRB => WatchAccess.ReadWrite,
            CpuInstructions.TSB => WatchAccess.ReadWrite,

            _ => WatchAccess.None,
        };
    }

    private void OnWatchpointsChanged() => IsActiveChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Represents a single watchpoint entry.
    /// </summary>
    public sealed class WatchpointEntry
    {
        private long hits;

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchpointEntry"/> class.
        /// </summary>
        /// <param name="address">The address being watched.</param>
        /// <param name="access">The access kinds that trigger the watchpoint.</param>
        /// <param name="stopOnHit">Whether the CPU should be requested to stop on hit.</param>
        /// <param name="label">Optional label.</param>
        /// <param name="enabled">Whether the watchpoint is enabled.</param>
        public WatchpointEntry(
            uint address,
            WatchAccess access,
            bool stopOnHit,
            string? label,
            bool enabled)
        {
            Address = address;
            Access = access;
            StopOnHit = stopOnHit;
            Label = label;
            Enabled = enabled;
        }

        /// <summary>
        /// Gets the address being watched.
        /// </summary>
        public uint Address { get; }

        /// <summary>
        /// Gets the access kinds that trigger the watchpoint.
        /// </summary>
        public WatchAccess Access { get; }

        /// <summary>
        /// Gets a value indicating whether a hit requests a CPU stop.
        /// </summary>
        public bool StopOnHit { get; }

        /// <summary>
        /// Gets the optional human-readable label for the watchpoint.
        /// </summary>
        public string? Label { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this watchpoint is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets the number of times this watchpoint has fired.
        /// </summary>
        public long Hits => Interlocked.Read(ref hits);

        /// <summary>
        /// Increments the hit counter for this watchpoint by one.
        /// </summary>
        internal void IncrementHits() => Interlocked.Increment(ref hits);
    }
}