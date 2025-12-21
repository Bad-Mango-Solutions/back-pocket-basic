# Privileged architecture spec v0.1 for 65832

This locks the “hard reality” the rest of the platform must obey: privilege, traps, paging, and compatibility mode rules. It’s written so you can implement it in an emulator first, then in hardware later without changing the contract.

------

## Execution modes and privilege

### Architectural modes

- **M0 — 65C02 mode:** legacy 6502 semantics (16-bit PC, 64 KB visible space, legacy flags/instructions).
- **M1 — 65816 mode:** legacy 65816 semantics (including emulation/native behavior as defined for 65816; 24-bit addressing semantics).
- **M2 — 65832 mode:** native 32-bit mode (flat 32-bit virtual addressing, R0–R7 available, protected execution).

### Privilege levels

- **U — user mode:** memory protections enforced; privileged instructions trap.
- **K — kernel mode:** full access; may manage MMU, interrupts, mode switching, I/O.

### Global invariants

- **Only K may change architectural mode** (M0/M1/M2).
- **U may execute in M0 or M1** only via a kernel-created compatibility context; user code cannot “flip” itself into legacy mode.
- **All traps/interrupts enter K mode** in **M2**, regardless of the mode of the interrupted context (see trap entry).

------

## Register file and core state

### General registers

- **A, X, Y:** 32-bit registers with 8/16/32-bit *views* (view selection is architectural state; truncation/extension rules are deterministic).
- **R0–R7:** 32-bit general registers (available in M2; reads/writes in M0/M1 are illegal and trap).
- **SP:** 32-bit stack pointer.
- **PC:** 32-bit program counter (in M0/M1, legacy semantics are emulated; architecturally, PC is still represented as 32-bit internally).
- **P:** status flags (includes at minimum C, Z, N, V, I, D; plus mode/view bits as needed).

### Control registers (K-only in M2)

- **CR0 (feature/control):**
  - **PG:** paging enable
  - **UM:** user-mode enable (if cleared, U behaves as K for bring-up; optional but useful)
  - **NX:** enforce non-executable pages (if implemented)
- **PTBR:** page table base register (physical pointer to root table).
- **ASID:** address space identifier for TLB tagging (optional in v0.1; reserved field required even if not implemented).
- **EPC / EPSR:** saved PC and saved P on trap entry (or banked “trap frame” mechanism; see trap frame).

If you’d rather keep fewer named control regs, treat CR0/PTBR/ASID as “system registers” accessed via privileged `MRS/MSR`-style instructions.

------

## Memory model

### Address spaces

- **Virtual address size:** 32-bit in M2.
- **Physical address size:** implementation-defined; must be at least 32-bit for a “full” system. Emulators may back it with host pointers.
- **Endianness:** little-endian.

### Page size and alignment

- **Page size:** 4 KB.
- **Accesses:** unaligned accesses are permitted in base v0.1 but may be slower; optional alignment-fault enforcement can be enabled later via CR0 (reserved).

### Permissions

Each page has:

- **R:** readable
- **W:** writable
- **X:** executable
- **U:** user-accessible (if clear, K-only)

Required behavior:

- Instruction fetch requires **X** and appropriate privilege.
- Data read/write requires **R/W** and appropriate privilege.
- If **NX** is supported and enabled, execute permission is strictly enforced via **X**.

------

## Paging and page tables

### Required paging semantics

- When **CR0.PG = 0**, the system is in **identity-mapped** mode:
  - Virtual addresses map directly to physical addresses (still subject to optional protection if you choose, but simplest is “no protection when PG=0”).
- When **CR0.PG = 1**, translations are via page tables rooted at **PTBR**.

### Page table structure (two-level, Linux-friendly)

Use a 32-bit VA split:

- **L1 index:** top 10 bits (1024 entries)
- **L2 index:** next 10 bits (1024 entries)
- **Offset:** low 12 bits (4 KB)

So each L2 table covers 4 MB; full space covers 4 GB.

### PTE format (v0.1)

A minimal 32-bit PTE is enough for emulation; reserve bits for growth:

- **P (bit 0):** present
- **R (bit 1):** readable
- **W (bit 2):** writable
- **X (bit 3):** executable
- **U (bit 4):** user
- **A (bit 5):** accessed (set by hardware if implemented; otherwise ignored)
- **D (bit 6):** dirty (set by hardware on write if implemented; otherwise ignored)
- **G (bit 7):** global (optional; TLB)
- **PFN (bits 12..31):** physical frame number (upper physical bits are implementation-defined; in a strict 32-bit physical system PFN fully defines PA)

Bits 8..11 reserved for:

- page attributes (cacheable, device, write-through)
- software-defined OS bits

### Fault model

A translation or permission violation raises a **Page fault** exception with:

- **FAR:** faulting virtual address
- **FSC:** fault status code (at minimum: not-present, read-violation, write-violation, exec-violation, privilege-violation)

These can be exposed via system registers readable in K.

------

## Trap, interrupt, and exception model

### Trap entry: deterministic and mode-clean

On any exception/interrupt/trap:

1. CPU switches to **K privilege**.
2. CPU switches to **M2 mode** for handler execution.
3. CPU saves interrupted context into a defined **trap frame** (see below).
4. CPU vectors to handler address from the **M2 vector table**.

### Vector table

- A fixed base address in virtual memory, e.g. `0x00000000` (or a relocatable **VBAR** system register; recommended).
- Vector entries are 32-bit addresses.

Minimum vector set:

- **RESET**
- **NMI**
- **IRQ**
- **PAGEFAULT**
- **SYSCALL**
- **ILLEGAL**
- **BREAKPOINT**

### Trap frame format (architectural contract)

Trap entry pushes a fixed frame to the **kernel stack** (K-mode SP), in this order:

1. **Saved PC (32)**
2. **Saved P (32)**
3. **Saved mode (M0/M1/M2) + saved privilege (U/K) (32)**
4. **Fault info (optional slot; for pagefault includes FAR/FSC pointer or packed) (32)**
5. **Saved A, X, Y (32 each)**
6. **Saved R0–R7 (32 each)**

That’s large, but it makes debugging and context switching brutally simple and consistent. If you want a “fast path,” allow an implementation option to omit saving R-registers unless configured—*but the architectural ABI for the kernel should assume the full frame exists*.

------

## Compatibility execution in a protected world

### Compatibility contexts

The kernel may run a user task in M0 or M1 semantics by setting:

- task mode = M0 or M1
- task privilege = U
- task memory map = appropriate compatibility layout

On trap entry, the CPU always returns control to M2 K handlers, with the trap frame recording the prior mode for emulation/return.

### Return from trap

`RTE` restores:

- mode (M0/M1/M2)
- privilege (U/K)
- PC, P
- registers as defined by the trap frame (or as requested by the kernel)

User code cannot fabricate an `RTE` to gain K; executing `RTE` in U traps as illegal.

------

## Privileged instruction set boundaries

### Privileged-only operations (must trap in U)

- Modify CR0/PTBR/ASID/VBAR/FAR/FSC
- Enable/disable interrupts globally (if exposed)
- Change architectural mode (M0/M1/M2)
- I/O port access (if you have a port model)
- TLB operations (invalidate, shootdown)
- Cache control hints (optional)

### Unprivileged but controlled

- `SYS imm8` is callable from U and enters SYSCALL vector.

------

## Debug support

### Breakpoints

- `BRK imm8` (or an M2-only `BRK32`) raises **BREAKPOINT** with the immediate recorded in the trap frame.

### Single-step

Reserve a flag in P (or a system register) for single-step; when set in U, the CPU traps after each instruction into a **DEBUG** vector (optional in v0.1, reserved now).

------

# ABI v1 spec for 65832 mode

This is the contract for userland toolchains, libraries, and syscalls in **M2 U-mode**.

------

## Data model

### Integer and pointer sizes

- **`char`:** 8-bit
- **`short`:** 16-bit
- **`int`:** 32-bit
- **`long`:** 32-bit
- **`long long`:** 64-bit
- **pointers:** 32-bit
- **`size_t`:** 32-bit
- **endianness:** little-endian

This is effectively **ILP32**, which keeps the machine small and honest.

------

## Register roles

### Volatility (caller/callee saved)

- **Caller-saved:** **A, X, Y, R0–R3**
- **Callee-saved:** **R4–R7**
- **Special:** **R7 is the canonical frame pointer when a frame pointer is used** (debug-friendly convention)

### Return values

- **32-bit integer/pointer:** R0
- **64-bit integer:** R0 = low32, R1 = high32
- **Small structs (<= 8 bytes):** may return in R0/R1 (compiler choice, but must be documented; safest rule is “return in memory” unless you want to standardize this)
- **Floating point:** none in v1 (if you add FP later, define a separate FP ABI extension)

------

## Calling convention

### Argument passing

- **Arg0–Arg3:** R0, R1, R2, R3
- **Arg4+:** on stack, 32-bit slots, pushed right-to-left
- **Varargs:** caller spills register args to a “register save area” in the callee’s frame when needed (standard modern ABI trick; you can mandate this for predictable `va_list`)

### Stack

- **Growth:** downward
- **Alignment at call boundary:** 16-byte aligned
- **Red zone:** none (simplifies interrupts/signal delivery)

### Function prologue (recommended pattern)

When a function needs locals or uses callee-saved regs:

- save R4–R7 as needed
- establish FP in R7 if debug/unwind requires it
- allocate locals by subtracting from SP

Leaf functions can omit FP and avoid touching callee-saved regs.

------

## Unwinding and debugging

### Frame pointer convention (v1 baseline)

- If a function uses FP, it must:
  - set **R7 = previous SP** (or a stable base)
  - store the prior R7 in the frame
- The return address location is fixed relative to FP.

This makes a backtrace possible even with minimal debug info.

If you want to go further later, add optional CFI metadata, but don’t require it for v1.

------

## Syscall ABI v1

### Invocation

- `SYS imm8` triggers SYSCALL trap.
- `imm8` is a **syscall class** (0 = core, others reserved) to leave space for fast-path subsystems later.

### Register convention

- **R0:** syscall number
- **R1–R3:** arg0–arg2
- **Stack:** arg3+ (32-bit slots)
- **Return success:** R0 = result, Carry clear
- **Return error:** R0 = errno (positive), Carry set

This keeps the classic “carry indicates failure” flavor while being ergonomic for POSIX shims.

------

## Signal/interrupt interaction rule for userland

- Userland must assume any instruction boundary can trap into the kernel.
- Caller-saved registers may be clobbered across signal delivery unless the kernel defines a signal frame that restores full state (recommended).

------

# Implementation notes that keep it elegant

- You can implement the paging model internally using host 64-bit sparse mappings and still present the exact two-level semantics architecturally.
- You can keep M0/M1 compatibility faithful by treating legacy address formation as an input to the M2 MMU translation (compat VA → MMU VA → PA).

------

## Two decisions to confirm before you paste this into the repo

1. **Vector base:** fixed at `0x00000000`, or do you want a **VBAR** register for relocatable vectors?
2. **Trap frame size:** do you want “save all regs always” (max debuggability) or “minimal + lazy save” (max performance)? My recommendation for your goals is **save all always** for v1, then optimize later.

# Privileged architecture spec v0.2 for 65832

This revision locks in: **VBAR**, **save-always trap frames**, and **NX** (no-execute) as a first-class security feature.

------

## Execution modes and privilege

### Architectural modes

- **M0 — 65C02 mode:** legacy 6502 semantics (16-bit PC, 64 KB visible space, legacy flags/instructions).
- **M1 — 65816 mode:** legacy 65816 semantics (including emulation/native behavior; 24-bit addressing semantics).
- **M2 — 65832 mode:** native 32-bit mode (flat 32-bit virtual addressing, R0–R7, protected execution).

### Privilege levels

- **U — user mode:** memory protections enforced; privileged instructions trap.
- **K — kernel mode:** full access; manages MMU, interrupts, mode switching, and privileged state.

### Invariants

- **Only K may change architectural mode** (M0/M1/M2).
- **All exceptions/traps/interrupts enter K in M2**, regardless of interrupted mode.
- **RTE is privileged:** executing `RTE` in U traps as illegal.

------

## Core registers and system registers

### General registers

- **A, X, Y:** 32-bit registers with 8/16/32-bit views (view state is architectural).
- **R0–R7:** 32-bit general registers (available in M2 only).
- **SP:** 32-bit stack pointer.
- **PC:** 32-bit program counter (M0/M1 compatibility semantics preserved).
- **P:** status flags (includes at minimum C, Z, N, V, I, D, plus architectural mode/view bits).

### System registers (K-only; reads in U either trap or return masked values, your choice—recommend trap)

- **CR0:**
  - **PG:** paging enable
  - **UM:** user-mode enable (if cleared, U behaves as K for bring-up; optional but useful)
  - **NXE:** NX enforcement enable (**required**; see NX behavior)
- **VBAR:** vector base address (32-bit, must be 4 KB aligned).
- **PTBR:** page table base register (physical pointer to L1 root).
- **ASID:** address space identifier (reserved in v0.2; may be implemented later).
- **FAR:** fault address register (VA that caused the most recent page fault).
- **FSC:** fault status code register (reason for the most recent page fault).

------

## Memory model, paging, and NX

### Addressing

- **Virtual address size:** 32-bit in M2.
- **Endianness:** little-endian.
- **Page size:** 4 KB.

### Two-level page tables

VA split:

- **L1 index:** bits 31..22 (1024 entries)
- **L2 index:** bits 21..12 (1024 entries)
- **Offset:** bits 11..0

### PTE format (v0.2, 32-bit)

- **bit 0 P:** present
- **bit 1 R:** readable
- **bit 2 W:** writable
- **bit 3 X:** executable
- **bit 4 U:** user
- **bit 5 A:** accessed (optional; if not implemented reads as 0 and is ignored)
- **bit 6 D:** dirty (optional; if not implemented reads as 0 and is ignored)
- **bit 7 G:** global (optional)
- **bits 8..11:** reserved for OS / attributes
- **bits 12..31 PFN:** physical frame number

### NX behavior

NX is implemented via the **X** permission bit plus the global enable **CR0.NXE**:

- If **CR0.NXE = 1**, an instruction fetch requires **P=1** and **X=1** and appropriate privilege; otherwise a page fault occurs with **exec-violation**.
- If **CR0.NXE = 0**, the **X** bit is ignored and instruction fetch permission is controlled only by **P** and privilege; this is intended only for early bring-up and must still be K-controlled.

This gives you strong defaults without blocking bootstrap.

### Fault status codes (minimum set)

`FSC` must encode at least:

- **not-present**
- **read-violation**
- **write-violation**
- **exec-violation**
- **privilege-violation**
- **reserved-bit/format-violation** (helps catch corrupted tables deterministically)

On a page fault, **FAR** is set to the faulting virtual address and **FSC** to the reason before vectoring.

------

## Vectoring with VBAR

### Vector base

- **VBAR** defines the base address of the vector table in **virtual address space**.
- **Alignment:** VBAR must be **4 KB aligned**; writes that violate alignment trap (or are masked—recommend trap).
- **Vector entries:** 32-bit handler addresses stored at `VBAR + 4 \cdot vector_index`.

### Minimum vector indices

Define these indices (stable forever):

- **0 RESET**
- **1 NMI**
- **2 IRQ**
- **3 PAGEFAULT**
- **4 SYSCALL**
- **5 ILLEGAL**
- **6 BREAKPOINT**
- **7 DEBUG** (reserved; optional implementation)

------

## Trap and interrupt entry with save-always frames

### Entry rules

On any exception/interrupt/trap:

1. Switch to **K privilege**.
2. Switch to **M2 mode** for handler execution.
3. Switch to **kernel stack** (see KSP).
4. Push the **full trap frame** (save-always).
5. Vector to handler `*(VBAR + 4 \cdot vector_index)`.

### Kernel stack pointer

To keep this implementable and debuggable, define **two stack pointers** architecturally in M2:

- **USP:** user stack pointer (used in U)
- **KSP:** kernel stack pointer (used in K)

`SP` refers to the active one based on privilege. On trap entry from U, the CPU switches from USP to KSP before pushing the frame.

(If you prefer to avoid new registers in the visible set, implement USP/KSP as banked SP values; architecturally the behavior must match.)

### Trap frame layout (fixed, save-always)

On entry, push in this exact order (lowest address last if stack grows down—what matters is the logical order is fixed and documented):

1. **Saved PC (32)**
2. **Saved P (32)**
3. **Saved mode+priv (32)**
   - includes prior M0/M1/M2 and prior U/K, plus reserved bits
4. **Saved FAR (32)**
   - meaningful for PAGEFAULT; otherwise implementation-defined (recommend 0)
5. **Saved FSC (32)**
   - meaningful for PAGEFAULT; otherwise implementation-defined (recommend 0)
6. **Saved A (32)**
7. **Saved X (32)**
8. **Saved Y (32)**
9. **Saved R0–R7 (32 each)**

This is intentionally “too much” in order to be **always correct**, always unwindable, always inspectable.

### Return from exception

`RTE` restores:

- R0–R7, A, X, Y
- FAR/FSC restoration is optional (recommend ignore on restore)
- prior mode/priv
- P, PC
- the appropriate SP (USP or KSP) based on restored privilege

------

## Privileged instruction boundaries

Must trap in U:

- writes to **CR0, VBAR, PTBR, ASID, FAR, FSC**
- `RTE`
- TLB invalidate operations (if present)
- interrupt mask manipulation (if privileged)
- mode switching (M0/M1/M2)
- I/O operations (if privileged model exists)

Unprivileged and defined:

- `SYS imm8` (SYSCALL vector; `imm8` is a syscall class)

------

# ABI v1 spec for 65832 mode v1.0

ABI v1 remains as previously drafted; no changes required for VBAR/NX/save-always, since those are privileged/kernel concerns. Re-stating the locked choices for clarity:

- **Data model:** ILP32, 32-bit pointers, little-endian
- **Args:** R0–R3, then stack
- **Return:** R0 (and R1 for 64-bit)
- **Caller-saved:** A/X/Y, R0–R3
- **Callee-saved:** R4–R7 (R7 is canonical FP when used)
- **Stack alignment:** 16-byte
- **Syscalls:** `SYS imm8`, R0=syscall#, R1–R3 args, stack spill, Carry indicates error

------

## One small follow-up decision worth locking now

### Page table placement: physical vs virtual pointer in PTBR

For Linux-friendliness and clean MMU reasoning, I recommend:

- **PTBR holds a physical address** of the L1 table.

Confirm that, and I’ll finalize v0.2 text with a single unambiguous sentence (“PTBR is physical; page table walks are physical; tables must be mapped for kernel access separately if desired”).

# Privileged architecture spec v0.3 for 65832

This revision locks **PTBR as a physical address**, and defines **reset + ROM mapping** in a way that preserves 65xx “personality” while remaining clean under paging and NX. It also bakes in the two implementation notes as *non-normative guidance* so you can “cheat” in the emulator without breaking the architectural contract.

------

## Paging model and PTBR

### PTBR definition

- **PTBR holds a physical address** of the L1 page table.
- **Page table walks use physical memory** and are not subject to translation.
- The kernel may also map page tables into virtual memory for convenience, but that mapping is optional and OS-defined.

### Two non-normative implementation notes

- **Sparse host mappings:** An emulator may implement physical memory using 64-bit host pointers and sparse mappings while exposing the exact same two-level translation semantics architecturally.
- **Compatibility address formation:** M0/M1 effective addresses may be formed per legacy rules and then treated as the *virtual address input* to the M2 MMU (compat VA → MMU translation → PA).

------

## ROM, vectors, and reset behavior

This section defines what the machine does at “power on,” and how legacy ROM behavior is preserved without infecting M2.

------

## Reset types

### Hard reset

Represents power-on or a full system reset.

Hard reset sets:

- **Privilege:** K
- **Mode:** M2 (handlers always run in M2)
- **CR0.PG:** 0 (paging disabled)
- **CR0.NXE:** 0 (NX disabled during earliest bring-up)
- **VBAR:** implementation-defined default (see below)
- **PC:** loaded from the **RESET vector** (see vector rules)
- **Registers:** architecturally undefined unless specified here; recommend zeroing in emulator for determinism (non-normative)

Rationale: you get a reliable bootstrap path even if RAM is uninitialized and page tables don’t exist yet.

### Soft reset

Represents a restart initiated by software (e.g., watchdog, kernel request).

Soft reset sets:

- **Privilege:** K
- **Mode:** M2
- **CR0.PG:** unchanged *or* cleared (choose one policy in your platform; recommendation below)
- **CR0.NXE:** unchanged *or* cleared (same)
- **VBAR:** unchanged
- **PC:** loaded from RESET vector

**Recommendation (platform-friendly):** soft reset clears **PG** and **NXE** (like hard reset) unless you have a strong reason to preserve them. It makes early bring-up and debugging dramatically easier.

------

## Vectoring on reset with VBAR

### VBAR initialization

Because VBAR is a system register but you need *somewhere* to fetch the first instruction from, define a deterministic default:

- On **hard reset**, **VBAR is set to `0x00000000`**.
- On **soft reset**, VBAR remains unchanged (unless you choose “soft reset acts like hard reset,” which is also defensible—pick one and we’ll freeze it).

### RESET vector fetch

- The CPU reads the 32-bit handler address at:
  - `VBAR + 4 \cdot RESET_INDEX` where `RESET_INDEX = 0`
- With **PG=0**, this is an identity-mapped physical fetch from address `0x00000000`.

That implies you need “something ROM-like” visible at physical 0 during the earliest phase.

------

# ROM mapping model

The machine needs three things simultaneously:

1. **Faithful Apple II family ROM behavior** in M0/M1 contexts.
2. A **clean, OS-friendly** bootstrap in M2.
3. A scheme that works great in an emulator with pluggable ROM images.

The clean answer is: **a small immutable Boot ROM plus optional Compatibility ROM windows**, each mapped by *mode* and *a compatibility control register*.

------

## ROM regions

### Boot ROM

- A small ROM region that is **always available at hard reset**, with paging disabled.
- Contains:
  - minimal initialization
  - ROM selection / personality policy
  - optional monitor / loader
  - the ability to jump into an OS loader in RAM

**Architectural requirement:** The system must expose a readable, executable ROM at physical address range starting at `0x00000000` sufficient to hold the initial vector table and bootstrap code.

**Suggested size:** 64 KB (simple), or 256 KB if you want room for multiple personalities and tooling.

### Compatibility ROM window

A mode-dependent ROM mapping that emulates the expected Apple II-family ROM visibility when executing M0/M1 code.

The compatibility ROM is *not* required to be physically present in one fixed place; it can be a mapping policy implemented by the emulator/chipset.

------

## Personality and ROM selection

Introduce a K-only system register:

### COMPATID system register (K-only)

Selects which legacy personality is active when in M0/M1:

- **0:** none (no compat ROM mapping; M0/M1 still exist but are “bare”)
- **1:** Apple IIe enhanced ROM personality
- **2:** Apple IIc ROM personality
- **3:** Apple IIGS ROM personality (for M1; M0 may still map IIe semantics, platform-defined)
- **4–255:** reserved

You can rename this later (`ROMSEL`, `PERS`, etc.). The important part is: *it’s explicit and kernel-controlled*.

------

## ROM mapping rules by mode

### M2 mode ROM mapping

- M2 is a modern protected environment: ROM visibility is a **platform policy**, not a legacy quirk.
- After reset bootstrap, the kernel may:
  - keep ROM mapped read/execute for diagnostics
  - unmap it entirely
  - map it read-only at a high address

**Strong recommendation:** once the kernel enables paging, map Boot ROM **read-only + executable**, and mark it **not writable**, and optionally **not user-accessible**.

### M1 mode ROM mapping

When executing M1 code and `COMPATID != 0`, the compatibility ROM window behaves as expected for that personality (vectors, firmware entry points, I/O expectations), with the important twist:

- The effective address produced by 65816 semantics becomes the **virtual address** input to the M2 MMU translation (compat VA → MMU → PA).

This allows the kernel to sandbox old ROM-based environments.

### M0 mode ROM mapping

Same concept as M1, but with 65C02 address formation.

------

## ROM and NX

- ROM pages must be mapped **X=1** to be executable.
- Under **NXE=1**, any attempt to execute from ROM-mapped pages with **X=0** must fault (exec-violation).
- Compatibility ROM should typically be **U=0** unless you explicitly want user tasks to execute ROM (many classic environments would; decide per OS policy).

------

# Reset flow, concretely

This is the “first instruction” story you can build around.

### Hard reset flow

1. CPU comes up **K + M2**, **PG=0**, **NXE=0**, **VBAR=0**.
2. CPU reads RESET vector from physical `0x00000000` and jumps there.
3. Boot ROM code runs:
   - initializes minimal hardware state
   - selects `COMPATID` default (likely 0)
   - optionally probes for a cartridge/ROM pack (emulator config)
   - loads/locates stage-2 loader
4. Boot ROM enables paging by:
   - building page tables somewhere in RAM
   - setting **PTBR** (physical)
   - setting **VBAR** to the kernel’s vector page
   - setting **CR0.PG=1**
   - setting **CR0.NXE=1**
5. Jump to kernel entry point (still K + M2).

This gives you a crisp “security flips on after we have tables” moment.

### Soft reset flow

- Jumps through RESET vector again, usually re-entering Boot ROM.
- If you choose to clear PG/NXE on soft reset, it behaves like a controlled reboot into stage-1.

------

# Things we should lock next

To make ROM mapping unambiguous (and implementation-friendly), we should freeze:

1. **Boot ROM size and address range** at physical zero (e.g., 64 KB @ `0x00000000–0x0000FFFF`).
2. **Where RAM begins** in physical space on a minimal system (e.g., RAM from `0x00010000` upward).
3. **Whether soft reset clears PG/NXE** (my recommendation: yes).
4. **Whether VBAR resets to 0 on soft reset** (my recommendation: no—leave it unchanged unless you want “soft reset equals hard reset”).

If you answer just #1 and #3, I can write the final frozen v0.3 text with exact addresses and reset policies, and we can then define the exact compatibility ROM windows for IIe/IIc/IIGS personalities.

# ROM placement decision for a 256 KB Boot ROM

Putting ROM “in high memory” is aesthetically faithful to the IIe/IIgs *and* ergonomically nice once you have paging—but it’s a bad fit for **first instruction after hard reset** unless you also invent a reset-time mapping trick.

So the cleanest, most teachable, most debuggable answer is:

- **Physically map the 256 KB Boot ROM at `0x00000000–0x0003FFFF` for hard reset.**
- **Also provide an optional fixed mirror of that same ROM at the top of the 32-bit space** (a “high-ROM alias”) so the modern OS world can treat ROM like a BIOS region and you get that classic “ROM up high” vibe once you’re running M2 with paging.

This gives you the best of both worlds: *simple reset semantics* and a *high-memory ROM identity* that feels historically resonant.

------

# Privileged architecture spec v0.4 deltas

## Physical memory map baseline

### Boot ROM

- **Boot ROM size:** 256 KB
- **Boot ROM physical base:** `0x00000000`
- **Boot ROM physical range:** `0x00000000–0x0003FFFF`

### High-ROM alias

- The same Boot ROM contents are also exposed at a fixed **alias** at the top of 4 GB:
  - **Alias range:** `0xFFFC0000–0xFFFFFFFF` (256 KB)

This alias is optional to *use* but not optional to *exist* if you want stable conventions for tooling and OSes.

### RAM base

- **RAM begins at:** `0x00040000`

That makes the early physical map easy to explain (ROM then RAM), and it avoids weird overlaps when you’re debugging bring-up dumps.

------

## Reset behavior with your choices locked

### Hard reset

- **Mode:** M2
- **Privilege:** K
- **CR0.PG:** 0
- **CR0.NXE:** 0
- **VBAR:** `0x00000000`
- **PC:** loaded from RESET vector at `VBAR + 0`

This guarantees the first fetch lands in Boot ROM without any special casing.

### Soft reset

- **Mode:** M2
- **Privilege:** K
- **CR0.PG:** 0 (**cleared**)
- **CR0.NXE:** 0 (**cleared**)
- **VBAR:** unchanged
- **PC:** loaded from RESET vector using current VBAR

This gives you the “reboot without amnesia” feel while still force-disabling paging/NX for deterministic recovery.

------

# ROM personalities and legacy mapping

## What Boot ROM owns vs what the compatibility ROM owns

- **Boot ROM** is *your* platform firmware: reset, loader, diagnostics, ROM selection, and the policy for mapping personalities.
- **Compatibility ROMs** are *payloads* selected by `COMPATID` and only become visible according to the compatibility mapping rules when executing M0/M1 contexts.

## Next lock to make personality mapping crisp

To avoid ambiguity later, we should define **compatibility windows** as explicit *virtual address regions* that the kernel may choose to map for a compat task. Concretely:

- **IIe enhanced personality:** map ROM into the classic high region that 6502 code expects (top of the 64 KB view).
- **IIc personality:** same idea, with its ROM image and any expected soft-switch behavior.
- **IIGS personality:** define the expected ROM visibility for M1 semantics (and decide what parts are “ROM” vs “toolbox stubs” vs “devices”).

You don’t need to finalize the exact Apple II addresses in the CPU spec itself if you’d rather keep them in a “chipset/personality spec,” but you *do* want the CPU spec to state that:

- personality ROM mapping is **kernel-controlled**
- implemented as **address-formation → MMU translation** (your compatibility note)
- and can be sandboxed with U/K and NX rules

------

# Why this matches your design philosophy

- **Elegant:** reset path is trivial—vectors at zero, no special “reset MMU.”
- **Teachable:** “ROM at 0, RAM after it” is the simplest mental model.
- **Debuggable:** memory dumps and early bring-up are obvious.
- **Fun:** the high-ROM alias lets you keep the classic “ROM lives up high” story once the modern system is online.
- **Secure:** NX flips on as soon as the kernel enables paging; the alias doesn’t weaken that.

------

# Two small follow-ups to finalize the spec text

1. Do you want the **high-ROM alias to remain accessible when paging is on**, or should the kernel explicitly map it (recommended: *kernel maps it read/exec, supervisor-only*)?
2. Should Boot ROM expose a standardized **handoff structure** in RAM (e.g., at `0x00040000`) containing `COMPATID`, memory size, and boot device info, so both your native OS and a Linux port can use the same entry contract?

# Compatibility windows, 4 KB paging, and the Apple II ROM/I/O split

You’ve put your finger on the exact place where “real protection” meets “Apple II expectations.” The good news: **4 KB pages don’t break this**, as long as you treat Apple II memory layout as a **compatibility view**, not as your native architectural truth.

The key is that in an Apple II personality, the classic regions:

- **`$0000–$BFFF`** RAM
- **`$C000–$CFFF`** I/O “soft switches” + device space
- **`$D000–$FFFF`** ROM

…are **virtual addresses within the compat task’s address space**, and your kernel is free to map those ranges to whatever backing objects it wants: RAM pages, device pages, ROM pages, or trap pages.

------

# The one real wrinkle with 4 KB pages

The Apple II regions align surprisingly well to 4 KB boundaries:

- `$C000–$CFFF` is exactly **4 KB** (one page)
- `$D000–$DFFF`, `$E000–$EFFF`, `$F000–$FFFF` are each **4 KB** pages
- The whole `$D000–$FFFF` ROM region is **12 pages = 48 KB**

So purely from an alignment standpoint, you’re fine.

The *actual* wrinkle is semantic: Apple II “I/O” is not normal memory.

- Reads/writes have side effects (soft switches)
- Some addresses look like ROM or empty space but are actually device behavior
- In a multi-VM world, you must ensure **device state is per-guest**, not global

That pushes you toward a clean “device page” abstraction rather than trying to pretend it’s RAM.

------

# Kernel-controlled compatibility mapping model v1

You asked to lock these principles in, so here they are as normative rules for the platform layer (chipset/personality spec), compatible with the CPU spec.

## Locked principles

- **Compat ROMs are payloads** selected by `COMPATID`.
- **Compatibility windows are explicit virtual regions** the kernel maps per compat task.
- **Personality mapping is kernel-controlled** (user code cannot change it).
- **Legacy address formation → MMU translation → PA** is the model.
- **Sandboxable with U/K and NX**.

This is also exactly what you want if you later add “hypervisor services,” because it makes “an Apple IIe” a *memory map + device set + ROM payload*, not a magical CPU mode.

------

# Personality window layout for an Apple IIe-style compat task

Define, for M0 Apple IIe personality, a **compat VA window** of exactly 64 KB:

- **Compat window base:** `COMPAT_BASE` (chosen by kernel, 4 KB aligned)
- **Compat VA:** `COMPAT_BASE + 0x0000 .. COMPAT_BASE + 0xFFFF`

Within that window, the kernel maps:

## RAM region

- **`+0x0000 .. +0xBFFF`** → guest RAM backing pages
  - Typical permissions: **U=1, R=1, W=1, X=1** (yes, classic code executes from RAM)
  - If you want better security, you can do W^X for native tasks; for Apple II guests, execute-from-RAM is part of the culture.

## I/O region

- **`+0xC000 .. +0xCFFF`** → guest device page(s)
  - Backed by an MMIO handler or “device memory object”
  - Typical permissions: **U=1, R=1, W=1, X=0**
  - With **NXE=1**, attempts to execute from `$Cxxx` fault—great for catching weirdness early.

## ROM region

- **`+0xD000 .. +0xFFFF`** → ROM payload pages
  - Typical permissions: **U=1, R=1, W=0, X=1**
  - This cleanly matches expectation: writable I/O below, immutable ROM above.

This maps perfectly onto 4 KB pages, and it’s per-task/per-guest because the kernel chooses the backing objects.

------

# “Writable to user mode” I/O and security

Allowing **U-mode writes to the I/O page** is not a security bug by itself—it’s just a policy choice for that *compat guest*. The isolation boundary is:

- The guest can scribble on its own MMIO page
- The MMIO page’s handler only mutates that guest’s device state
- The guest cannot access kernel mappings or other guests’ pages

So the real security work is:

- **per-guest device instances**
- and **no shared writable mappings between guests**

NX makes this nicer because you also block “execute-from-I/O” accidents and some classes of exploit primitives.

------

# Hypervisor-style “army of Apple IIe’s” without new CPU modes

You don’t need a new privilege ring to do this. You can get 90% of the “hypervisor feel” by defining a *kernel service model*:

- **Each guest = a compat task** with:
  - its own page tables / ASID (when you implement ASID)
  - its own ROM payload mapping
  - its own device pages (C000 page and optional slot pages)
  - its own event sources (timer/IRQ virtualization)

The CPU spec already supports this well because:

- traps enter **M2 K**
- you have **save-always frames**
- `SYS` gives you a clean para-virtual channel if you ever want it
- the MMU gives you hard isolation

If you later want “true hypervisor services,” the least invasive extension is a third privilege level (H) *or* a virtualization extension bitset—but you can postpone that and still run many guests convincingly.

------

# High-ROM alias policy locked

## High-ROM alias mapping

- The high-ROM alias exists physically, but when paging is enabled:
  - The **kernel explicitly maps** `0xFFFC0000–0xFFFFFFFF` as:
    - **U=0, R=1, W=0, X=1**
- User mode cannot directly fetch from or read it unless you decide otherwise (you shouldn’t).

This gives you the “ROM up high” convention without exposing firmware to userland.

------

# Boot handoff structure v1

A handoff structure is a great way to keep Boot ROM, your native OS, and a Linux port all speaking the same language.

## Location and discovery

- Boot ROM places a handoff block at a fixed **physical RAM address**:
  - **`0x00040000`** (first RAM page after ROM) is a good default
- Boot ROM passes its **physical address** in **R0** at kernel entry (simple and friendly)
- The kernel may remap it later, but early boot can treat it as identity-mapped with PG=0.

## Contents (minimum)

- **Magic + version**
- **Detected RAM size / memory map**
- **Boot device / boot arguments**
- **Selected `COMPATID` default**
- **ROM payload inventory** (which compat ROMs are present, hashes, sizes)
- **Framebuffer/console hints** (optional)

If you want, I can write the exact C struct layout with padding/alignment rules next.

------

# Two concrete spec decisions to lock next

1. **Compat base address policy:** do we standardize a default `COMPAT_BASE` (e.g., `0x01000000`) for convenience, or leave it fully OS-defined?
2. **Device-page model:** do you want `$C000–$CFFF` to be:
   - **Option A:** a single “device page” with sub-decodes handled in software (simplest), or
   - **Option B:** split into multiple device pages as needed (more flexible, more page-table churn)?

Pick A or B and I’ll fold it into the frozen v0.4 text, plus the handoff struct definition.

# Device-page model choice across IIe, IIc, IIgs, and your 65832 machine

Your instinct is right: if you want to support **IIe/IIc**, **IIgs**, and a future “fruit machine” personality, you don’t want to hardcode “one special $C000 page” into the CPU. You want a **platform/chipset abstraction** that *can* emulate the Apple II single-page I/O model, but is not constrained by it.

The good news: the historic Apple II map already suggests the pattern: `$C000–$C0FF` soft switches, `$C100–$C7FF` peripheral/firmware, `$C800–$CFFF` expansion/“card” space  [kreativekorp.com](https://www.kreativekorp.com/miscpages/a2info/memorymap.shtml). That’s conceptually *one 4 KB region with multiple subregions*, which maps cleanly to your “device page object” approach.

## Recommendation

Choose **Option A as the architectural primitive** (“one device page object for a compat window”), but implement it with **pluggable sub-decoders** so it behaves like Option B in practice. This is the rare case where “simpler primitive” actually scales better.

- **Architecturally (MMU/page tables):** map **one 4 KB I/O page** at the compat VA `$C000–$CFFF` region.
- **Platform personality layer:** dispatch reads/writes inside that page to a **device bus** (soft switches, slot ROM windows, IIgs-specific firmware windows, your own devices). This matches the way Apple II already partitions that region  [kreativekorp.com](https://www.kreativekorp.com/miscpages/a2info/memorymap.shtml).
- **Hypervisor-friendly:** each guest gets its **own I/O page object instance**, so `$C0xx` writes are isolated per-guest.

This keeps page tables stable (great for performance and debugging) while giving you unlimited internal complexity.

------

# Spec addendum: compat device page object

## Mapping rule

For any compat task that exposes an Apple II-family window, the kernel may map:

- **Compat I/O region:** `COMPAT_BASE + 0xC000 .. +0xCFFF`
- Backing: a **device-page object**, not RAM
- Suggested PTE perms: **U=1, R=1, W=1, X=0** (NX catches accidental execution)

## Subdecode inside the page

The device-page object must support at least these subregions (Apple II-inspired, but extensible):

- **`$C000–$C0FF`:** soft switches / status locations  [kreativekorp.com](https://www.kreativekorp.com/miscpages/a2info/memorymap.shtml)
- **`$C100–$C7FF`:** peripheral/firmware windows (slot or built-in)  [kreativekorp.com](https://www.kreativekorp.com/miscpages/a2info/memorymap.shtml)
- **`$C800–$CFFF`:** expansion “card” window / extended peripheral memory  [kreativekorp.com](https://www.kreativekorp.com/miscpages/a2info/memorymap.shtml)

Whether those areas return ROM bytes, device registers, or traps is personality-defined and kernel-controlled.

> This preserves the Apple II story, but your future machine can treat `$Cxxx` as a fully virtualized device bus.

------

# Hypervisor path without new CPU modes

With the device-page object model, “a small army of IIe’s” becomes:

- **Per-guest memory objects:** RAM pages + ROM payload pages + one I/O page object
- **Per-guest device state:** inside the I/O page object (and any mapped device regions)
- **Scheduling:** normal U-mode tasks
- **Traps:** handled by M2 K-mode as already specified

You can add “hypervisor services” later as an optimization layer (e.g., faster device calls), but the architectural contract stays clean.

------

# Boot handoff structure v1.0

You asked for a C# or Rust layout. Below are both, designed to be **stable, endian-safe, and versionable**.

## Semantics

- **Location:** physical RAM `0x00040000` (first RAM after Boot ROM).
- **Boot ROM passes physical pointer to the structure in `R0`** at kernel entry.
- **All integers little-endian.**
- **String fields are UTF-8, NUL-terminated where applicable.**
- **All offsets are relative to the start of the structure** (so the kernel can relocate/copy it safely).

------

## C# struct layout

```csharp
using System;
using System.Runtime.InteropServices;

public static class BootHandoff
{
    // "BMHO" = Bad Mango Handoff (pick your own magic)
    public const uint Magic = 0x4F484D42;

    public enum BootDeviceType : uint
    {
        Unknown = 0,
        Disk = 1,
        Network = 2,
        VirtualMedia = 3,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Header
    {
        public uint magic;            // Magic == Magic
        public ushort versionMajor;   // 1
        public ushort versionMinor;   // 0
        public uint totalSize;        // Total bytes of handoff blob (including variable areas)
        public uint flags;            // Bitfield (e.g., hasMemoryMap, hasRomInventory, etc.)

        public uint bootRomPhysBase;  // 0x00000000
        public uint bootRomSize;      // 256 * 1024
        public uint ramPhysBase;      // 0x00040000
        public uint ramSize;          // Total RAM bytes

        public uint compatIdDefault;  // 0 = none, 1 = IIe, 2 = IIc, 3 = IIGS, etc.

        public uint cmdlineOffset;    // Offset to UTF-8 NUL-terminated string, or 0
        public uint cmdlineLength;    // Including NUL if present, or 0

        public uint memMapOffset;     // Offset to MemoryMapEntry[memMapCount], or 0
        public uint memMapCount;

        public uint romInvOffset;     // Offset to RomInventoryEntry[romInvCount], or 0
        public uint romInvCount;

        public uint reserved0;
        public uint reserved1;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MemoryMapEntry
    {
        public uint physBase;
        public uint sizeBytes;
        public uint type;   // 1=RAM, 2=Reserved, 3=MMIO, 4=ROM, 5=Framebuffer, etc.
        public uint flags;  // cacheable, device, etc.
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RomInventoryEntry
    {
        public uint compatId;     // Which personality this ROM payload supports
        public uint imageOffset;  // Offset to ROM bytes (if embedded), else 0
        public uint imageSize;    // Bytes
        public uint sha256Offset; // Offset to 32-byte SHA-256, or 0
        public uint nameOffset;   // Offset to UTF-8 NUL-terminated name, or 0
    }
}
```

### Notes for C#

- `Pack = 1` gives you byte-accurate layout, but it’s slightly slower to access; that’s fine because this is early boot, not a hot path.
- Read it via `Span<byte>` + `MemoryMarshal.Read<T>` to avoid unsafe pointers.

------

## Rust struct layout

```rust
#[repr(C, packed)]
pub struct HandoffHeader {
    pub magic: u32,          // "BMHO"
    pub version_major: u16,  // 1
    pub version_minor: u16,  // 0
    pub total_size: u32,
    pub flags: u32,

    pub boot_rom_phys_base: u32, // 0x00000000
    pub boot_rom_size: u32,      // 256 KiB
    pub ram_phys_base: u32,      // 0x00040000
    pub ram_size: u32,

    pub compat_id_default: u32,

    pub cmdline_offset: u32,
    pub cmdline_length: u32,

    pub mem_map_offset: u32,
    pub mem_map_count: u32,

    pub rom_inv_offset: u32,
    pub rom_inv_count: u32,

    pub reserved0: u32,
    pub reserved1: u32,
}

#[repr(C, packed)]
pub struct MemoryMapEntry {
    pub phys_base: u32,
    pub size_bytes: u32,
    pub kind: u32,  // 1=RAM, 2=Reserved, 3=MMIO, 4=ROM, ...
    pub flags: u32,
}

#[repr(C, packed)]
pub struct RomInventoryEntry {
    pub compat_id: u32,
    pub image_offset: u32,
    pub image_size: u32,
    pub sha256_offset: u32, // points to 32 bytes
    pub name_offset: u32,   // points to UTF-8 NUL-terminated string
}
```

### Notes for Rust

- `#[repr(C, packed)]` avoids padding; accessing fields may require `read_unaligned`.
- In practice you’ll parse from a byte slice and avoid taking references to packed fields.

------

# Updated locked decisions

- **Compat base:** OS-defined (no architectural constant).
- **I/O model:** **one mapped 4 KB device-page object** per compat window, with internal subdecode; per-guest instancing for virtualization.
- **High-ROM alias:** kernel maps read/exec, supervisor-only (as stated).
- **Handoff:** fixed physical placement + pointer in `R0`.

------

# One last spec choice to make this airtight

Do you want the compat device page object to be represented in the MMU as:

1. **A normal PTE with a “device” attribute bit** (preferred: keeps it in the page table model), or
2. **A separate mapping mechanism outside the MMU** (simpler emulator bring-up, but less crisp spec-wise)?

If you pick (1), we’ll define one of the reserved PTE bits (8..11) as `DEV`, meaning “this page is handled by the device bus, not RAM.”

# Spec update: device pages via PTE attribute bit DEV

Option 1 is the cleanest because it keeps *everything*—RAM, ROM, MMIO, compat windows—inside one coherent mechanism: the MMU.

------

## Privileged architecture spec v0.4 deltas

### PTBR and page table walk

- **PTBR is a physical address** of the L1 table.
- **Page table walks are physical** (never translated).

### Boot ROM and RAM physical map

- **Boot ROM:** 256 KB at `0x00000000–0x0003FFFF`
- **RAM base:** `0x00040000`
- **High-ROM alias (physical):** `0xFFFC0000–0xFFFFFFFF`
- **Kernel policy:** when paging is enabled, the kernel maps the high-ROM alias **R=1, W=0, X=1, U=0**.

### Reset policy

- **Hard reset:** `PG=0`, `NXE=0`, `VBAR=0x00000000`
- **Soft reset:** **clears `PG` and `NXE`**, leaves **VBAR unchanged**

------

## PTE format update: DEV bit

The PTE reserved bits (8..11) are now partially defined.

### PTE bits (32-bit)

- **bit 0 P:** present
- **bit 1 R:** readable
- **bit 2 W:** writable
- **bit 3 X:** executable
- **bit 4 U:** user
- **bit 5 A:** accessed (optional)
- **bit 6 D:** dirty (optional)
- **bit 7 G:** global (optional)
- **bit 8 DEV:** **device page**
- **bits 9..11:** reserved (future attributes)
- **bits 12..31 PFN:** physical frame number

### DEV semantics

When **DEV=0**:

- the page is backed by normal physical memory at PFN.

When **DEV=1**:

- the page is backed by a **device mapping object** selected by PFN (PFN is interpreted as a *device object ID* or *device page handle*, platform-defined).
- **Reads/writes/fetches** are handled by the device layer, not RAM.

### DEV + permissions + NX

- Permission checks still apply first:
  - data reads require **R**
  - data writes require **W**
  - instruction fetch requires **X** and (if `NXE=1`) must have **X=1**
- If permission fails, raise **page fault** with the appropriate violation code.
- If permission succeeds and **DEV=1**, dispatch to the device object.

### DEV and caching

By default, **DEV pages are non-cacheable** (platform rule). If you later add cache attributes, DEV pages should default to “device/strongly ordered.”

------

## Compatibility I/O mapping rule (normative)

For Apple II-family compat personalities, the compat I/O region:

- `COMPAT_BASE + 0xC000 .. +0xCFFF`

must be mappable as:

- a **single 4 KB page** with **DEV=1**, **U=1**, **R=1**, **W=1**, **X=0** (recommended)

The internal subdecode of addresses within that page is personality-defined and per-guest.

------

# Device object ID encoding in PFN

To keep the spec implementable in both emulator and “hardware someday,” define PFN interpretation for DEV pages:

- If **DEV=1**, then **PFN is not a physical frame number**; it is a **Device Page ID**.
- Device Page ID namespace is **local to the current address space** (so guests can reuse IDs without conflict), unless the kernel chooses to globalize it.

This is hypervisor-friendly: each guest can have Device Page ID 0 mean “its own `$C000` page.”

If you’d rather make it global and tag with ASID later, that also works, but per-address-space is easier to reason about.

------

# Boot handoff structure v1.1 minor addition

To make DEV mappings easier for early boot OS code, add one field:

- **`devIdNamespaceHint`** (optional): tells the kernel whether the firmware set up device IDs per-ASID/per-address-space or globally (mostly useful if you ever pre-map devices before the kernel takes over).

If you don’t want that complexity now, skip it—the kernel can assume it owns device IDs from the moment it enables paging.

------

## Next thing to lock (so you can start coding confidently)

Do you want the **Device Page ID** (PFN when DEV=1) to be:

1. **A simple 20-bit ID** (fits existing PFN field; max 1,048,576 device pages), or
2. **A bitfield** (e.g., top bits = device class, low bits = instance/page), which makes debugging and tooling nicer?

If you pick **(2)**, I’ll propose a compact, future-proof bit allocation that still keeps the PTE 32-bit.

# Device Page ID bitfield encoding v1

Option 2 is the right call: it makes dumps readable, tools friendlier, and it gives you a natural seam for “many guests” without forcing you to redesign PTEs later.

## PTE recap for DEV pages

- **DEV=1** means **PFN is not a physical frame number**.
- **PFN[31:12]** is reinterpreted as a **Device Page ID**.

That gives you **20 bits** to work with.

------

## Device Page ID layout inside PFN[31:12]

Use a 3-part bitfield:

- **Class (4 bits):** what kind of device page this is
- **Instance (8 bits):** which device instance (per guest, per machine, etc.)
- **Page (8 bits):** which page within that instance

In other words (bits are within the 20-bit PFN payload):

- **PFN[19:16] = class**
- **PFN[15:8] = instance**
- **PFN[7:0] = page**

### Encoding helpers

- **DevicePageId = (class << 16) | (instance << 8) | page**
- Stored into PTE as: **PTE.PFN = DevicePageId**

### Capacity

- **16 classes**
- **256 instances per class**
- **256 pages per instance**
- Total addressable device pages: **1,048,576**

This is plenty, and it’s easy to eyeball in logs.

------

## Recommended class assignments

Keep a small, stable allocation. Don’t over-design; reserve space.

- **0x0:** Reserved (invalid / unmapped)
- **0x1:** Apple II-family compat I/O page (`$C000–$CFFF` device bus)
- **0x2:** Slot/expansion ROM windows (if you ever choose to model them as DEV-backed)
- **0x3:** Video/framebuffer aperture
- **0x4:** Storage controller MMIO
- **0x5:** Network controller MMIO
- **0x6:** Timer/interrupt controller MMIO
- **0x7:** Debug/semihosting console (optional)
- **0x8–0xF:** Reserved for future

You can refine later, but locking the idea of a “class nibble” is the win.

------

## Hypervisor-friendly semantics (without forcing H today)

### Per-address-space interpretation (default)

To make “an army of Apple IIe’s” natural:

- The **instance** field is interpreted **within the current address space context**.
- So guest A’s `(class=1, instance=0)` is a different object than guest B’s `(class=1, instance=0)`.

This keeps VM creation trivial: clone page tables + new device state, no global registry collisions.

### Optional global interpretation (reserved)

If/when you implement an H layer, you can permit a policy where:

- certain class/instance ranges are treated as global and mediated by H
- or H controls the mapping of device IDs to backing objects

You don’t need that now. You just need to not block it.

------

# Adding an H privilege ring as a reserved seam

You can absolutely reserve an H ring now without implementing it. The trick is: **define how it looks in the trap frame and privilege encoding**, but allow implementations to behave as if H == K until you turn it on.

## Privilege encoding update

Expand privilege from {U,K} to {U,K,H}:

- **U:** user
- **K:** kernel (guest kernel if virtualized)
- **H:** hypervisor (host kernel / VMM)

### Minimal contract (v0.x)

- If H is **unimplemented**, the CPU treats:
  - **H privilege as K privilege** for access checks
  - **H-only instructions** as illegal (or as K-only aliases). I recommend: **illegal** unless you’re in “virtualization enabled” mode.

This prevents “ghost features” from becoming relied-upon by accident.

## Trap entry with H reserved

- Traps still enter the “topmost implemented privilege.”
   In v0.x without H: they enter K (as we have).
   In a future v1.x with H: traps from a guest K could enter H depending on configuration.

## Trap frame field update

In the **Saved mode+priv** word:

- reserve bits for **prior privilege = 2 bits** (U=0, K=1, H=2, reserved=3)

That’s enough to make future virtualization non-breaking.

------

# DEV pages under virtualization later

When H exists, you’ll likely want:

- **Two-stage translation** (guest VA → guest PA → host PA), or
- **Stage-1 paging only + H-mediated memory objects**, depending on how ambitious you get

Your DEV encoding stays valid either way, because:

- it’s **a semantic mapping**, not a physical address
- H can virtualize the device registry cleanly

------

# One decision to lock next (so the emulator and OS code stay consistent)

## Fault code on DEV access to unmapped device IDs

If **DEV=1** and the Device Page ID does not resolve to a backing object, should it raise:

1. **not-present** (treat as missing mapping), or
2. **device-fault** (new FSC code, more explicit)

I recommend **(2)** because it’s gold for debugging and for sandboxing guests (“you touched a device you don’t have”), but (1) is simpler and more Linux-like.

# Spec update: explicit device-fault for DEV pages

Locked: if a DEV-backed mapping resolves to no device object (or the device signals an access failure), the CPU raises a **page fault** with a distinct **device-fault** status code. This is a big win for debugging, sandboxing, and hypervisor behavior.

------

## Privileged architecture spec v0.5 deltas

### PTE and DEV recap

- **PTE.bit8 = DEV**.
- If **DEV=1**, then **PTE.PFN[31:12]** is a **Device Page ID** (not a physical frame number).
- **Device Page ID bitfield (20 bits):**
  - **Class:** 4 bits
  - **Instance:** 8 bits
  - **Page:** 8 bits

### DEV permission checks are still first

Before any device dispatch:

- **Read:** requires R=1
- **Write:** requires W=1
- **Fetch:** requires X=1, and if **NXE=1** then X must be set or fault with exec-violation
- **Privilege:** U must be allowed if current privilege is U (U bit)

If these fail, raise the corresponding violation (read/write/exec/privilege), not device-fault.

------

## Fault status codes update

### New required FSC code

Add a new `FSC` code:

- **device-fault:** access targeted a DEV page whose Device Page ID could not be resolved, or the resolved device mapping rejected/failed the access.

This is distinct from not-present and from reserved-bit violations.

### FAR/FSC behavior

On a device-fault:

- **FAR** is set to the faulting virtual address.
- **FSC** is set to **device-fault**.

Optionally (recommended, but not required in v0.5), the platform may expose:

- **FDR (fault device register):** last Device Page ID involved
- **FIO (fault I/O info):** read/write/fetch + width

If you don’t want new system regs yet, you can place this info in the trap frame’s reserved slots later. For now, FAR+FSC is the required minimum.

------

## Hypervisor seam remains intact

This choice plays nicely with the reserved H ring:

- In a future virtualization-enabled build, **device-fault** becomes the clean signal that a guest attempted to touch a device it does not own (or a device the VMM wants to trap/emulate differently).
- It also avoids the ambiguity of “not-present,” which could mean memory bug *or* device policy.

------

# Small follow-up to freeze behavior

Should **device-fault** be raised only when the device ID is unmapped, or also when the device is mapped but the sub-decode rejects the access (e.g., invalid register offset, unsupported width)?

I recommend: **both**—treat it as “DEV access failed for device reasons,” not “device missing.” That keeps guest debugging straightforward (“you hit the device wrong”), and it’s also a clean trap signal for emulated slot cards with partial decode.

# Spec lock: `device-fault` covers missing devices and rejected accesses

Locked: **any failed DEV dispatch** raises a page fault with `FSC=device-fault`, whether the failure is because the **Device Page ID is unmapped** *or* because the mapped device **rejects the access** (invalid sub-address, unsupported width, illegal operation, etc.).

------

## Privileged architecture spec v0.6 deltas

### When `device-fault` is raised

If and only if:

1. **The PTE is valid and allows the access** (P=1 and permissions/privilege/NX checks pass), and
2. **DEV=1**, and
3. The device layer cannot complete the access because:
   - **No device object exists** for the Device Page ID, or
   - A device object exists but **rejects/fails** the operation (decode miss, illegal register, unsupported access width, device error, etc.)

Then:

- Raise **PAGEFAULT**
- Set **FAR = faulting virtual address**
- Set **FSC = device-fault**

### Precedence rules

- **Permission/privilege/NX violations win** over device-fault.
  - Example: DEV page has X=0 and code tries to execute there with NXE=1 → `exec-violation`, not `device-fault`.
- **Not-present wins** over device-fault.
  - If P=0 → `not-present`.

------

## Recommended (still optional) extra fault detail

To keep debugging excellent without bloating the base spec, the platform *may* expose either system regs or trap-frame fields for:

- **FDI (fault device id):** 20-bit Device Page ID (class/instance/page)
- **FOP (fault op):** read/write/fetch
- **FW (fault width):** 8/16/32 (and later 64 if you add it)

If you want, we can lock these as v0.7 *required* once you’ve decided whether you prefer “system registers” vs “extra trap frame words.”

------

## Next item that’s worth locking for implementation clarity

Since you’re writing the emulator in C# (and maybe Rust for hot loops), the most implementation-sensitive remaining ambiguity is:

- **Access widths and atomicity rules** for DEV pages (and for normal memory): what happens on 16/32-bit accesses to a DEV page when the device only meaningfully supports 8-bit registers?

If you tell me your preference (strict fault vs byte-split vs device-defined), I’ll freeze it into the spec in a way that stays teachable and hypervisor-friendly.

