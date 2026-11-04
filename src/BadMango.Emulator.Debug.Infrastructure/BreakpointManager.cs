// <copyright file="BreakpointManager.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure;

using System.Text.Json;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;
using BadMango.Emulator.Core;
using BadMango.Emulator.Core.Interfaces.Cpu;

/// <summary>
/// Manages execution breakpoints by registering <see cref="TrapOperation.Call"/>
/// handlers with the active <see cref="ITrapRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each breakpoint is implemented as a call trap that fires when the CPU fetches an
/// instruction at the breakpoint address. When an armed breakpoint fires, the trap
/// increments the hit counter, calls <see cref="ICpu.RequestStop"/>, marks the
/// breakpoint as pending-skip-on-resume, and returns a handled trap result with
/// <see cref="TrapReturnMethod.None"/> and zero cycles. Because the trap is
/// reported as handled with no return-address override, the CPU leaves the program
/// counter pointing at the breakpoint address and the run loop exits before the
/// instruction at that address is executed. This gives "stop-before-execute"
/// semantics, which preserves the original PC, opcode, and operand bytes for
/// inspection -- especially important when the trapped instruction is a JMP, JSR,
/// or branch that would otherwise change control flow.
/// </para>
/// <para>
/// When the user resumes (via <c>step</c> or <c>run</c>), the trap fires again at
/// the same PC. The pending-skip-on-resume flag is consumed and the trap returns
/// <see cref="TrapResult.NotHandled"/>, allowing the original instruction to
/// execute normally. The breakpoint is automatically rearmed for the next visit.
/// </para>
/// <para>
/// Breakpoints can be temporarily disabled without being removed. Disabled
/// breakpoints are still registered with the trap registry but neither stop the
/// CPU nor consume a skip-on-resume.
/// </para>
/// </remarks>
public sealed class BreakpointManager
{
    private readonly Lock syncLock = new();
    private readonly Dictionary<uint, BreakpointEntry> entries = [];
    private ITrapRegistry? registry;
    private ICpu? cpu;

    /// <summary>
    /// Gets the number of registered breakpoints.
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

    /// <summary>
    /// Gets the address of the breakpoint that most recently caused a stop,
    /// or <see langword="null"/> if no breakpoint has fired.
    /// </summary>
    public uint? LastHitAddress { get; private set; }

    /// <summary>
    /// Attaches the manager to a CPU and trap registry. Any breakpoints added
    /// before attachment are re-registered with the new registry.
    /// </summary>
    /// <param name="cpu">The CPU whose execution will be stopped on hits.</param>
    /// <param name="registry">The trap registry to register breakpoint traps with.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cpu"/> or <paramref name="registry"/> is <see langword="null"/>.
    /// </exception>
    public void Attach(ICpu cpu, ITrapRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(cpu);
        ArgumentNullException.ThrowIfNull(registry);

        lock (syncLock)
        {
            this.cpu = cpu;
            this.registry = registry;

            // Re-register any pre-existing breakpoints with the new registry.
            foreach (var entry in entries.Values)
            {
                RegisterWithRegistry(entry);
            }
        }
    }

    /// <summary>
    /// Detaches the manager, unregistering all live trap entries from the
    /// previously attached registry. Breakpoint definitions are retained.
    /// </summary>
    public void Detach()
    {
        lock (syncLock)
        {
            if (registry is not null)
            {
                foreach (var addr in entries.Keys)
                {
                    registry.Unregister(addr, TrapOperation.Call);
                }
            }

            registry = null;
            cpu = null;
        }
    }

    /// <summary>
    /// Adds a breakpoint at the specified address.
    /// </summary>
    /// <param name="address">The address to break on.</param>
    /// <param name="label">Optional human-readable label for the breakpoint.</param>
    /// <returns>
    /// <see langword="true"/> if a new breakpoint was added;
    /// <see langword="false"/> if one already existed at the address.
    /// </returns>
    public bool Add(uint address, string? label = null)
    {
        lock (syncLock)
        {
            if (entries.ContainsKey(address))
            {
                return false;
            }

            var entry = new BreakpointEntry(address, label, true);
            entries[address] = entry;
            RegisterWithRegistry(entry);
            return true;
        }
    }

    /// <summary>
    /// Removes the breakpoint at the specified address.
    /// </summary>
    /// <param name="address">The address of the breakpoint to remove.</param>
    /// <returns>
    /// <see langword="true"/> if the breakpoint existed and was removed;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Remove(uint address)
    {
        lock (syncLock)
        {
            if (!entries.Remove(address))
            {
                return false;
            }

            registry?.Unregister(address, TrapOperation.Call);
            return true;
        }
    }

    /// <summary>
    /// Removes every breakpoint.
    /// </summary>
    public void Clear()
    {
        lock (syncLock)
        {
            if (registry is not null)
            {
                foreach (var addr in entries.Keys)
                {
                    registry.Unregister(addr, TrapOperation.Call);
                }
            }

            entries.Clear();
        }
    }

    /// <summary>
    /// Enables or disables the breakpoint at the specified address.
    /// </summary>
    /// <param name="address">The address of the breakpoint.</param>
    /// <param name="enabled">Whether the breakpoint should be enabled.</param>
    /// <returns>
    /// <see langword="true"/> if the breakpoint exists and its state was updated;
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
    /// Gets a snapshot of every registered breakpoint.
    /// </summary>
    /// <returns>A copy of the current breakpoint entries.</returns>
    public IReadOnlyList<BreakpointEntry> GetAll()
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
    }

    /// <summary>
    /// Saves the current breakpoint configuration to a JSON file.
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
                breakpoints = entries.Values.Select(bp => new
                {
                    address = $"0x{bp.Address:X4}",
                    label = bp.Label,
                    enabled = bp.Enabled,
                }).ToList(),
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
    }

    /// <summary>
    /// Loads breakpoint configuration from a JSON file and adds them to the manager.
    /// </summary>
    /// <param name="filePath">The path to the configuration file to load.</param>
    /// <exception cref="IOException">Thrown if the file cannot be read.</exception>
    /// <exception cref="JsonException">Thrown if the file is not valid JSON.</exception>
    public void LoadFromFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Breakpoint configuration file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("breakpoints", out var bpsElement))
        {
            return;
        }

        lock (syncLock)
        {
            foreach (var bpElement in bpsElement.EnumerateArray())
            {
                if (!bpElement.TryGetProperty("address", out var addrElement))
                {
                    continue;
                }

                var addrStr = addrElement.GetString();
                if (string.IsNullOrEmpty(addrStr) || !uint.TryParse(addrStr.StartsWith("0x") ? addrStr[2..] : addrStr, System.Globalization.NumberStyles.HexNumber, null, out uint address))
                {
                    continue;
                }

                string? label = null;
                if (bpElement.TryGetProperty("label", out var labelElement) && labelElement.ValueKind != JsonValueKind.Null)
                {
                    label = labelElement.GetString();
                }

                bool enabled = true;
                if (bpElement.TryGetProperty("enabled", out var enabledElement))
                {
                    enabled = enabledElement.GetBoolean();
                }

                // Add the breakpoint
                if (!entries.ContainsKey(address))
                {
                    var entry = new BreakpointEntry(address, label, enabled);
                    entries[address] = entry;
                    RegisterWithRegistry(entry);
                }
            }
        }
    }

    private void RegisterWithRegistry(BreakpointEntry entry)
    {
        if (registry is null)
        {
            return;
        }

        TrapResult Handler(ICpu trapCpu, IMemoryBus trapBus, IEventContext trapContext)
        {
            BreakpointEntry? snapshot;
            lock (syncLock)
            {
                entries.TryGetValue(entry.Address, out snapshot);
            }

            if (snapshot is null || !snapshot.Enabled)
            {
                return TrapResult.NotHandled;
            }

            // If this trap fired because the user just resumed from a stopped
            // breakpoint, consume the skip-once flag and let the original
            // instruction execute normally. The breakpoint rearms automatically.
            if (snapshot.SkipNextHit)
            {
                snapshot.SkipNextHit = false;
                return TrapResult.NotHandled;
            }

            // Armed hit: stop *before* the instruction executes. Mark the
            // breakpoint to skip its very next trap (the resume) and report
            // the trap as handled with no PC change and zero cycles. The CPU
            // leaves PC at the breakpoint address, the run loop sees the stop
            // request and exits, and the instruction at the breakpoint is
            // preserved for inspection.
            snapshot.IncrementHits();
            snapshot.SkipNextHit = true;
            LastHitAddress = entry.Address;
            cpu?.RequestStop();
            return TrapResult.Success(Cycle.Zero, TrapReturnMethod.None);
        }

        registry.Register(
            entry.Address,
            TrapOperation.Call,
            $"BP_{entry.Address:X4}",
            TrapCategory.UserDefined,
            Handler,
            entry.Label);
    }

    /// <summary>
    /// Represents a single breakpoint entry.
    /// </summary>
    public sealed class BreakpointEntry
    {
        private long hits;

        /// <summary>
        /// Initializes a new instance of the <see cref="BreakpointEntry"/> class.
        /// </summary>
        /// <param name="address">The breakpoint address.</param>
        /// <param name="label">Optional label.</param>
        /// <param name="enabled">Whether the breakpoint is enabled.</param>
        public BreakpointEntry(uint address, string? label, bool enabled)
        {
            Address = address;
            Label = label;
            Enabled = enabled;
        }

        /// <summary>
        /// Gets the address of the breakpoint.
        /// </summary>
        public uint Address { get; }

        /// <summary>
        /// Gets the optional human-readable label for the breakpoint.
        /// </summary>
        public string? Label { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the breakpoint is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets the number of times this breakpoint has been hit.
        /// </summary>
        public long Hits => Interlocked.Read(ref hits);

        /// <summary>
        /// Gets or sets a value indicating whether the next trap firing at this
        /// address should be skipped (i.e., the original instruction should execute
        /// without re-triggering the breakpoint). Used to implement
        /// stop-before-execute resume semantics.
        /// </summary>
        internal bool SkipNextHit { get; set; }

        /// <summary>
        /// Increments the hit counter for this breakpoint by one.
        /// </summary>
        internal void IncrementHits() => Interlocked.Increment(ref hits);
    }
}