# PRD: Keyboard System

| Field | Value |
|---|---|
| Status | Draft for engineering review |
| Owner | Josh |
| Tracking issue | #386 |
| Builds on | #331 / PR #375 (`HostKey`, `IKeyTranslator`, `AppleIIeKeyTranslator`, scheduler-driven `TypeString`). This spec renames `HostKey` to `KeyboardKey`. |
| Related | #333 (agent-friendly debugger), #337 (headless host), #330 and #369 (reset), #371 (Game I/O OR) |
| Supersedes | The *Implementation Architecture* section of [`specs/Keyboard Mapping Specification.md`](./specs/Keyboard%20Mapping%20Specification.md). Its key tables remain a reference, with corrections listed in §15. |

---

## 1. Summary

Define one keyboard architecture that serves every machine the core will host: the Apple IIe today; the Apple IIc, the Apple IIgs (65816), and the speculative 65832 machine later. It must also serve every input source: the Avalonia `VideoWindow`, the debugger REPL, scripts, and the future MCP/agent host.

The design has three layers:

1. **Input sources** only produce host-neutral events: a `KeyboardKey` going down or up, with a `KeyboardModifiers` snapshot, or plain text.
2. **`IKeyboardInput`**, one per machine, accepts those events from any thread. It applies them on the emulator thread through scheduler events, and owns text-typing timing.
3. **The machine's keyboard device**, chosen by the machine profile, turns `KeyboardKey` events into whatever its hardware presents to software. For the IIe and IIc that is an ASCII latch at `$C000`/`$C010` built on an `IKeyTranslator`. For the IIgs it is ADB key codes handled by an ADB controller.

No static or global state is allowed, so any number of machines can run in one process.

## 2. Problem

The current design is one Apple IIe-shaped `KeyboardDevice` that every caller drives directly with machine codes:

- **Host code chooses machine codes.** `VideoWindow` translates keys itself and calls `KeyDown(byte)`. The REPL `keyboard press $8D` and `ResetCommand` call the device directly. Each new input source (MCP, scripts) would need its own copy of the IIe rules.
- **There is only one keyboard model.** A IIc, IIgs, or 65832 keyboard cannot be swapped in, because callers assume `$C000` ASCII semantics.
- **Threading is per-method.** `KeyboardDevice` marshals each mutator through `IMachineDispatcher.Post`. Text typing used to schedule events from the REPL thread and advance on strobe clear; #375 fixed that inside the device, but it belongs in a shared layer.
- **Fidelity gaps.** There is no auto-repeat. The `$C010` read returns no ASCII bits. Caps Lock follows PC rules (`pc-toggle`); this spec defaults IIe profiles to real-IIe `hardware` mode instead (§8.1.7, Q1). The profile's `preset` and `autoRepeat` keyboard settings are ignored. See §8.1.
- **Determinism is not testable.** There is no record of when host input landed, so a run with keyboard input cannot be replayed.

## 3. Goals

| # | Goal | Acceptance |
|---|---|---|
| G1 | Input sources are machine-agnostic. | `VideoWindow`, the REPL, and the MCP host only use `KeyboardKey`, `KeyboardModifiers`, and text. None of them references a machine-specific type or code. |
| G2 | One input entry point per machine. | `machine.GetComponent<IKeyboardInput>()` returns the machine's input layer. Nothing outside a keyboard device calls `KeyDown(byte)`. |
| G3 | Emulator-thread application. | Every input event changes machine state only on the machine thread, at a scheduler event boundary, whichever thread it came from. |
| G4 | Deterministic, replayable input. | The same profile, initial state, and input journal produce an identical bus and keyboard trace. A golden-trace test enforces this. |
| G5 | Reliable text typing. | `TypeText` delivers every mappable character, in order, to a polling program. Each key has an observable key-up before the next one. Type-ahead is never overwritten. |
| G6 | Per-machine keyboards. | The profile selects `apple-iie`, `apple-iic`, `adb` (IIgs), or a 65832 model. Each keyboard owns its own translation and soft switches. |
| G7 | Layouts are configuration. | QWERTY and Dvorak (and later international layouts) are chosen in the profile, not in code paths in the window. |
| G8 | Multi-machine safe. | Two machines in one process type independently. Translators are stateless and shareable, and nothing is static. |
| G9 | Testable at every layer. | There are table tests per translator, `IKeyboardInput` tests with a fake scheduler, end-to-end polling-loop tests on real profiles, and golden traces. |

## 4. Non-goals (for v1)

- Implementing the IIc, IIgs/ADB, or 65832 keyboards. This PRD specifies their shape so the IIe work doesn't block them; each is its own epic.
- Implementing the IIe J16 keypad connector and the IIgs keypad hardware. `KeyboardKey` already includes the full keypad (§7.1); IIe and IIc translators return `null` for those keys until this work lands.
- Mouse and joystick input. These use separate input paths, which may follow the same `IKeyboardInput`-style pattern later.
- International character sets in the video ROM.
- Emulating the ADB microcontroller at the instruction level (M50740 firmware). The IIgs path is high-level emulation (HLE) first.
- Shipping keyboard ROM images. Layouts are C# code tables (see §11 and Q9).
- A key-binding editor UI. The JSON keymap and the REPL `keymap` commands are the v1 way to view and change mappings; the eventual UI can load, modify and save the same files.

## 5. Users and scenarios

| Persona | Scenario |
|---|---|
| Player | Types into Applesoft in the video window. Shift+1 gives `!`, and holding a key auto-repeats like a real IIe. |
| Developer at the REPL | `keyboard type "10 PRINT \"HI\"\r"` on a running machine; the line appears and runs. `keyboard press ctrl+reset` resets the machine. |
| Agent (MCP, #337) | Types `CATALOG` plus Return on a headless machine, runs until idle, and screen-captures the result. The same script gives the same trace on every run. |
| Emulator developer | Writes a test that boots a profile, types through `IKeyboardInput`, and asserts on RAM. No UI and no `$C000` pokes are involved. |
| Future IIgs work | Adds an `AdbKeyboard` and an ADB controller. Window, REPL, and MCP code are unchanged. |

## 6. Architecture

```mermaid
flowchart LR
  subgraph Host["Input sources (any thread)"]
    VW["VideoWindow<br/>(Avalonia Key)"]
    KM["IHostKeymap<br/>(JSON keymap)"]
    REPL["Debugger REPL<br/>keyboard press/type"]
    MCP["MCP / agent host<br/>(#333, #337)"]
    SCR["Scripts / tests"]
  end

  subgraph Machine["One machine instance"]
    IN["IKeyboardInput (KeyboardInput)<br/>queue + journal<br/>TypeText timing"]
    SCH[["Scheduler<br/>(DeferredWork / DeviceTimer)"]]
    subgraph KB["Keyboard device (chosen by profile)"]
      SINK["IHostKeyboardSink"]
      TR["IKeyTranslator<br/>(IIe/IIc only, layout from profile)"]
      SS["Soft switches<br/>$C000 / $C010 / PB0-PB2 / $C025..."]
    end
    GIO["GameIOController<br/>(PB0/PB1 OR, #371)"]
    SIG["SignalBus RESET"]
  end

  VW --> KM
  KM --> IN
  REPL --> IN
  MCP --> IN
  SCR --> IN
  IN -- "Post + schedule" --> SCH
  SCH -- "machine thread" --> SINK
  SINK --> TR
  SINK --> SS
  SS --> GIO
  SINK --> SIG
```

The layers have these responsibilities:

| Layer | Owns | Must not |
|---|---|---|
| Input source | Mapping its native key type to `KeyboardKey` through the JSON host keymap (§12), including Reset and Power, plus the modifier snapshot. Its Caps Lock toggle state. | Know any machine code, translator, or soft switch. The core never sees a host key type. |
| `IKeyboardInput` | Thread-safe intake, ordering, and hand-off to the emulator thread. `TypeText` (characters to keystrokes, timing). Filtering host auto-repeat. The input journal. | Know `$C000`, ASCII, or ADB. It asks the sink for keystrokes. |
| Keyboard device (sink) | Hardware semantics: translation, latch, strobe, any-key-down, auto-repeat, modifier soft switches, the RESET line, model-specific switches. | Be called from a host thread, or be called by anything other than its `IKeyboardInput` (except bus reads and writes). |

## 7. Contracts

All new types live in `BadMango.Emulator.Devices`. Interfaces go in `BadMango.Emulator.Devices.Interfaces`, following the existing convention. Nothing here references Avalonia.

### 7.1 `KeyboardKey` (renamed from `HostKey` in #375; expanded by the implementation of this spec)

`KeyboardKey` is the shared key set that all input sources produce and all keyboard devices consume, not the host's keys. It is modeled on the Apple Extended Keyboard II (M3501). Figure 8-10 of the *Guide to the Macintosh Family Hardware* [MacHW] is the reference for that layout and for the keys the keyboard actually has.

The app host (`VideoWindow`, the REPL, MCP, scripts, a headless runner) maps physical host keys to `KeyboardKey` through a JSON keymap (§12). The core never sees a host key type (Avalonia `Key`, a terminal code, or anything else). Each keyboard device, and its translator where it has one, interprets `KeyboardKey`.

PR #375 introduced this enum under the name `HostKey`. That name no longer fits: the set includes Reset and Power, which a typical host keyboard does not have, and the app host maps physical host keys onto this set through the keymap rather than treating the enum as "the host's keys." The implementation of this spec renames it to `KeyboardKey`. Its modifiers type is `KeyboardModifiers`.

`KeyboardKey` describes a **physical position** (or a dedicated machine-control key), not a character. Layout (Dvorak, international) is applied by the machine keyboard, never by the input source. Legends below are US-layout.

```csharp
public enum KeyboardKey
{
    None,

    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,

    // Digit row
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    Minus, Equals, Backtick,

    // Other punctuation
    OpenBracket, CloseBracket, Backslash,
    Semicolon, Quote,
    Comma, Period, Slash,

    // Editing and whitespace
    Space, Return, Tab, Escape,
    Backspace,       // main-keyboard delete (Mac "Delete", PC Backspace)
    ForwardDelete,   // dedicated forward delete (Mac "Forward Delete", PC Delete)

    // Function keys (Extended Keyboard II)
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12, F13, F14, F15,

    // Arrows and the navigation cluster
    Left, Right, Up, Down,
    Home, End, PageUp, PageDown,
    Help,            // host Insert maps to Help

    // Keypad (Extended Keyboard II)
    Keypad0, Keypad1, Keypad2, Keypad3, Keypad4,
    Keypad5, Keypad6, Keypad7, Keypad8, Keypad9,
    KeypadDecimal, KeypadEnter,
    KeypadPlus, KeypadMinus, KeypadMultiply, KeypadDivide,
    KeypadEquals, KeypadClear,

    // Modifiers
    LeftShift, RightShift,
    LeftControl, RightControl,
    LeftOption, RightOption,     // host Alt
    LeftCommand, RightCommand,   // host GUI / Windows / Command
    CapsLock,

    // Machine control
    Reset,
    Power,
}
```

Members that #375 already has keep their names, except: `Delete` is renamed to `ForwardDelete`, and `LeftAlt` / `RightAlt` are renamed to `LeftOption` / `RightOption`. The implementation issue (#388) expands the smaller #375 `HostKey` enum, renames it to `KeyboardKey`, and performs those member renames. Do not change #375 itself. `Reset` and `Power` are included even though a typical modern host keyboard has no Reset key, so a headless machine can be sent Reset (or Power) without a window.

- Every `KeyboardKey` needs an explicit mapping decision in every translator, including "this key produces no code." A coverage test fails if a `KeyboardKey` is added without a decision. Unmapped keys return `null`. They do not strobe and do not count toward AKD.
- **`Reset` is a `KeyboardKey`, not a separate input.** Hosts press it with `KeyDown(KeyboardKey.Reset, modifiers)` and `KeyUp(KeyboardKey.Reset, modifiers)`. IIe and IIc devices route it to the RESET signal path, not to the encoder: it never strobes, never counts toward AKD, and honors `requireControlForReset`. The IIgs device routes it the same way (the ADB Reset key). The earlier idea that F12 is not a `KeyboardKey` member and that Reset stays a separate input is withdrawn: `F12` is an ordinary `KeyboardKey`, and the F12 / Ctrl+F12 and Ctrl+Alt+F12 defaults are JSON keymap entries that map a host chord to `KeyboardKey.Reset` with the right modifiers, or to the cold-reset machine action (§12). Those defaults stay provisional. **Cold reset stays a machine action, not a `KeyboardKey`.**
- **`Power`** is the Extended Keyboard II / ADB power key. Machines that have no power key (the IIe and IIc) ignore it: the translator returns `null`, and it does not strobe or assert RESET. The IIgs may treat it as the ADB Power key when that work lands; until then it is reserved.

The default host-to-`KeyboardKey` map is the contents of the built-in `avalonia-default.json` keymap (§12). It is not a translator rule and is not hard-coded in `VideoWindow`.

### 7.2 `IKeyboardInput`

```csharp
public interface IKeyboardInput
{
    /// A KeyboardKey went down. modifiers is the full KeyboardModifiers snapshot
    /// (Shift, Control, OpenApple, ClosedApple, CapsLock) at the time of the event.
    void KeyDown(KeyboardKey key, KeyboardModifiers modifiers);

    /// A KeyboardKey went up. modifiers is the snapshot after the release.
    void KeyUp(KeyboardKey key, KeyboardModifiers modifiers);

    /// Queues text to be typed as keystrokes. Returns immediately with what was accepted.
    TypeTextReceipt TypeText(string text, TypeTextOptions? options = null);

    /// Drops queued text and releases a key that TypeText is holding.
    void CancelTyping();

    /// True while TypeText has queued or in-flight keystrokes. Thread-safe snapshot.
    bool IsTyping { get; }

    /// Input applied so far, with the cycle at which each event took effect (§7.5).
    IKeyboardInputJournal Journal { get; }
}
```

- **Modifiers travel as a snapshot on every event.** Modifier keys are also `KeyboardKey`s, so `KeyDown(LeftOption, OpenApple)` updates PB0 even though it produces no character. The snapshot is authoritative and avoids drift when the host misses an event, for example after a focus change. Caps Lock in the snapshot is the host's toggle state.
- **Mapping Option (host Alt) to the Apple keys is the input source's job.** It is a host binding. The current default is Left Option (Left Alt) for Open Apple and Right Option (Right Alt) for Closed Apple (Command and Option on the IIgs). Host Command / GUI keys arrive as `LeftCommand` / `RightCommand`; whether they also set the Apple-key snapshot is a host binding, and the default does not.
- **Host auto-repeat is filtered.** A `KeyDown` for a key that is already down is ignored, because the machine keyboard generates its own auto-repeat (§8.1.4). Sources must still send `KeyUp`.
- **Reset is a `KeyboardKey`.** `KeyDown(KeyboardKey.Reset, modifiers)` is how every host, including a headless runner, presses Reset. Whether the RESET line asserts depends on the device (Control required or not, §8.1.6). There is no separate `ResetKeyDown` / `ResetKeyUp` method.

### 7.3 Threading and scheduler semantics

1. **Any thread may call.** Calls append an `InputEvent` to a lock-protected FIFO in call order, then `IMachineDispatcher.Post` a drain action. If the machine is quiescent (paused, or stepped by a test), `Post` runs the drain inline before returning, which is the current dispatcher contract.
2. **The drain runs on the machine thread.** It schedules one `ScheduledEventKind.DeferredWork` event per drained batch, due at the next scheduler turn. That event applies the batch to the sink in FIFO order and stamps each event with `scheduler.Now`. Input is never applied in the middle of an instruction or bus access.
3. **Typing uses `DeviceTimer` events.** All `TypeText` steps (key down, key up, gap, strobe-wait re-checks) are `DeviceTimer` events scheduled by the input layer on the machine thread. The machine's RESET scheduler flush drops them. A typing-generation counter makes any survivors no-ops (§10.4).
4. **Strobe clear never starts the next key.** Software clearing the strobe only changes device state. The next typed key starts on its own scheduled event, after it checks the sink's `IsReadyForTypedKey`.
5. **Time is emulated time.** Durations in `TypeTextOptions` are milliseconds of emulated time, converted to cycles with the machine's CPU clock from the profile (`cpu.clockSpeed`). There is no hard-coded 1,020 cycles/ms, which is wrong for the IIgs and the 65832.

### 7.4 `IHostKeyboardSink` (machine side)

```csharp
public interface IHostKeyboardSink
{
    void OnHostKeyDown(KeyboardKey key, KeyboardModifiers modifiers);
    void OnHostKeyUp(KeyboardKey key, KeyboardModifiers modifiers);

    /// For TypeText: the keystroke that produces c on this keyboard and layout, if any.
    bool TryGetKeystroke(char c, out HostKeystroke keystroke);

    /// For TypeText: true when software has consumed the previous key (IIe: strobe clear).
    bool IsReadyForTypedKey { get; }

    /// Releases every key the input layer pressed (used by CancelTyping and RESET).
    void ReleaseAllTypedKeys();
}

public readonly record struct HostKeystroke(KeyboardKey Key, KeyboardModifiers Modifiers);
```

Every machine keyboard implements this. Only its `KeyboardInput` calls it, and only on the machine thread. That contract is the boundary between "host" and "hardware". `KeyboardKey.Reset` and `KeyboardKey.Power` arrive through `OnHostKeyDown` / `OnHostKeyUp`, not through a separate method. The device decides what they do (§7.1, §8.1.6, §8.3).

### 7.5 Determinism and the input journal

- Each applied event is journaled as `(Cycle, InputEvent)`. That includes key down, key up (including `KeyboardKey.Reset` and `KeyboardKey.Power`), every `TypeText` keystroke, and `CancelTyping`.
- **Determinism contract:** the same profile, the same initial machine state (RAM fill, any RNG seed), and the same journal replayed at the same cycles produce an identical trace. Live host input is not deterministic in *when* it lands, but once journaled it replays exactly.
- `KeyboardInput.Replay(IEnumerable<(Cycle, InputEvent)>)` schedules journaled events at their recorded cycles. This is used by golden-trace tests and by the agent host for reproducible sessions.
- When the machine is paused or stepped by a caller (tests, an agent between `run-until` calls), events apply at the current cycle. Scripted input is deterministic without a journal.

### 7.6 Multi-machine

- `KeyboardInput` and the keyboard device are per-machine components, created by `MachineFactory` or the profile builder and found through `GetComponent<T>()`.
- Translators are stateless and immutable, so one instance may be shared.
- No `static` mutable state is allowed anywhere in the keyboard path. One existing exception remains: `KeyboardDevice` uses `ResetSourceDeviceId = int.MinValue + 2` as a constant source ID, which is fine because it is per-signal-bus.
- A test boots two machines in one process, types into one, and asserts that the other's `$C000` is unchanged (§13).

## 8. Per-machine keyboards

### 8.1 Apple IIe: `AppleIIeKeyboard` (today's `KeyboardDevice`)

Hardware facts are cited to: **[TRM-e]** *Apple IIe Technical Reference Manual*, ch. 2 "The Keyboard" and ch. 4 "Reset routine"; and **[Sather]** Jim Sather, *Understanding the Apple IIe*, ch. 7 "Keyboard" and ch. 3, Figure 3.8.

#### 8.1.1 Encoder and translation

- The keyboard is a switch matrix scanned by an AY-3600-PRO encoder. Its key code, together with Shift and Control, addresses a 2K keyboard ROM that produces the ASCII code [TRM-e ch. 2; Sather ch. 7]. The ROM holds four sets: caps-locked or unlocked, standard or alternate. The alternate set is Dvorak on US machines and the local layout on export machines [Sather ch. 7].
- **Caps Lock** selects the caps-locked set. That set differs from the unlocked set only in that the unmodified ("ALONE") code for the 26 letters is uppercase. On a real IIe, Caps Lock with Shift still gives uppercase, and Caps Lock never affects digits or punctuation [Sather ch. 7]. IIe profiles default to that `hardware` behavior (§8.1.7, Q1). PR #375's translator still uses the PC rule (Shift XOR Caps Lock, `pc-toggle`); the default flips when the implementation of this spec lands.
- **Control** behavior follows the Control and Both columns of TRM-e Table 2-3:
  - Ctrl+A…Z gives `$01`–`$1A`.
  - Ctrl+`[`, Ctrl+`\` and Ctrl+`]` give `$1B`, `$1C` and `$1D`.
  - Ctrl+2 gives `$00` and Ctrl+6 gives `$1E`, with or without Shift. These keys exist so the keyboard can produce every code from `$00` to `$7F` [Sather ch. 7].
  - Ctrl+Minus gives `$1F`.
  - Control leaves every other key unchanged. For example, Ctrl+1 is `1` and Ctrl+Shift+1 is `!`.
- **Special keys** are the same in every column: Delete `$7F`, Left `$08`, Tab `$09`, Down `$0A`, Up `$0B`, Return `$0D`, Right `$15`, Esc `$1B`, Space `$20` [TRM-e Table 2-3]. Return and the arrows share codes with Ctrl-M, H, U, J and K [Sather ch. 7].
- The host's **Backspace** maps to `$08`, the IIe left arrow, which is what Apple II software treats as "back". **Delete** maps to `$7F`.
- **Open Apple and Closed Apple** are not part of the encoder. They drive PB0 and PB1 directly and never change the character code [TRM-e ch. 2, Sather ch. 7].

`AppleIIeKeyTranslator` (an `IKeyTranslator`) implements the table above for the US QWERTY layout. It is exhaustively table-tested since #375.

#### 8.1.2 Soft switches

| Address | Access | Behavior | Source |
|---|---|---|---|
| `$C000`–`$C00F` read | R | Bit 7 is the keyboard strobe (set by a key press or auto-repeat). Bits 0–6 hold the last key's ASCII, which persists after the strobe clears. | TRM-e ch. 2; Sather ch. 7, Keyboard Operational Summary |
| `$C010` read | R | Bit 7 is any-key-down (AKD). Bits 0–6 are the ASCII. Clears the strobe. | Sather ch. 7, Keyboard Operational Summary ("Read AKD and ASCII; reset KEYSTROBE") |
| `$C011`–`$C01F` read | R | IOU/MMU status in bit 7; ASCII in bits 0–6. | Sather ch. 7, Keyboard Operational Summary |
| `$C010`–`$C01F` write | W | Clears the strobe (any value). | Sather ch. 7, Keyboard Operational Summary ("W$C01X Reset KEYSTROBE") |
| `$C061`, `$C062` | R | PB0 (Open Apple) and PB1 (Closed Apple), bit 7. These are ORed with joystick buttons (#371). | TRM-e ch. 2 |
| `$C063` | R | PB2. Shows the Shift key only if the shift-key mod (X6 jumper) is installed. | TRM-e ch. 2 "Shift-Key Mod" |

- **AKD** is high while any matrix key is down. It excludes Reset, Control, Shift, Caps Lock and both Apple keys [TRM-e ch. 2]. With several keys held, AKD stays high until all are up.
- **Debounce:** ASCII and the strobe appear about 10 ms after the key goes down, while AKD rises at once [Sather ch. 7, Keyboard Operational Summary]. We model the strobe as immediate by default, and the profile can opt into a debounce delay (Q4).
- **Rollover:** a new key pressed while another is held is encoded and read normally [Sather ch. 7, Keyboard Operational Summary]. TRM-e describes this as both "N-key rollover" and "two-key rollover", so see Q3.

#### 8.1.3 The strobe and `TypeText` readiness

`IsReadyForTypedKey` is true when the strobe is clear, meaning software has consumed the last key. This is a convenience the real keyboard doesn't have (a real typist can overwrite an unread key), and it is configurable through `TypeTextOptions.WaitForConsumption`.

#### 8.1.4 Auto-repeat

When a key is held for 534–801 ms, the IOU begins generating auto-repeat strobes at 15 Hz. The exact delay depends on the phase of the flash counter. Both the delay (32–48 vertical scans) and the rate (vertical frequency ÷ 4) derive from the video vertical scan [Sather ch. 3, Figure 3.8; ch. 7, Keyboard Operational Summary]. Pressing any matrix key restarts the delay [Sather ch. 7, Keyboard Operational Summary]. *Inside the Apple IIe* gives about 900 ms and 15 per second, so we use Sather's derivation, which is tied to the scan count.

The emulation counts VBlank events from the video device. On the 4th VBlank boundary after 32–48 scans, it sets the strobe again with the same ASCII while the key is held. The profile's `autoRepeat` flag enables this, and it already exists in the a2 profiles. `TypeText` hold times (50 ms by default) are far below the repeat delay.

#### 8.1.5 Shift-key mod (PB2)

On the IIe, PB2 reflects Shift only when the X6 jumper is soldered (the "single-wire shift-key mod"). Without it, PB2 is the game-port switch 2 input [TRM-e ch. 2]. A per-device profile option, `shiftKeyMod`, gates this. It defaults to off, matching a stock IIe with no X6 jumper. This is a behavior change: today's device always reports Shift on PB2.

#### 8.1.6 Reset combinations

- **`KeyboardKey.Reset`** is routed to the RESET signal path, not to the encoder. It never latches a code, never sets the strobe, and never counts toward AKD. The translator returns `null` for it. RESET pulls low only when Control is in the modifier snapshot, unless the keyboard jumpers are changed so Reset alone works [Sather ch. 7]. That jumper is the profile option `requireControlForReset`, the existing `RequireControlForReset`. A headless machine therefore presses Reset with `KeyDown(KeyboardKey.Reset, KeyboardModifiers.Control)`. With `requireControlForReset` true, `KeyDown(KeyboardKey.Reset, KeyboardModifiers.None)` asserts nothing and does not strobe.
- **Ctrl+Open Apple+Reset:** the firmware does a forced cold start. It writes two bytes into each page of main RAM, which destroys the reset vector, and then cold-starts [TRM-e ch. 4 "Forced Cold Start"].
- **Ctrl+Closed Apple+Reset,** with or without Open Apple: the firmware runs the built-in self-test [TRM-e ch. 4].
- These are firmware behaviors. The keyboard only has to hold PB0/PB1 while RESET is released, which already works through #369 (`reset --open-apple` / `--closed-apple`) and the #371 OR.

#### 8.1.7 Layout and Caps Lock options

- **Layout:** `us-qwerty` (default) or `us-dvorak`, the IIe's alternate keyboard ROM set [Sather ch. 7]. The IIe Dvorak switch is a hardware mod [Sather application note], so the profile treats it as a fixed layout plus an optional runtime toggle.
- **Caps Lock mode:**
  - `hardware` (the IIe default): Caps Lock forces letters to uppercase, and Shift has no further effect on letters. This matches a real IIe [Sather ch. 7].
  - `pc-toggle`: Shift XOR Caps Lock, which is today's behavior in PR #375 and remains available as a per-profile option.

  IIe profiles default to `hardware`. This is a behavior change from today's Shift-XOR-Caps translator. Any profile can set `capsLockMode` to `pc-toggle`.

### 8.2 Apple IIc: `AppleIIcKeyboard`

Hardware facts are cited to **[TRM-c]** *Apple IIc Technical Reference Manual*, ch. 1, ch. 4 Table 4-1, Table B-7 and Appendix G.

- **It shares the IIe programming model.** `$C000` holds the data and strobe. `$C010` holds AKD and clears the strobe, and AKD excludes the Apple keys, Control, Shift, Caps Lock and Reset. Software must read the strobe before AKD [TRM-c ch. 4]. The IIc reuses the IIe translator rules through a shared base or composition.
- **The keyboard switch** selects one of two built-in layouts and the matching screen character sets. On US machines, up is QWERTY and down is Dvorak; on international machines it toggles the local layout and US [TRM-c ch. 1]. Software cannot read the switch, since it is handled in the keyboard circuitry [TRM-c ch. 1; RC-SE]. The device exposes it as `KeyboardLayoutSwitch { Primary, Alternate }`, settable from the profile and at runtime by the host.
- **The 80/40 switch** is read at `$C060` bit 7: 1 means the switch is down (40 columns) [TRM-c Table 4-1, Table B-7]. It is only a status bit, and software decides what to do with it. The device exposes `Is40ColumnSwitchDown`.
- **`$C063`** reads 1 in bit 7 when the mouse button is **not** pressed. That makes software looking for the shift-key mod find it [TRM-c Table B-7 notes]. This is the mouse device's switch, not the keyboard's.
- Auto-repeat timing, rollover and the Caps Lock+Shift rule on the IIc are unverified (Q6).

### 8.3 Apple IIgs: `AdbKeyboard` plus `AdbController`

Hardware facts are cited to **[HWR-gs]** *Apple IIgs Hardware Reference*, ch. 6 "The Apple Desktop Bus", and **[TN-58]** Apple IIgs Technical Note #58.

- **Transport:** the keyboard is an ADB device. ADB keyboard register 0 holds two key events. Each is a 7-bit key code plus a "key released" bit 7 [HWR-gs Table 6-10]. The keyboard device turns `KeyboardKey` into ADB key codes and up/down transitions. It does **not** produce ASCII, so it does not use `IKeyTranslator`.
- **Translation:** the ADB microcontroller (an M50740, a 6502-superset microcontroller) generates all 128 ASCII codes and fills the ADB GLU registers [HWR-gs ch. 6]. The emulator implements this as an HLE `AdbController` that applies the layout (selected in the Control Panel) to produce ASCII. It is a separate component from the keyboard, so mouse and ADB commands share it.
- **GLU registers:**

  | Register | Address | Notes |
  |---|---|---|
  | Keyboard Data | `$C000` | Bit 7 is the strobe. It is cleared by reading or writing any address from `$C010` to `$C01F`. |
  | Modifier Key | `$C025` | Bit 7 Command, 6 Option, 5 modifier latch updated with no key, 4 keypad key, 3 key held down (repeating), 2 Caps Lock, 1 Control, 0 Shift. |
  | Mouse Data | `$C024` | |
  | ADB Command/Data | `$C026` | |
  | ADB Status | `$C027` | Keyboard-data-full and keyboard-interrupt-enable bits, among others. |

  `$C010` still provides the AKD and strobe-clear compatibility [HWR-gs Tables 6-2 to 6-7].
- **Auto-repeat:** the rate and delay are set in the Control Panel [HWR-gs ch. 6].
- **Rollover:** modifier keys have n-key rollover. Alphanumeric keys have 2-key lockout, so a third simultaneous key is not recognized [HWR-gs ch. 6].
- **Command key** does the same job as Open Apple on the IIe [HWR-gs glossary]. Whether Option reads as Closed Apple at `$C062`, and how `$C061`/`$C062` are wired on the IIgs, is Q7.
- **Reset combinations** (official sources only; see also §12 for host keymap entries):
  - The ADB microcontroller detects the reset key combination and asserts RESET [HWR-gs ch. 6]. `OnHostKeyDown(KeyboardKey.Reset, modifiers)` maps onto that: the IIgs device routes `KeyboardKey.Reset` to the RESET signal path (the ADB Reset key), not to a typable ADB key-down in register 0. It does not strobe `$C000` as a character.
  - **Control-Command-Reset** is special: it generates a special key code, the microcontroller asserts the RESET line, and the system restarts (the reset routine) [HWR-gs ch. 6]. Bit 5 of the ADB Command/Data register at `$C026` is set when Control, Command, and Reset are pressed together; "this condition is usually used to initiate a cold start up" [HWR-gs Table 6-3]. Apple's Tech Info Library article on resetting calls this a cold reset: hold Control and Apple, then press and release Reset, "which makes the machine reboot as if it were just turned on" [TIL-2061]. The IIgs Quick Start Guide gives the same chord (Command-Control-Reset) to start up a disk when the power is already on [QSG-gs].
  - **Control-Reset** is a warm reset: hold Control and press and release Reset [TIL-2061]. The HWR-gs glossary defines Control-Reset as the combination that usually stops an Applesoft program immediately.
  - **Self-test:** "The self-test is activated by pressing Open-Apple and Option on power up, or Open-Apple, Option, and Reset" [TN-95]. That wording does **not** mention Control. This spec does not add Control to the official combination. Many secondary sources write Control-Open-Apple-Option-Reset; that has not been confirmed from an official Apple manual here.
  - **Control Panel:** the documented keyboard path that uses Reset is Option-Control-Reset [QSG-gs; TIL-522]. Holding Option while turning the power on is also documented, though Apple noted that it "doesn't always work" [TIL-522, citing the *Apple IIGS Owner's Guide*, p. 13]. Control-Open-Apple-Escape is widely cited as the running-machine Classic Desk Accessory / Control Panel shortcut; it was **not** independently verified from an official Apple manual for this spec.
- **Extended ADB keyboards and function keys.** The Apple Extended Keyboard (M0115) and Apple Extended Keyboard II (M3501) are ADB keyboards with fifteen function keys, F1–F15 [MacHW ch. 8 and Figure 8-10; TIL-112]. `KeyboardKey` already includes F1–F15, the navigation cluster, the full keypad, Help, Forward Delete, Command, Option, Reset and Power, using Figure 8-10 as the reference (§7.1). The Extended Keyboard II's stated requirement is "Any Apple personal computer with an Apple Desktop bus interface" [TIL-112], which includes the IIgs [HWR-gs ch. 6]. The M0115 is the same ADB device class [MacHW ch. 8]. The compact keyboard that shipped with the IIgs has no function keys; these extended keyboards are the ADB devices that add them. Contemporary IIgs programmers used an Extended ADB keyboard on the IIgs and read F1–F15 from software [Empson-94].
  - ADB key-down codes for F13, F14 and F15 are `$69`, `$6B` and `$71` [MacHW Figure 8-10]. Those three keys sit immediately to the right of F12 in the function-key row. A PC 101-key keyboard labels that same cluster Print Screen, Scroll Lock and Pause, so the host maps Print Screen → `KeyboardKey.F13`, Scroll Lock → `KeyboardKey.F14` and Pause → `KeyboardKey.F15`. That is a layout correspondence, not an Apple document that literally names those PC legends on the M0115/M3501.
  - **`KeyboardKey.Power`** is reserved on the IIgs until the ADB keyboard epic decides how to expose the Extended Keyboard II power key; it is not typable ASCII.
  - Software can see the function keys. Through the Event Manager, the extended keys arrive as key-down events whose character codes look like lowercase letters from the numeric keypad, distinguished by the keypad modifier bit (`keyPad`, `$2000`) [Empson-94]. Through `$C000`/`$C010`, the same keys are identified by reading the Modifier Key register at `$C025` and testing the keypad bit (bit 4) [Empson-94; HWR-gs Table 6-5]. A few programs use them; they are ordinary ADB keys, not reserved host shortcuts.
- **Known hardware anomaly:** while an arrow key (or Space or Delete with Fast Space/Delete on) auto-repeats, pressing Control sets the Shift bit in `$C025` instead of Control [TN-58]. The fidelity profile may reproduce this.

### 8.4 65832 machine (placeholder)

This machine defines its own keyboard and does not have to imitate the Apple II. Requirements on it:

- It implements `IHostKeyboardSink`, so window, REPL, MCP and journal support come for free.
- It is expected to expose raw `KeyboardKey`-derived scan codes, through a FIFO and an interrupt, rather than an ASCII latch.
- `TryGetKeystroke` can be implemented with a US layout table.
- Its register layout belongs in the 65832 architecture spec, not here.

## 9. `TypeText`

### 9.1 Characters to keystrokes

- Each character goes to `sink.TryGetKeystroke(c, out keystroke)`. For IIe and IIc keyboards, this is a reverse table built once per translator and layout by enumerating every `KeyboardKey` × {none, Shift, Control, Control+Shift}. Among the keystrokes that produce a code, the one with the fewest modifiers wins.
- Newlines: `\r`, `\n`, and `\r\n` each become **one** Return. `\t` becomes Tab, and `\b` becomes Backspace (`$08`).
- **Case** is typed exactly: `A` is typed as Shift+A. The host's live Caps Lock state is ignored, because `TypeText` uses its own modifier snapshot. `TypeTextOptions.ForceUppercase` upper-cases letters first, for software that needs uppercase. The keyboard-device option is `typeText.forceUppercase` in the machine profile's device configuration. The unenhanced `pocket2e-a2` profile defaults it to true, because Applesoft on an unenhanced IIe needs uppercase keywords. Every other profile defaults it to false. Any profile can set the option.
- **Unmappable characters** are anything with no keystroke, such as non-ASCII or emoji. `TypeTextOptions.Unmappable` decides what happens: `Skip` (the default), `Replace` (with `ReplacementChar`, default `?`), or `Reject` (nothing is queued and the receipt lists the offending characters). The receipt always reports accepted and skipped counts, so the REPL and MCP can surface them.

### 9.2 Timing (defaults)

| Step | Default | Notes |
|---|---|---|
| Hold | 50 ms | Key down to key up. This is the current `TypeString` default. |
| Gap | 10 ms (hold ÷ 5, minimum 1 ms) | Key up to the next key down. AKD is observably low in between. |
| Strobe wait poll | 1 ms | Used only when `WaitForConsumption` is set and the previous key is unread. |
| Wait for consumption | on | The next key waits until the sink reports it is ready (IIe: strobe clear). |
| Return extra delay | 0 ms | Optional `ReturnDelay`, for software that is slow after a line ends. |

Durations are emulated time (§7.3). Typing state is per machine. A new `TypeText` while typing is in progress appends to the queue.

### 9.3 Interaction with live keys

While `TypeText` is active, live `KeyDown` events still apply, but `TypeText` keystrokes use their own modifier snapshot. After typing finishes, the input layer restores the device's modifier state to the last live snapshot, so a held Open Apple stays visible on PB0.

## 10. Integration

### 10.1 `VideoWindow`

The window does not contain a switch or a hard-coded table from Avalonia `Key` to `KeyboardKey`. An Avalonia adapter in the UI project turns the Avalonia `Key` (and, when useful, the physical key or scan code) into the host-key names used in the JSON keymap (§12). The window then calls `IHostKeymap.TryMap` (and `TryMapAction` for cold reset). Ordinary keys become `IKeyboardInput.KeyDown` and `KeyUp` with the resulting `KeyboardKey` and a `KeyboardModifiers` snapshot. The window still keeps its Option/Alt-to-Apple-key snapshot and Caps Lock toggle. It never touches a translator or `KeyDown(byte)`. The core never sees Avalonia `Key`. (The #375 interim reads `IKeyboardDevice.Translator` and uses a code switch; both go away in the VideoWindow migration step.)

**Reset and cold-reset mappings** come from the resolved keymap (§12), not from hard-coded F12 chords and not from a `bindings` block in the profile. A Reset mapping is a keymap entry from a host chord to `KeyDown` / `KeyUp` of `KeyboardKey.Reset` with the modifiers the entry specifies (Control implied where the default says so). A cold-reset mapping is a keymap **action** entry (`coldReset`); it is not a `KeyboardKey` and does not also assert `KeyboardKey.Reset`. When a mapping consumes a host key (for example F12 in `avalonia-default.json`), that key is not also delivered as its ordinary `KeyboardKey` (it is not also `KeyboardKey.F12`). Defaults are machine-specific and **provisional** (§12). The menu command and the REPL `reset` command always work, independently of the keymap, by sending `KeyboardKey.Reset` themselves.

### 10.2 Debugger REPL

- `keyboard type <text>` calls `TypeText` and prints the receipt.
- `keyboard press <chord>` accepts chords such as `shift+1`, `ctrl+c`, `ctrl+reset`, `return` and `open-apple`. It does key down, holds for the default hold time, then key up.
- `keyboard press $8D` stays available as a debugging convenience. It maps the code back to a keystroke with `TryGetKeystroke` and fails clearly if there is none.
- `keyboard read` is unchanged.
- `reset` (#369) uses `KeyDown(KeyboardKey.Reset, modifiers)` and `KeyUp(KeyboardKey.Reset, modifiers)`, with Control and the Apple keys in the snapshot, instead of device methods.
- `keymap show|load|save|set|unset|reset` views and edits the resolved JSON keymap (§12.6).

### 10.3 MCP / agent host (#333, #337)

The agent tools are `keyboard.type(text, options)`, `keyboard.press(chord)` and `keyboard.key(down|up, key, modifiers)`. They are thin wrappers over `IKeyboardInput`. A headless runner presses Reset the same way as any other key: `KeyDown(KeyboardKey.Reset, modifiers)`. Because the agent normally drives a paused or `run-until` machine, input lands at known cycles (§7.5). The journal is exported with snapshots (#339) for reproducible sessions.

### 10.4 Reset (#330, #369)

When RESET asserts, the device clears the strobe and cancels typing. The input layer's typing generation increments, and any key held by `TypeText` is released. That matches today's `OnResetAsserted` behavior from #369. Live held keys and modifiers survive RESET, which already has a test. The machine's scheduler flush drops `DeviceTimer` typing events, and the generation counter makes any `DeferredWork` survivors no-ops.

### 10.5 Game I/O (#371)

`GameIOController` keeps ORing joystick buttons with `IKeyboardDevice.Modifiers` for PB0, PB1 and PB2. On the IIe, PB2 joins the OR only when `shiftKeyMod` is on. On the IIc, `$C063` is the mouse button (§8.2). On the IIgs, the wiring is open (Q7).

## 11. `IKeyTranslator` and layouts

- `IKeyTranslator.Translate(KeyboardKey, KeyboardModifiers) -> byte?` is a building block for keyboards that output **one code per keystroke** (the IIe and IIc). It is not a cross-machine contract. ADB and the 65832 machine do not use it.
- Implementations are stateless and shareable.
- **Layouts** are C# code tables (or separate translator classes), selected in the profile: `AppleIIeKeyTranslator` (US QWERTY) and, later, `AppleIIeDvorakTranslator`. The keyboard ROM is emulated in those tables. We will not ship keyboard ROM images. A ROM-image-driven translator is out of scope unless a real need appears later. Sather Table 7.2 documents the 2K IIe keyboard ROM layout (matrix position XY maps to ROM address `XY × 4` plus the mode and set bits) and is the reference for building the C# tables [Sather ch. 7].

### 11.1 Profile schema sketch

This extends the existing `motherboard[].config` for the keyboard. Existing keys are kept, and `preset` is mapped to `model`.

```jsonc
{
  "type": "keyboard",
  "name": "Keyboard",
  "enabled": true,
  "config": {
    "model": "apple-iie",              // apple-iie | apple-iic | adb | (65832 TBD); default from machine type
    "preset": "enhanced",              // existing; kept for compatibility (enhanced | original)
    "layout": "us-qwerty",             // primary layout
    "alternateLayout": "us-dvorak",    // IIc keyboard switch "down" / IIe Dvorak mod; optional
    "layoutSwitch": "primary",         // initial switch position: primary | alternate
    "capsLockMode": "hardware",        // hardware | pc-toggle. IIe default is hardware (Q1).
    "requireControlForReset": true,    // IIe keyboard jumper
    "autoRepeat": true,                // existing key; enables §8.1.4
    "shiftKeyMod": false,              // IIe X6 jumper: Shift on PB2. Default off (stock IIe).
    "strobeDebounceMs": 0,             // 0 = immediate; 10 ≈ AY-3600 bounce mask (Q4)
    "keymap": "avalonia-default",      // name or path of the JSON host keymap (§12). IIe/IIc default shown; IIgs default is avalonia-iigs.
    "typeText": {                      // defaults for TypeText on this machine
      "holdMs": 50,
      "gapMs": 10,
      "waitForConsumption": true,
      "forceUppercase": false,         // true by default on pocket2e-a2 only
      "unmappable": "skip"             // skip | replace | reject
    }
  }
}
```

For the IIc, `"model": "apple-iic"` also accepts `"columns80Switch": "up" | "down"`, the initial 80/40 switch position. On the unenhanced `pocket2e-a2` profile, `typeText.forceUppercase` defaults to true; every other profile leaves it false. IIe profiles default `capsLockMode` to `hardware`. IIe and IIc profiles default `keymap` to `avalonia-default`; IIgs profiles default it to `avalonia-iigs`. Reset and cold-reset chords live in the keymap, not in a `bindings` block. Those keymap defaults are provisional and can be overridden by a different `keymap` name or path, or by a user overlay, without a code change.

## 12. Host keymap

The mapping from physical host keys to `KeyboardKey` plus `KeyboardModifiers` lives in a JSON keymap file, not in code (Josh, 2026-11-10). Host key names are the Avalonia `Key` enum names for the Avalonia host, plus an optional physical-key or scan-code field when a `Key` name is not enough to tell two keys apart. The REPL and the eventual UI can load, modify and save the same files. The tables and excerpts in this section are the contents of those files, not translator rules and not a switch statement in `VideoWindow`.

### 12.1 File format

Each keymap is a JSON document that validates against `schemas/host-keymap.schema.json`.

```jsonc
{
  "$schema": "./schemas/host-keymap.schema.json",
  "version": 1,
  "name": "avalonia-default",
  "inherits": null,          // optional name of a base keymap; omitted or null means none
  "mappings": [
    { "hostKey": "A", "key": "A" },
    { "hostKey": "Insert", "key": "Help" },
    { "hostKey": "F12", "key": "Reset", "modifiers": "Control" },
    { "hostKey": "F12", "hostModifiers": "Control", "action": "coldReset" }
  ]
}
```

| Field | Required | Meaning |
|---|---|---|
| `$schema` | no | JSON Schema id, pointing at `schemas/host-keymap.schema.json`. |
| `version` | yes | Format version. The current version is `1`. A newer loader must reject an unknown version with a clear error. |
| `name` | yes | Stable name used for resolution and for `inherits`. |
| `inherits` | no | Name of a base keymap whose mappings this one starts from. A child entry with the same `hostKey` and `hostModifiers` replaces the inherited one. A child entry may set `"unset": true` to remove an inherited mapping. |
| `mappings` | yes | Array of entries, each either a key mapping or a machine-action mapping. |

Each mapping entry:

| Field | Required | Meaning |
|---|---|---|
| `hostKey` | yes | Host key name. In `avalonia-*.json` this is an Avalonia `Key` name (`A`, `D1`, `OemPlus`, `Back`, `Delete`, `LeftAlt`, `LWin`, `F12`, `PrintScreen`, `Pause`, …). Unknown names are a validation error. |
| `hostPhysicalKey` / `hostScanCode` | no | Optional extra identity when the Avalonia `Key` name is ambiguous. When present, a match requires both the name and this field. |
| `hostModifiers` | no | Host modifiers that must be down for this entry to apply. A plus-separated list of `Shift`, `Control`, `Alt`, `Meta` (Windows / Command / GUI). Omitted or `"None"` means no extra host modifiers are required. |
| `key` | one of `key` or `action` | The `KeyboardKey` this host key maps to (`A`, `Reset`, `ForwardDelete`, `LeftOption`, …). |
| `modifiers` | no | Extra `KeyboardModifiers` to merge into the snapshot sent with the `KeyboardKey` (for example `"Control"` so F12 still resets when `requireControlForReset` is on). The live host snapshot (Shift, Option as Open/Closed Apple, Caps Lock, and so on) is still applied. |
| `action` | one of `key` or `action` | A machine action, not a `KeyboardKey`. The only defined action in v1 is `coldReset`. An action entry does not also assert `KeyboardKey.Reset`. |
| `unset` | no | When `true`, removes the inherited mapping for this `hostKey` + `hostModifiers`. Cannot be combined with `key` or `action`. |

Matching: an entry applies when its `hostKey` (and optional physical/scan field) matches and every modifier in `hostModifiers` is down. If several entries match, the one with the most `hostModifiers` wins. A matching entry consumes the host key: it is not also delivered as any less-specific or default `KeyboardKey` for that host key. Conflicts that cannot be resolved this way (two entries with the same `hostKey`, `hostModifiers` and specificity, or an entry that sets both `key` and `action`) are validation errors, as are duplicate entries and unknown `key`, `action`, `hostKey` or modifier names.

### 12.2 Built-in `avalonia-default.json`

`avalonia-default.json` is the built-in keymap for the Avalonia host on IIe and IIc profiles. Its mappings are the default host-to-`KeyboardKey` correspondences:

| Host input (Avalonia `Key`) | `KeyboardKey` | Notes |
|---|---|---|
| Letters, digits, punctuation | The matching letter, `D0`–`D9`, or punctuation member | US-layout legends. Avalonia names such as `OemPlus` → `Equals`, `OemTilde` → `Backtick`. |
| F1–F15 | `F1`–`F15` | Unless a Reset or cold-reset mapping consumes the key (§12.3). |
| `PrintScreen`, `Scroll`, `Pause` | `F13`, `F14`, `F15` | Same cluster as F13–F15 on the Extended Keyboard II [MacHW Figure 8-10]. |
| `LeftAlt` / `RightAlt` | `LeftOption` / `RightOption` | Default Apple-key snapshot: Left Option → Open Apple, Right Option → Closed Apple. On the IIgs, Option. |
| `LWin` / `RWin` (Windows, Command) | `LeftCommand` / `RightCommand` | On the IIgs, Command. The default IIe/IIc keymap does not also treat these as Apple keys. |
| `Back` (PC Backspace / Mac Delete) | `Backspace` | IIe `$08`. |
| `Delete` (PC Delete / Mac Forward Delete) | `ForwardDelete` | IIe `$7F`. |
| `Insert` | `Help` | Help sits where PC Insert is on the Extended Keyboard II. |
| Home, End, Page Up, Page Down, arrows | The matching member | |
| Keypad keys | The matching `Keypad*` member | |
| Host power key, if any | `Power` | |
| (no modern Reset key) | `Reset` | Only via a keymap entry (§12.3) or an explicit API call (headless, REPL, MCP, menu). |

Reset and cold-reset entries that this file also contains are in §12.3.

Short excerpt (illustrative, not the complete file):

```json
{
  "$schema": "./schemas/host-keymap.schema.json",
  "version": 1,
  "name": "avalonia-default",
  "mappings": [
    { "hostKey": "A", "key": "A" },
    { "hostKey": "D1", "key": "D1" },
    { "hostKey": "OemPlus", "key": "Equals" },
    { "hostKey": "OemTilde", "key": "Backtick" },
    { "hostKey": "Back", "key": "Backspace" },
    { "hostKey": "Delete", "key": "ForwardDelete" },
    { "hostKey": "Insert", "key": "Help" },
    { "hostKey": "LeftAlt", "key": "LeftOption" },
    { "hostKey": "RightAlt", "key": "RightOption" },
    { "hostKey": "LWin", "key": "LeftCommand" },
    { "hostKey": "RWin", "key": "RightCommand" },
    { "hostKey": "PrintScreen", "key": "F13" },
    { "hostKey": "Scroll", "key": "F14" },
    { "hostKey": "Pause", "key": "F15" },
    { "hostKey": "F12", "key": "Reset", "modifiers": "Control" },
    { "hostKey": "F12", "hostModifiers": "Control", "action": "coldReset" }
  ]
}
```

Letters, digits, F1–F11, arrows, the navigation cluster and the keypad follow the same pattern (`"hostKey": "F1", "key": "F1"`, and so on) and are omitted here for brevity. The complete built-in file is the source of truth and is tested for load and round-trip (§12.7, §13).

### 12.3 Reset and cold-reset mappings

Reset and cold-reset host chords are **keymap entries**, not code and not a `bindings` block in the profile. Each machine family has its own built-in keymap (or a built-in that `inherits` from another) with its own defaults. **These defaults are provisional and subject to change.** Changing a default later means editing the built-in keymap (and any user overlay), not a code change. Tests must resolve the keymap rather than hard-coding F12 or any other chord.

The Reset mapping sends `KeyboardKey.Reset` with the modifiers the entry specifies. The cold-reset mapping is an `action` of `coldReset`. The menu command and the REPL `reset` command always work, including `reset --open-apple` and `reset --closed-apple` (#369), by sending `KeyboardKey.Reset` themselves, regardless of the keymap.

#### 12.3.1 IIe and IIc: `avalonia-default.json`

These continue the earlier decision in [`specs/Keyboard Mapping Specification.md`](./specs/Keyboard%20Mapping%20Specification.md). They apply to IIe and IIc profiles, which use `avalonia-default`. The IIe and IIc have no function keys, so using F12 does not hide a machine key.

| Host input | Default mapping | Notes |
|---|---|---|
| F12 | `key: Reset`, `modifiers: Control` | The host F12 key is mapped to `KeyboardKey.Reset`, not to `KeyboardKey.F12`, with Control implied, so `requireControlForReset` does not block the shortcut. Left Alt+F12 holds Open Apple (IIe reboot / forced cold start). Right Alt+F12 holds Closed Apple (IIe self-test). |
| Ctrl+F12 | `action: coldReset` | The cold-reset machine action. It is not a `KeyboardKey` and does not also assert `KeyboardKey.Reset`. |

#### 12.3.2 IIgs: `avalonia-iigs.json`

`avalonia-iigs.json` is a built-in keymap that `inherits` from `avalonia-default` and overrides the F12 / Ctrl+F12 entries so every host function key reaches the emulated machine as an ADB key (§8.3). F12 is a real key on the Extended Keyboard and Extended Keyboard II [MacHW Figure 8-10], and software can read it [Empson-94], so F12 is **not** the Reset default on this keymap. Host Print Screen, Scroll Lock and Pause stay mapped to F13, F14 and F15 and also pass through.

```json
{
  "$schema": "./schemas/host-keymap.schema.json",
  "version": 1,
  "name": "avalonia-iigs",
  "inherits": "avalonia-default",
  "mappings": [
    { "hostKey": "F12", "key": "F12" },
    { "hostKey": "F12", "hostModifiers": "Control", "unset": true },
    { "hostKey": "F12", "hostModifiers": "Control+Alt", "key": "Reset", "modifiers": "Control" },
    { "hostKey": "F12", "hostModifiers": "Control+Alt+Shift", "action": "coldReset" }
  ]
}
```

| Host input | Default mapping | Notes |
|---|---|---|
| F1–F15 | The matching `KeyboardKey` (`F1`–`F15`) and ADB function key | Pass through. `F12` is `KeyboardKey.F12` on this keymap. |
| Print Screen, Scroll Lock, Pause | `F13`, `F14`, `F15` | Inherited from `avalonia-default` (§12.2). |
| Ctrl+Alt+F12 | `key: Reset`, `modifiers: Control` | The whole chord is consumed and mapped to `KeyboardKey.Reset` with Control implied (a warm Control-Reset [TIL-2061]). This is not a single ADB key, so it does not collide with `KeyboardKey.F12` or any other function key. |
| Ctrl+Alt+Shift+F12 | `action: coldReset` | The cold-reset machine action. It is not a `KeyboardKey`, it does not also assert `KeyboardKey.Reset`, and it does not collide with a single ADB key. |
| IIgs Control-Command-Reset / self-test / Control Panel | Menu or REPL | Use `reset --open-apple` (Control-Command-Reset cold start [HWR-gs Table 6-3; TIL-2061]), `reset --open-apple --closed-apple` (self-test, following [TN-95]), or the documented Option-Control-Reset Control Panel path when that action exists. Additional host chords may be added later as more keymap entries; they are data. |

### 12.4 Where keymaps live and how they resolve

- **Built-in defaults** are shipped as embedded resources, one per host, plus per-machine-family overrides where needed: `avalonia-default.json` (Avalonia host, IIe/IIc Reset chords) and `avalonia-iigs.json` (same host, F1–F15 pass-through and the IIgs Reset chords). Other hosts can add their own built-ins later (for example a headless or terminal host) without changing the format.
- **User overrides** live in the user config directory, alongside the existing settings and profiles: `~/.backpocket/keymaps/<name>.json` (for example `~/.backpocket/keymaps/avalonia-default.json`).
- **The profile** references a keymap by name or path in its keyboard `config.keymap` field (§11.1). IIe and IIc profiles default to `avalonia-default`. IIgs profiles default to `avalonia-iigs`. A path is taken as a profile-supplied keymap file.
- **Resolution order is user, then profile, then built-in.** `HostKeymap.Resolve` loads the name or path from the profile (or the host built-in if the profile omits it), walks `inherits` through built-in resources (and files), then overlays the user file for that name if one exists. The user file wins on any (`hostKey`, `hostModifiers`) conflict. A user file may also set `inherits` to the built-in of the same name and list only its overrides.

### 12.5 `IHostKeymap` / `HostKeymap`

`IHostKeymap` and `HostKeymap` live in a host-neutral assembly that can already see `KeyboardKey` (start in `BadMango.Emulator.Devices`, or a small dedicated project if that becomes too heavy). They must not reference Avalonia. The Avalonia adapter — which turns an Avalonia `Key`, and when useful a physical key or scan code, into the `hostKey` / `hostPhysicalKey` / `hostScanCode` strings the JSON uses — lives in the UI project (`BadMango.Emulator.Debug.UI`).

```csharp
public interface IHostKeymap
{
    string Name { get; }
    int Version { get; }

    bool TryMap(
        string hostKey,
        HostModifiers hostMods,
        out KeyboardKey key,
        out KeyboardModifiers modifiers);

    bool TryMapAction(
        string hostKey,
        HostModifiers hostMods,
        out string action);
}

public sealed class HostKeymap : IHostKeymap
{
    public static HostKeymap Load(string path);
    public static HostKeymap LoadBuiltIn(string name);
    public static HostKeymap Resolve(string nameOrPath, HostKeymapSources sources);
    public void Save(string path);
    public IReadOnlyList<KeymapValidationError> Validate();
    // TryMap / TryMapAction as on the interface
}
```

`TryMap` is the lookup used for ordinary keys and for Reset. It returns false when the matching entry is an action (or when nothing matches). `TryMapAction` is the lookup for machine actions such as `coldReset`; it returns false when the matching entry is a key mapping. Both apply the most-specific `hostModifiers` rule in §12.1. `HostKeymapSources` describes the built-in resource set, the user config directory, and an optional profile path. `Load` / `Save` read and write JSON. `Resolve` applies the resolution order in §12.4. `Validate` reports unknown key or modifier names, unknown versions, unknown `inherits` targets, duplicates, and conflicts, each as a `KeymapValidationError` with a clear message (file, entry, and reason). An invalid file fails to load; it is not silently ignored.

`HostModifiers` is a host-side flags type (`Shift`, `Control`, `Alt`, `Meta`), distinct from `KeyboardModifiers`. The Avalonia adapter fills it from the host event before calling `TryMap`.

### 12.6 REPL commands

The debugger REPL gains a `keymap` command that operates on the resolved keymap for the current machine (or a named file):

| Command | Effect |
|---|---|
| `keymap show` | Prints the resolved name, the inheritance chain, and every mapping (host chord → `KeyboardKey` plus modifiers, or action). |
| `keymap load <name-or-path>` | Loads a built-in, a user file, or a path, and makes it the effective keymap (still overlaid by a user file of the same name, unless the path is the user file itself). |
| `keymap save [path]` | Writes the current keymap to `~/.backpocket/keymaps/<name>.json`, or to `path` if given. |
| `keymap set <host-chord> <KeyboardKey> [modifiers]` | Adds or replaces a key mapping (example: `keymap set F12 Reset Control`). |
| `keymap set <host-chord> action:<name>` | Adds or replaces an action mapping (example: `keymap set Ctrl+F12 action:coldReset`). |
| `keymap unset <host-chord>` | Removes a mapping, so the inherited or built-in entry (if any) shows through. |
| `keymap reset` | Discards the user overlay for this name and reloads the built-in (and profile) keymap. |

### 12.7 Validation and testing

Every built-in keymap must load and round-trip: `Load` then `Save` then `Load` yields an equivalent keymap (same `name`, `version`, `inherits`, and mappings, ignoring insignificant JSON whitespace and property order). Unknown `hostKey`, `key`, `action`, modifier, or `version` values fail with a clear `KeymapValidationError`; they must not load. Duplicate and conflicting entries fail the same way. Hot reload of a keymap that changes on disk while the emulator is running is optional and is left for later.


## 13. Testing strategy

1. **Translator table tests** (exist for the IIe since #375). For every `KeyboardKey` × {Shift, Caps Lock, Control} × Apple-key combination, they check the expected code or `null`. That includes the expanded members (F1–F15, the navigation cluster, the keypad, Help, Forward Delete, Command, Reset, Power, and so on). A coverage test fails if a `KeyboardKey` is added without a decision. Unmapped keys, including `Reset` and `Power` on the IIe translator, return `null`. Each new layout or model gets its own table.
2. **`KeyboardInput` unit tests** use a fake scheduler and dispatcher. They cover:
   - FIFO order across threads
   - nothing applied off the machine thread
   - host auto-repeat filtering
   - `TypeText` steps scheduled with the exact cycle deltas for a given clock rate
   - `WaitForConsumption` behavior
   - `CancelTyping` and the RESET generation
   - receipt counts and unmappable policies
3. **End-to-end polling-loop tests** (exist for the IIe since #375). A 6502 loop in RAM waits for `$C000` bit 7, clears the strobe through `$C010`, and stores to a buffer on the real `pocket2e-a2-enh` profile. Input goes only through `IKeyboardInput`, and nothing pokes `$C000`. Cases:
   - `TypeText("Hello, World!")` with a key-up between characters
   - Shift+H, `i`, Shift+1 giving `Hi!`
   - Caps Lock in both modes: with Caps Lock down, Shift+A gives `A` in `hardware` mode and `a` in `pc-toggle`
   - Control
   - Apple keys held
   - type-ahead kept while the program isn't reading
   - auto-repeat count over a held key (once implemented)
   - a headless machine pressing `KeyboardKey.Reset` with Control asserts the RESET signal; with no Control and `requireControlForReset` true, nothing is asserted and no strobe fires
4. **Golden traces.** A fixed `TypeText` script runs on a fixed profile and initial state. The trace records `(cycle, $C000, AKD, PB0–PB2, RESET)` at each keyboard state change and is compared with a checked-in golden file. A determinism test replays the journal and asserts an identical trace.
5. **Multi-machine test.** Two machines run in one process, input goes to one, and the other is unaffected.
6. **Flake guard.** The `TypeText` end-to-end tests run in a loop (20 or more times) in CI or before merge.
7. **Host keymap tests.** Every built-in keymap (`avalonia-default.json`, `avalonia-iigs.json`) loads and round-trips through `Save`/`Load`. A user overlay in `~/.backpocket/keymaps/` wins over the profile and the built-in for the same (`hostKey`, `hostModifiers`). An unknown `hostKey`, `KeyboardKey`, action, or modifier is rejected with a clear `KeymapValidationError`. `VideoWindow` has no Avalonia-to-`KeyboardKey` switch; it goes through `IHostKeymap`. Reset and cold-reset chords come from the resolved keymap.

## 14. Migration plan

Each step builds and tests green on its own (`dotnet build BackPocketEmulator.slnx -c Release -warnaserror` and `dotnet test`).

1. **Done in #375.** Added a smaller `HostKey`, `IKeyTranslator`, `AppleIIeKeyTranslator`, and `IKeyboardDevice.Translator` (interim). `VideoWindow` maps to `HostKey`. `TypeString` runs on scheduler events, the strobe clear never starts a key, and the test suite was added.
2. **Add the input layer, expand the enum, and rename it to `KeyboardKey`.** Expand #375's `HostKey` to the set in §7.1 (F1–F15, navigation, keypad, Help, Forward Delete, Option, Command, Reset, Power), rename the enum to `KeyboardKey`, and rename #375's `Delete` to `ForwardDelete` and `LeftAlt` / `RightAlt` to `LeftOption` / `RightOption`. Do not change #375 itself; do the expansion and rename here. Update every translator table so each new member has an explicit decision (`null` on the IIe for keys the encoder does not produce, including Reset and Power). Add `IKeyboardInput`, `TypeTextOptions`, `TypeTextReceipt`, `IHostKeyboardSink`, `HostKeystroke`, the `InputEvent` journal types, and a `KeyboardInput` implementation. There is no separate Reset method: Reset arrives as `KeyDown(KeyboardKey.Reset, modifiers)`. `KeyboardDevice` implements `IHostKeyboardSink` and routes `KeyboardKey.Reset` to the RESET signal path. `MachineFactory` (or the builder) registers one `KeyboardInput` per machine whenever a keyboard sink exists.
3. **Move text typing.** Move `TypeString` logic into `KeyboardInput.TypeText`, using the profile clock rate instead of 1,020 cycles/ms. Delete `IKeyboardDevice.TypeString`, and update `KeyboardCommand type` and the tests in the same change.
4. **JSON host keymap.** Add `IHostKeymap` / `HostKeymap`, `schemas/host-keymap.schema.json`, and the built-in embedded resources `avalonia-default.json` and `avalonia-iigs.json`. Implement `Load`, `Save`, `Resolve` (user, then profile, then built-in), `TryMap`, `TryMapAction`, and validation (§12). Tests: every built-in loads and round-trips; a user overlay wins; an unknown name is rejected with a clear error. The Avalonia adapter lives in the UI project. Hot reload is out of scope.
5. **Move `VideoWindow`.** Switch it to `IKeyboardInput` and remove its use of `IKeyboardDevice.Translator`, `KeyDown(byte)`, `KeyUp()` and `SetModifiers`. Remove the Avalonia `Key` switch / hard-coded table; the window uses the resolved keymap via the Avalonia adapter and `IHostKeymap.TryMap` / `TryMapAction` (§12). Reset and cold-reset come from the keymap, including the IIe/IIc Alt+F12 Open-Apple and Closed-Apple combinations when those defaults are in effect. Do not hard-code F12.
6. **Move the REPL.** Switch `KeyboardCommand` (`press` chords, `type`) and `ResetCommand` to `IKeyboardInput`. `keyboard press ctrl+reset` and the `reset` command send `KeyboardKey.Reset` with the appropriate modifiers. Add `keymap show|load|save|set|unset|reset` (§12.6).
7. **Narrow `IKeyboardDevice`.** Remove `KeyDown(byte)`, `KeyUp()`, `SetModifiers`, `Translator`, `PressResetKey` and `ReleaseResetKey` from the public interface. `KeyDown(byte)`, `KeyUp()` and `SetModifiers` become sink methods (`OnHostKeyDown` / `OnHostKeyUp`). `PressResetKey` and `ReleaseResetKey` go away: Reset arrives as `OnHostKeyDown(KeyboardKey.Reset, modifiers)`. Rename `KeyboardDevice` to `AppleIIeKeyboard`, keeping `[DeviceType("keyboard")]`.
8. **Read the profile config.** Parse the keyboard config from §11.1: `capsLockMode` (default **`hardware`** on IIe profiles), `requireControlForReset`, `autoRepeat`, `shiftKeyMod` (default **false**), the `typeText` defaults (including `forceUppercase`, true on `pocket2e-a2` only), and `keymap` (name or path; IIe/IIc default `avalonia-default`; IIgs default `avalonia-iigs`). There is no `bindings` block; Reset and cold-reset chords live in the keymap. Update the profile JSON schema.
9. **Add the journal and replay.** Add the input journal and `Replay`, the golden-trace test, the determinism test, and the multi-machine test.
10. **IIe fidelity.** Add auto-repeat (§8.1.4), ASCII in the `$C010` low bits, strobe clear on writes to `$C010`–`$C01F` (check the overlap with the IOU/MMU `$C00x`/`$C01x` handlers first), AKD with multiple held keys, PB2 gated by `shiftKeyMod`, and the `hardware` Caps Lock mode as the IIe default, with `pc-toggle` still selectable. **Behavior changes:** `shiftKeyMod` defaults to off, so Shift is no longer shown on PB2 unless a profile turns the option on; `capsLockMode` defaults to `hardware`, which changes today's Shift-XOR-Caps behavior in PR #375's `AppleIIeKeyTranslator`.
11. **MCP tools** (with #337). Add `keyboard.type`, `keyboard.press` and `keyboard.key`.
12. **Later epics:** `AppleIIcKeyboard` (§8.2), `AdbKeyboard` and `AdbController` (§8.3), the 65832 keyboard (§8.4), and later layouts such as Dvorak as C# tables (§11).

## 15. Corrections to `specs/Keyboard Mapping Specification.md`

- It says Delete gives "`$FF` or N/A". On the IIe, Delete is `$7F` [TRM-e Table 2-3].
- It lists the Control combinations only for letters and `[`. The full IIe table includes `$1C`, `$1D`, `$00` (Ctrl+2), `$1E` (Ctrl+6) and `$1F` (Ctrl+Minus) [TRM-e Table 2-3].
- It says the letters are "uppercase by default". The IIe keyboard produces lowercase unless Shift or Caps Lock is active [Sather ch. 7].
- Its `IKeyboardController` sketch is superseded by §7.

## 16. Open questions

| # | Question | Notes |
|---|---|---|
| Q2 | Should the `$C011`–`$C01F` reads expose keyboard ASCII in bits 0–6? | Sather says yes. Today the IOU/MMU status handlers own those reads, so we need to confirm their low bits come from the keyboard latch. |
| Q3 | **IIe rollover.** TRM-e says both "N-key rollover" and "two-key rollover". When the newest of two held keys is released, does the older key re-strobe, and which key auto-repeats? | Needs a hardware check or the AY-3600 datasheet. Sather quotes the datasheet: "If the match occurs for an already encoded key, the match is not recognized." |
| Q4 | Should we model the roughly 10 ms debounce (AKD high before the strobe and ASCII update)? | Some software may read AKD and `$C000` together. The default is off. |
| Q6 | **IIc details:** auto-repeat delay and rate, rollover, the Caps Lock+Shift rule, and the encoder part. | TRM-c ch. 11 still needs reading. |
| Q7 | **IIgs:** do `$C061`/`$C062` reflect Command/Option (and the joystick buttons) the way the IIe Apple keys do, and is Option the IIe Closed Apple? | HWR-gs only confirms that Command does Open Apple's job (glossary). Function-key pass-through is a separate, verified fact (§8.3, [Empson-94]) and is not part of this question. |

### Decided (Josh, 2026-11-10)

| # | Decision | Notes |
|---|---|---|
| Q1 | **IIe profiles default to `hardware` Caps Lock, configurable.** With Caps Lock down, letters are uppercase whether or not Shift is held, matching a real IIe [Sather ch. 7]. `pc-toggle` (Shift XOR Caps Lock, today's PR #375 behavior) remains a per-profile option. | This is a behavior change from the current translator. The default flips when the implementation of this spec lands; #375 is left as `pc-toggle`. |
| Q5 | **Shift-key mod defaults to off.** PB2 does not show Shift unless the profile sets `shiftKeyMod` to true. | Matches a stock IIe with no X6 jumper [TRM-e ch. 2]. This is a behavior change: today's device always reports Shift on PB2. |
| Q8 | **Reset and cold-reset chords are JSON keymap entries with machine-specific defaults, and those defaults are provisional (subject to change).** They produce `KeyboardKey.Reset` with the specified modifiers, or invoke the cold-reset machine action (`action: coldReset`). They are not a separate Reset input, and F12 remains an ordinary `KeyboardKey`. IIe and IIc profiles use `avalonia-default.json`, which maps F12 to Reset (Control implied) and Ctrl+F12 to cold reset, with Alt modifiers as Open Apple / Closed Apple. IIgs profiles use `avalonia-iigs.json`, which inherits from that file, passes every host function key (F1–F15, plus Print Screen / Scroll Lock / Pause as F13–F15) through as an ADB key, and default Reset to Ctrl+Alt+F12 and cold reset to Ctrl+Alt+Shift+F12, chords that do not collide with any single ADB key. The menu command and the REPL `reset` command always work, by sending `KeyboardKey.Reset`. A profile selects a keymap by name or path; a user overlay wins over the profile and the built-in. Changing a default later means editing the keymap, not a code change. | IIe/IIc F12 defaults come from [`specs/Keyboard Mapping Specification.md`](./specs/Keyboard%20Mapping%20Specification.md). IIgs F-key pass-through is required because the Extended Keyboard and Extended Keyboard II have F1–F15 as ordinary ADB keys [MacHW Figure 8-10; TIL-112; Empson-94]. Refined (Josh, 2026-11-10) so Reset is itself a `KeyboardKey`, and again so the host mapping lives in a JSON keymap; see the KeyboardKey and Host keymap rows below. |
| KeyboardKey | **`KeyboardKey` is the shared key set that all input sources produce and all keyboard devices consume, not the host's keys.** It is modeled on the Apple Extended Keyboard II (M3501), including Reset and Power. The app host maps physical host keys to `KeyboardKey` through a JSON keymap (§12). The core never sees host key types. Each keyboard device interprets `KeyboardKey`. Reset is a `KeyboardKey` so a headless instance can press it. IIe and IIc devices route `KeyboardKey.Reset` to the RESET signal path, not the encoder (no strobe, no AKD, `requireControlForReset` still applies). The IIgs routes it the same way (the ADB Reset key). Power is ignored or reserved on machines that have no power key. Cold reset stays a machine action, not a key. Every `KeyboardKey` needs an explicit translator decision, enforced by a test; unmapped keys return `null`. The modifiers type is `KeyboardModifiers`. | Josh, 2026-11-10. Reference: [MacHW] Figure 8-10. Renamed from #375's `HostKey` (Josh, 2026-11-10) because that name no longer fits. The implementation issue (#388) expands the enum, renames it, and renames `Delete` → `ForwardDelete` and `LeftAlt` / `RightAlt` → `LeftOption` / `RightOption`. Do not change #375 itself. |
| Host keymap | **The mapping from physical host keys to `KeyboardKey` plus `KeyboardModifiers` lives in a JSON keymap file, not in code.** The file has `$schema` / `version`, a `name`, optional `inherits`, and `mappings` from a host key plus host modifiers to a `KeyboardKey` plus modifiers, or to a machine action (`coldReset`). Built-in defaults are embedded resources per host (`avalonia-default.json`) and per machine family where needed (`avalonia-iigs.json`). User overrides live in `~/.backpocket/keymaps/`. The profile's keyboard section references a keymap by name or path. Resolution order is user, then profile, then built-in. `IHostKeymap` / `HostKeymap` (`Load` / `Save` / `Resolve`, `TryMap`, `TryMapAction`, validation) live in a host-neutral assembly; the Avalonia adapter lives in the UI project. The REPL exposes `keymap show`, `load`, `save`, `set`, `unset` and `reset`. Every built-in keymap must load and round-trip; unknown names fail with a clear error. Hot reload is later. The REPL and the eventual UI can load, modify and save the same files. | Josh, 2026-11-10. See §12. |
| Q9 | **We will not ship keyboard ROM images.** The keyboard ROM is emulated in C# code tables. A ROM-image-driven translator is out of scope unless a real need appears later. | Sather Table 7.2 remains the reference for building the C# tables. |
| Q10 | **Yes.** `TypeText` forces uppercase on the unenhanced `pocket2e-a2` profile by default (`typeText.forceUppercase: true`). Other profiles default to false. Any profile can set the option. | Applesoft on an unenhanced IIe needs uppercase keywords. |

## 17. Sources

- **[TRM-e]** Apple Computer, *Apple IIe Technical Reference Manual*: ch. 2 "The Keyboard" (Table 2-2, Table 2-3, Switch Inputs and Shift-Key Mod) and ch. 4 (Forced Cold Start, Automatic Self-Test). Full text: <https://archive.org/stream/Apple_IIe_Technical_Reference_Manual/Apple_IIe_Technical_Reference_Manual_djvu.txt>
- **[Sather]** Jim Sather, *Understanding the Apple IIe* (Quality Software, 1985): ch. 7 "Input/Output" (keyboard ROM, Caps Lock sets, Table 7.2, Keyboard Operational Summary, keyboard section and Keyboard Operational Summary) and ch. 3, Figure 3.8 (auto-repeat timing). Scan: <https://dmcmillan.co.uk/blog/apple-iie-without-a-keyboard/Understanding_the_Apple_IIe.pdf>
- **[TRM-c]** Apple Computer, *Apple IIc Technical Reference Manual*: ch. 1 (80/40 and keyboard switches), ch. 4 Table 4-1, Table B-7. <https://archive.org/stream/apple-iic-tech-ref/Image081317124723.duplex.merged_djvu.txt>
- **[HWR-gs]** Apple Computer, *Apple IIgs Hardware Reference*: ch. 6 "The Apple Desktop Bus" (Control-Command-Reset and the reset routine; Tables 6-2 to 6-7 and 6-10, including Table 6-3 bit 5 for the Control-Command-Reset cold start-up; glossary entry for Control-Reset). A clean PDF is at <https://retrotechwiki.com/images/b/bf/Apple_IIGS_Hardware_Reference_1989.pdf>. OCR text: <https://archive.org/stream/Apple_IIgs_Hardware_Reference/Apple_IIgs_Hardware_Reference_djvu.txt>
- **[RC-SE]** Retrocomputing Stack Exchange, "What do the 80/40 and keyboard buttons on my Apple IIc do?" (cites TRM-c). <https://retrocomputing.stackexchange.com/questions/13977>
- **[TN-58]** Apple IIgs Technical Note #58, "Keyboard Modifiers Register Anomaly" (Dave Lyons, 1989). <https://apple2.gs/technotes/tn-iigs-058/>
- **[TN-95]** Apple IIgs Technical Note #95, "ROM Diagnostic Errors" (Dan Strnad, September 1990): self-test is activated by Open-Apple and Option on power up, or Open-Apple, Option, and Reset. <https://apple2.gs/technotes/tn-iigs-095/>
- **[TIL-112]** Apple Tech Info Library #112, "Apple Extended Keyboard: Specifications" (created 4 December 1989; reviewed 25 June 1992): the Extended Keyboard II (replacement for M0115) has 15 function keys (F1–F15); system requirement is any Apple personal computer with an Apple Desktop Bus interface. <https://www.savagetaylor.com/TIL/TIL00112.pdf>
- **[TIL-522]** Apple Tech Info Library #522, "Apple IIGS: Entering the Control Panel" (reviewed 21 September 1987): Option while powering on (Owner's Guide p. 13; "doesn't always work"); Option-Control-Reset. <https://www.savagetaylor.com/TIL/TIL00522.pdf>
- **[TIL-2061]** Apple Tech Info Library #2061, "Apple IIGS: Resetting And Buffer Clearing": warm reset is Control-Reset; cold reset is Control-Apple-Reset, "reboot as if it were just turned on." <http://absurdengineering.org/library/MASTER%20Tech%20Info%20Library/Apple%20II%20Hardware/Apple%20IIGS%20Hardware%20Issues/TIL02061%20-%20Apple%20IIGS%20-%20Resetting%20And%20Buffer%20Clearing.pdf>
- **[QSG-gs]** Apple Computer, *Apple IIGS Quick Start Guide* (1988, part no. 031-0107-A): Control Panel via Option-Control-Reset; start a disk when the power is on via Command-Control-Reset. <https://mirrors.apple2.org.za/ftp.apple.asimov.net/documentation/hardware/machines/Apple%20IIGS%20Quick%20Start%20Guide.pdf>
- **[MacHW]** Apple Computer, *Guide to the Macintosh Family Hardware*, 2nd edition (Addison-Wesley, 1990), ch. 8 "Apple Desktop Bus": the Apple Extended Keyboard is an ADB keyboard. Figure 8-10 is the reference layout for `KeyboardKey` (the Extended Keyboard II, M3501): F1–F15 and their key-down transition codes (F13 `$69`, F14 `$6B`, F15 `$71`, immediately to the right of F12), the navigation cluster (Help, Home, Page Up, Forward Delete, End, Page Down, arrows), the full keypad (0–9, `.`, Enter, `+`, `-`, `*`, `/`, `=`, Clear), and the modifier and power keys. <https://vintageapple.org/inside_o/pdf/Guide_to_Macintosh_Family_Hardware_2nd_Edition_1990.pdf>
- **[Empson-94]** David Empson, "Re: Extended ADB keyboard help," *comp.sys.apple2.programmer*, 2 August 1994: on a IIgs with an Extended ADB keyboard, F1–F15 are visible to the Event Manager (keypad modifier bit) and via `$C000`/`$C010` plus `$C025` bit 4. <https://groups.google.com/g/comp.sys.apple2.programmer/c/oZBTT54qOTc>
- Gary Little, *Inside the Apple IIe* (Brady, 1985), ch. 6, "Keyboard auto-repeat" (about 900 ms, 15 per second). <https://apple2online.com/wp-content/uploads/Inside-the-Apple-IIe.pdf>
