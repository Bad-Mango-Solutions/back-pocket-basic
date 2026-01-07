# Pocket2e Debug Video Window (Avalonia) — Specification

## 1. Architectural Positioning

### Core principle

**The emulator does not know about Avalonia.**  
 Avalonia is a *presentation layer* consuming stable device interfaces exposed by `IMachine`.

```
Console REPL
    |
    v
IMachine  ── Devices ── Video / Keyboard / Speaker
    |
    v
Video Session (host-side)
    |
    v
Avalonia Window (framebuffer + input)
```

This allows:

- Headless operation
- Debug-first workflows
- Reuse in the full UI app
- Deterministic testing of video logic

------

## 2. New Host-Side Components

### 2.1 `IVideoSession`

**Purpose:** Owns the lifetime of the video window and binds it to a machine.

```csharp
public interface IVideoSession : IDisposable
{
    bool IsOpen { get; }

    void Open();
    void Close();

    void SetScale(int scale);
    void SetShowFps(bool enabled);
}
```

**Responsibilities**

- Spin up Avalonia UI thread
- Create window
- Wire renderer → presenter
- Wire keyboard input → `IKeyboardDevice`
- Observe machine lifecycle (`Run`, `Stop`, `Reset`)

------

### 2.2 `IVideoRenderer`

**Purpose:** Convert Apple IIe video state + memory into pixels.

```csharp
public interface IVideoRenderer
{
    VideoFrame RenderFrame();
}
public sealed record VideoFrame(
    int Width,
    int Height,
    ReadOnlyMemory<byte> PixelData, // BGRA8888
    VideoMode Mode,
    ulong FrameNumber
);
```

**Key rule:**  
 Renderer is **pure**. No UI, no timing, no threading assumptions.

------

### 2.3 `IVideoPresenter`

**Purpose:** Display frames in Avalonia.

```csharp
public interface IVideoPresenter
{
    void Present(VideoFrame frame);
}
```

Implementation uses:

- `WriteableBitmap`
- Nearest-neighbor scaling
- Fixed integer scale (default 2×)

------

## 3. Canonical Framebuffer Model

### Internal resolution (fixed)

**560 × 384**, BGRA8888

Why:

- 2× vertical scaling of 192-line modes
- Native width for double-hires
- Clean text rendering
- No mode-dependent window resizing

### Mapping rules

| Mode         | Source      | Rendered Region           |
| ------------ | ----------- | ------------------------- |
| 40-col text  | $0400/$0800 | 560×384                   |
| 80-col text  | main+aux    | 560×384                   |
| Lo-res       | text memory | 560×384                   |
| Hi-res       | $2000/$4000 | 560×384                   |
| Double-hires | main+aux    | 560×384                   |
| Mixed        | split       | top graphics, bottom text |

------

## 4. Video State Inputs (Already Excellent)

You already have the right interfaces.

### Required devices (via `IMachine.Devices`)

- `IVideoModeDevice`
- `IKeyboardDevice`
- (later) `ISpeakerDevice`

### Renderer reads:

- `IVideoModeDevice.CurrentMode`
- `IsTextMode`
- `IsMixedMode`
- `IsPage2`
- `IsHiRes`
- `Is80Column`
- `IsDoubleHiRes`
- `IsAltCharSet` (MouseText)
- Annunciators (optional overlays)

### Memory access

Renderer reads memory via:

- Main RAM
- Aux RAM

**Spec requirement:**  
 Renderer must *not* guess soft-switch behavior. It must either:

- Use a video-memory abstraction, or
- Explicitly apply IIe rules (80STORE, aux read/write, etc.)

This keeps Monitor ROM tricks honest.

------

## 5. Text Rendering & MouseText

### Character generator

Introduce:

```csharp
public interface ICharacterGenerator
{
    ReadOnlySpan<byte> GetGlyph(byte code, bool inverse, bool flashing);
}
```

- 8×8 glyphs
- MouseText glyph bank selected via `IsAltCharSet`
- Flashing handled via frame counter (toggle every ~0.5s)

### Text layout

| Mode   | Columns | Rows |
| ------ | ------- | ---- |
| 40-col | 40      | 24   |
| 80-col | 80      | 24   |

Glyphs are scaled to fit canonical framebuffer.

------

## 6. Graphics Rendering Rules

### Lo-res / Double lo-res

- 4-bit color nibbles
- 40×48 or 80×48 logical grid
- Expanded to framebuffer

### Hi-res / Double hi-res

- Bitplane decoding
- Page selection respected
- Artifact color **optional** (stage 2)

**Stage 1 rule:**  
 Monochrome or deterministic palette is acceptable. Correct memory interpretation matters more than color fidelity initially.

------

## 7. Keyboard Input (Already Solved)

Avalonia window captures:

- `KeyDown`
- `KeyUp`
- `TextInput`

Mapped to:

```csharp
IKeyboardDevice.KeyDown(byte ascii);
IKeyboardDevice.KeyUp();
IKeyboardDevice.SetModifiers(...);
```

### Modifier mapping

| Host  | Apple II                         |
| ----- | -------------------------------- |
| Shift | Shift                            |
| Ctrl  | Control                          |
| Alt   | Open/Closed Apple (configurable) |

**Important:**  
 Console REPL remains active. Window input only affects the emulated keyboard.

------

## 8. Threading Model

### UI thread

- Dedicated Avalonia thread
- Owns window + bitmap

### Emulator thread

- Existing run/step logic unchanged

### Frame scheduling

**Initial implementation (recommended):**

- UI timer at 60 Hz
- Calls `RenderFrame()`
- Presents latest frame
- Drops frames if emulator is faster

Later you can switch to event-driven if desired.

------

## 9. DI & Startup Integration

### Autofac registrations

```csharp
builder.RegisterType<VideoSession>()
    .As<IVideoSession>()
    .SingleInstance();

builder.RegisterType<AppleIIeVideoRenderer>()
    .As<IVideoRenderer>()
    .SingleInstance();
```

### REPL command

Add `VideoCommand`:

```
video open
video close
video scale 2
video fps on
video status
```

------

## 10. Reuse in Full UI Project

This design lifts cleanly because:

- Renderer is UI-agnostic
- Presenter is Avalonia-specific
- Keyboard routing already matches your device model
- `IVideoSession` becomes a shared service

Your full UI app can:

- Reuse `IVideoRenderer`
- Replace `IVideoSession` with a richer host
- Add overlays, inspectors, breakpoints

------

## 11. Implementation Order (Low Risk)

1. Open Avalonia window from console
2. Render test pattern
3. Render text page `$0400`
4. Wire keyboard input
5. Implement 40/80 text
6. Implement lo-res / hi-res
7. Add aux/double modes
8. Add MouseText glyphs

------

## Final Assessment

You’re not “adding a window.”
 You’re **formalizing the boundary between emulated hardware and host presentation**, and your existing interfaces already reflect that mindset.

This spec will:

- Validate your video soft-switch logic
- Make Monitor ROM behavior visible
- Enable real debugging of graphics code
- Scale naturally into a full UI

Next step, if you want:
 I can sketch the **exact Avalonia window + WriteableBitmap presenter** and how to spin it up from a console app without blocking the REPL.