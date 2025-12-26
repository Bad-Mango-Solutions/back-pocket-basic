# Emulator UI Specification

**Document Purpose:** Design specification for the Avalonia-based emulator user interface.  
**Version:** 1.0  
**Date:** 2025-01-13  
**Target Framework:** Avalonia UI 11.x on .NET 10.0

---

## Overview

The emulator UI provides a graphical frontend for the BackPocket emulator family, supporting:

1. **Machine Management** - Profile definition, instance lifecycle, bus runtime
2. **Storage Management** - Disk and ROM image creation/manipulation
3. **Display and Input** - Video rendering in all supported modes, keyboard/mouse translation
4. **Debug Integration** - Live debugging of running instances
5. **Development Tools** - Assembly editor/compiler, BASIC editor (future)

### Machine Personalities

| Personality | Display Modes | Input Model | Status |
|-------------|--------------|-------------|--------|
| **Pocket2e** | Text 40/80, Lo-Res, Hi-Res, Double Hi-Res | Apple IIe keyboard, mouse | Phase 1 |
| **PocketGS** | All IIe modes + Super Hi-Res (320/640×200) | ADB keyboard/mouse | Phase 2 |
| **PocketME** | Native modern modes (TBD) | PC keyboard/mouse | Phase 3 |

---

## Architecture

### Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **UI Framework** | Avalonia 11.x | Cross-platform XAML UI |
| **MVVM** | CommunityToolkit.Mvvm | ViewModels, commands, observables |
| **DI Container** | Microsoft.Extensions.DI | Service resolution |
| **Rendering** | SkiaSharp + Avalonia | Video display compositing |
| **Logging** | Serilog | Structured logging |

### Project Structure

```
BadMango.Emulator.UI/
??? App.axaml                     # Application definition
??? App.axaml.cs                  # Application code-behind
?
??? Converters/                   # Value converters
?   ??? BoolToVisibilityConverter.cs
?   ??? AddressFormatConverter.cs
?   ??? MachineStateConverter.cs
?
??? Models/                       # UI data models
?   ??? MachineProfile.cs         # Profile definition
?   ??? DiskImage.cs              # Disk image metadata
?   ??? RomImage.cs               # ROM image metadata
?   ??? DisplaySettings.cs        # Display configuration
?
??? Services/                     # UI services
?   ??? IMachineService.cs        # Machine lifecycle
?   ??? IStorageService.cs        # Disk/ROM management
?   ??? IDisplayService.cs        # Video rendering
?   ??? IInputService.cs          # Keyboard/mouse handling
?   ??? IDebugService.cs          # Debug integration
?
??? ViewModels/                   # MVVM ViewModels
?   ??? MainWindowViewModel.cs
?   ??? MachineManagerViewModel.cs
?   ??? StorageManagerViewModel.cs
?   ??? DisplayViewModel.cs
?   ??? DebugConsoleViewModel.cs
?   ??? EditorViewModel.cs
?
??? Views/                        # AXAML Views
?   ??? MainWindow.axaml
?   ??? MachineManagerView.axaml
?   ??? StorageManagerView.axaml
?   ??? DisplayView.axaml
?   ??? DebugConsoleView.axaml
?   ??? EditorView.axaml
?
??? Controls/                     # Custom controls
?   ??? VideoDisplay.axaml        # Apple II display rendering
?   ??? DebugTerminal.axaml       # Debug console terminal
?   ??? HexEditor.axaml           # Memory/disk hex editor
?   ??? RegisterPanel.axaml       # CPU register display
?   ??? DisassemblyView.axaml     # Live disassembly
?
??? Resources/                    # Assets
    ??? Fonts/                    # Apple II character ROMs
    ??? Icons/                    # UI icons
    ??? Styles/                   # AXAML styles
```

---

## Module 1: Machine Management

### Purpose

Create, configure, start, stop, and monitor emulator instances.

### Machine Profile Model

```csharp
/// <summary>
/// Defines a machine configuration for instantiation.
/// </summary>
public record MachineProfile
{
    /// <summary>Gets the unique identifier for this profile.</summary>
    public required string Id { get; init; }
    
    /// <summary>Gets the display name.</summary>
    public required string Name { get; init; }
    
    /// <summary>Gets the machine personality.</summary>
    public required MachinePersonality Personality { get; init; }
    
    /// <summary>Gets the CPU configuration.</summary>
    public required CpuProfile Cpu { get; init; }
    
    /// <summary>Gets the memory configuration.</summary>
    public required MemoryProfile Memory { get; init; }
    
    /// <summary>Gets the peripheral configurations.</summary>
    public IReadOnlyList<PeripheralProfile> Peripherals { get; init; } = [];
    
    /// <summary>Gets the ROM images to load.</summary>
    public IReadOnlyList<RomBinding> RomBindings { get; init; } = [];
    
    /// <summary>Gets the disk images to mount.</summary>
    public IReadOnlyList<DiskBinding> DiskBindings { get; init; } = [];
}

public enum MachinePersonality
{
    Pocket2e,       // Apple IIe enhanced
    Pocket2c,       // Apple IIc
    PocketGS,       // Apple IIgs
    PocketME,       // Native 65832
}
```

### IMachineService Interface

```csharp
/// <summary>
/// Manages machine instance lifecycle.
/// </summary>
public interface IMachineService
{
    /// <summary>Gets all active machine instances.</summary>
    IReadOnlyList<IMachine> Instances { get; }
    
    /// <summary>Gets all saved machine profiles.</summary>
    IReadOnlyList<MachineProfile> Profiles { get; }
    
    /// <summary>Creates a new machine instance from a profile.</summary>
    Task<IMachine> CreateInstanceAsync(MachineProfile profile);
    
    /// <summary>Starts a machine instance.</summary>
    Task StartAsync(IMachine machine);
    
    /// <summary>Stops a machine instance.</summary>
    Task StopAsync(IMachine machine);
    
    /// <summary>Resets a machine instance.</summary>
    Task ResetAsync(IMachine machine, bool cold);
    
    /// <summary>Pauses a running machine.</summary>
    Task PauseAsync(IMachine machine);
    
    /// <summary>Resumes a paused machine.</summary>
    Task ResumeAsync(IMachine machine);
    
    /// <summary>Destroys a machine instance.</summary>
    Task DestroyAsync(IMachine machine);
    
    /// <summary>Saves a machine profile.</summary>
    Task SaveProfileAsync(MachineProfile profile);
    
    /// <summary>Deletes a machine profile.</summary>
    Task DeleteProfileAsync(string profileId);
    
    /// <summary>Event raised when instance state changes.</summary>
    event EventHandler<MachineStateChangedEventArgs>? StateChanged;
}
```

### Machine Manager View

```
???????????????????????????????????????????????????????????????????
? Machine Manager                                           [?][?][×] ?
???????????????????????????????????????????????????????????????????
? ?? Profiles ???????????????????? ?? Active Instances ?????????? ?
? ? ?? My Apple IIe              ? ? ? My Apple IIe [Running]   ? ?
? ? ?? Development Machine       ? ?   CPU: 1.023 MHz           ? ?
? ? ?? Game Testing              ? ?   RAM: 128KB               ? ?
? ?                              ? ?   Disk 1: DOS33.dsk        ? ?
? ? [+ New Profile]              ? ?                            ? ?
? ?                              ? ?   [Pause] [Reset] [Stop]   ? ?
? ???????????????????????????????? ?????????????????????????????? ?
?                                                                 ?
? ?? Profile Details ????????????????????????????????????????????? ?
? ? Name: [My Apple IIe                    ]                     ? ?
? ? Type: [Pocket2e (Apple IIe Enhanced)   ?]                    ? ?
? ?                                                              ? ?
? ? CPU:    65C02 @ 1.023 MHz                                    ? ?
? ? Memory: 128 KB RAM                                           ? ?
? ?                                                              ? ?
? ? Peripherals:                                                 ? ?
? ?   Slot 6: Disk II Controller                                 ? ?
? ?   Slot 7: Serial Card (Super Serial)                         ? ?
? ?                                                              ? ?
? ? ROMs:                                                        ? ?
? ?   System: AppleIIe_Enhanced.rom                              ? ?
? ?   Video: Video_Enhanced.rom                                  ? ?
? ?                                                              ? ?
? ? [Edit] [Duplicate] [Delete]         [Start Instance ?]       ? ?
? ???????????????????????????????????????????????????????????????? ?
???????????????????????????????????????????????????????????????????
```

---

## Module 2: Storage Management

### Purpose

Create, edit, import/export disk and ROM images.

### Storage Types

| Type | Formats | Operations |
|------|---------|-----------|
| **Disk Images** | .dsk, .do, .po, .nib, .woz | Create, format, sector edit, file extract |
| **ROM Images** | .rom, .bin | Import, verify, patch |
| **Tape Images** | .wav, .cas | Import, export (future) |

### IStorageService Interface

```csharp
/// <summary>
/// Manages disk and ROM image storage.
/// </summary>
public interface IStorageService
{
    /// <summary>Gets the storage library location.</summary>
    string LibraryPath { get; }
    
    /// <summary>Gets all known disk images.</summary>
    IReadOnlyList<DiskImageInfo> DiskImages { get; }
    
    /// <summary>Gets all known ROM images.</summary>
    IReadOnlyList<RomImageInfo> RomImages { get; }
    
    /// <summary>Creates a new blank disk image.</summary>
    Task<DiskImageInfo> CreateDiskAsync(DiskFormat format, string name);
    
    /// <summary>Imports a disk image from file.</summary>
    Task<DiskImageInfo> ImportDiskAsync(string path);
    
    /// <summary>Exports a disk image to file.</summary>
    Task ExportDiskAsync(DiskImageInfo disk, string path, DiskFormat format);
    
    /// <summary>Opens a disk image for editing.</summary>
    Task<IDiskEditor> OpenDiskEditorAsync(DiskImageInfo disk);
    
    /// <summary>Imports a ROM image from file.</summary>
    Task<RomImageInfo> ImportRomAsync(string path, RomType type);
    
    /// <summary>Verifies ROM integrity against known checksums.</summary>
    Task<RomVerificationResult> VerifyRomAsync(RomImageInfo rom);
    
    /// <summary>Scans library for changes.</summary>
    Task RefreshLibraryAsync();
}

public enum DiskFormat
{
    Dos33_140K,         // DOS 3.3, 140KB
    Dos33_800K,         // DOS 3.3, 800KB (3.5")
    ProDos_140K,        // ProDOS, 140KB
    ProDos_800K,        // ProDOS, 800KB
    ProDos_32MB,        // ProDOS, 32MB hard disk
    Nib,                // Raw nibble format
    Woz,                // WOZ 2.0 flux-level
}
```

### Disk Editor Features

```
???????????????????????????????????????????????????????????????????
? Disk Editor: MyGame.dsk                                   [?][?][×] ?
???????????????????????????????????????????????????????????????????
? [Catalog] [Sector Edit] [Track Map] [Files]                    ?
???????????????????????????????????????????????????????????????????
? ?? Catalog ????????????????????????????????????????????????????? ?
? ? DISK VOLUME 254                                              ? ?
? ?                                                              ? ?
? ?  A 002 HELLO                                                 ? ?
? ?  B 034 GAME.BIN                                              ? ?
? ?  A 015 STARTUP                                               ? ?
? ?  T 003 README                                                ? ?
? ?                                                              ? ?
? ? FREE SECTORS: 412                                            ? ?
? ?                                                              ? ?
? ? [Extract Selected] [Delete] [Import File]                    ? ?
? ???????????????????????????????????????????????????????????????? ?
?                                                                 ?
? ?? Sector View (T17, S0) ??????????????????????????????????????? ?
? ? 00: 04 11 0F 00 00 00 00 00  FE 00 00 00 00 00 00 00         ? ?
? ? 10: 00 00 00 00 00 00 00 00  00 00 00 00 00 00 00 00         ? ?
? ? ...                                                          ? ?
? ?                                    [Apply] [Revert]          ? ?
? ???????????????????????????????????????????????????????????????? ?
???????????????????????????????????????????????????????????????????
```

---

## Module 3: Display and Input

### Purpose

Render video output in all supported modes with accurate scaling and color.

### Display Modes (Pocket2e)

| Mode | Resolution | Colors | Memory |
|------|-----------|--------|--------|
| **Text 40** | 40×24 | 16 (MouseText) | $0400-$07FF |
| **Text 80** | 80×24 | 16 | $0400-$07FF + aux |
| **Lo-Res** | 40×48 | 16 | $0400-$07FF |
| **Hi-Res** | 280×192 | 6 | $2000-$3FFF |
| **Double Hi-Res** | 560×192 | 16 | $2000-$3FFF + aux |
| **Mixed** | Text bottom 4 lines | varies | varies |

### IDisplayService Interface

```csharp
/// <summary>
/// Manages video display rendering.
/// </summary>
public interface IDisplayService
{
    /// <summary>Gets the current display mode.</summary>
    DisplayMode CurrentMode { get; }
    
    /// <summary>Gets the display scaling factor.</summary>
    double Scale { get; set; }
    
    /// <summary>Gets or sets scanline emulation.</summary>
    bool ScanlineEffect { get; set; }
    
    /// <summary>Gets or sets color palette.</summary>
    ColorPalette Palette { get; set; }
    
    /// <summary>Attaches to a machine's video output.</summary>
    void AttachMachine(IMachine machine);
    
    /// <summary>Detaches from current machine.</summary>
    void Detach();
    
    /// <summary>Renders current frame to bitmap.</summary>
    void RenderFrame(WriteableBitmap target);
    
    /// <summary>Event raised when display mode changes.</summary>
    event EventHandler<DisplayModeChangedEventArgs>? ModeChanged;
}

public enum ColorPalette
{
    NTSC,               // Original NTSC artifact colors
    RGB,                // Clean RGB (IIgs-style)
    Monochrome,         // Green phosphor
    Amber,              // Amber phosphor
    Custom,             // User-defined
}
```

### Video Display Control

The `VideoDisplay` control handles rendering and input capture:

```csharp
/// <summary>
/// Apple II video display control with integrated input handling.
/// </summary>
public partial class VideoDisplay : UserControl
{
    public static readonly StyledProperty<IMachine?> MachineProperty = ...;
    public static readonly StyledProperty<double> ScaleProperty = ...;
    public static readonly StyledProperty<bool> CaptureInputProperty = ...;
    
    private readonly IDisplayService _display;
    private readonly IInputService _input;
    private WriteableBitmap _framebuffer;
    
    protected override void OnRender(DrawingContext context)
    {
        _display.RenderFrame(_framebuffer);
        context.DrawImage(_framebuffer, ...);
    }
    
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (CaptureInput && Machine is not null)
        {
            _input.HandleKeyDown(Machine, e);
            e.Handled = true;
        }
    }
}
```

### Input Service

```csharp
/// <summary>
/// Translates host input to guest input events.
/// </summary>
public interface IInputService
{
    /// <summary>Gets or sets the keyboard mapping.</summary>
    KeyboardMapping KeyMapping { get; set; }
    
    /// <summary>Handles a key down event.</summary>
    void HandleKeyDown(IMachine machine, KeyEventArgs e);
    
    /// <summary>Handles a key up event.</summary>
    void HandleKeyUp(IMachine machine, KeyEventArgs e);
    
    /// <summary>Handles mouse movement.</summary>
    void HandleMouseMove(IMachine machine, Point position, Size displaySize);
    
    /// <summary>Handles mouse button.</summary>
    void HandleMouseButton(IMachine machine, int button, bool pressed);
    
    /// <summary>Injects text (paste operation).</summary>
    Task InjectTextAsync(IMachine machine, string text);
}
```

### Display Scaling

Modern displays require clean scaling from the low-resolution Apple II output:

| Scaling Mode | Description | Best For |
|-------------|-------------|----------|
| **Integer** | Exact 2×, 3×, 4× | Crisp pixels |
| **Aspect-Correct** | Maintain 4:3 ratio | Authenticity |
| **Fill** | Fill window | Convenience |
| **Custom** | User-defined | Flexibility |

---

## Module 4: Debug Integration

### Purpose

Integrate the existing debug console infrastructure with the GUI.

### Architecture

```
????????????????????????????????????????????
?              Debug Console View           ?
?  ???????????????????????????????????????? ?
?  ? > step                               ? ?
?  ? PC=$1000 A=$00 X=$FF Y=$00 SP=$FF    ? ?
?  ? > mem 1000 20                        ? ?
?  ? 1000: A9 00 8D 00 C0 4C 00 10...     ? ?
?  ? > _                                  ? ?
?  ???????????????????????????????????????? ?
?                                          ?
?  ?? Registers ??? ?? Stack ???????????? ?
?  ? A:  $00      ? ? $01FF: $00        ? ?
?  ? X:  $FF      ? ? $01FE: $10        ? ?
?  ? Y:  $00      ? ? $01FD: $00        ? ?
?  ? SP: $FF      ? ? $01FC: $00        ? ?
?  ? PC: $1000    ? ?                   ? ?
?  ? P:  NV-BDIZC ? ?                   ? ?
?  ?    00100100  ? ?                   ? ?
?  ???????????????? ????????????????????? ?
????????????????????????????????????????????
```

### IDebugService Interface

```csharp
/// <summary>
/// Provides debug integration with running machines.
/// </summary>
public interface IDebugService
{
    /// <summary>Attaches debugger to a machine.</summary>
    Task<IDebugSession> AttachAsync(IMachine machine);
    
    /// <summary>Detaches from a machine.</summary>
    Task DetachAsync(IDebugSession session);
    
    /// <summary>Gets active debug sessions.</summary>
    IReadOnlyList<IDebugSession> Sessions { get; }
}

/// <summary>
/// Represents an active debug session.
/// </summary>
public interface IDebugSession
{
    /// <summary>Gets the attached machine.</summary>
    IMachine Machine { get; }
    
    /// <summary>Gets the command dispatcher.</summary>
    ICommandDispatcher CommandDispatcher { get; }
    
    /// <summary>Gets the debug context.</summary>
    IDebugContext Context { get; }
    
    /// <summary>Executes a debug command.</summary>
    Task<CommandResult> ExecuteCommandAsync(string command);
    
    /// <summary>Gets current CPU state.</summary>
    CpuState GetCpuState();
    
    /// <summary>Reads memory range.</summary>
    ReadOnlyMemory<byte> ReadMemory(Addr address, int length);
    
    /// <summary>Sets a breakpoint.</summary>
    void SetBreakpoint(Addr address);
    
    /// <summary>Clears a breakpoint.</summary>
    void ClearBreakpoint(Addr address);
    
    /// <summary>Event raised when execution stops.</summary>
    event EventHandler<DebugStopEventArgs>? Stopped;
}
```

### Debug Console ViewModel

```csharp
public partial class DebugConsoleViewModel : ViewModelBase
{
    [ObservableProperty]
    private IDebugSession? _session;
    
    [ObservableProperty]
    private string _commandInput = "";
    
    [ObservableProperty]
    private ObservableCollection<DebugOutputLine> _output = [];
    
    [ObservableProperty]
    private CpuStateViewModel? _cpuState;
    
    [ObservableProperty]
    private ObservableCollection<BreakpointViewModel> _breakpoints = [];
    
    [RelayCommand]
    private async Task ExecuteCommandAsync()
    {
        if (Session is null || string.IsNullOrEmpty(CommandInput))
            return;
        
        Output.Add(new DebugOutputLine($"> {CommandInput}", OutputType.Command));
        
        var result = await Session.ExecuteCommandAsync(CommandInput);
        
        if (result.Success)
            Output.Add(new DebugOutputLine(result.Output, OutputType.Result));
        else
            Output.Add(new DebugOutputLine(result.Error, OutputType.Error));
        
        CommandInput = "";
        RefreshCpuState();
    }
    
    [RelayCommand]
    private async Task StepAsync()
    {
        await ExecuteCommandAsync("step");
    }
    
    [RelayCommand]
    private async Task RunAsync()
    {
        await ExecuteCommandAsync("run");
    }
}
```

---

## Module 5: Development Tools

### Purpose

Provide integrated assembly language editor and compiler.

### Assembly Editor Features

| Feature | Description |
|---------|-------------|
| **Syntax Highlighting** | 6502/65C02/65816/65832 mnemonics, labels, directives |
| **Auto-completion** | Instruction mnemonics, defined labels |
| **Error Markers** | Inline error display |
| **Symbol Browser** | Navigate to labels/constants |
| **Memory Map** | View code placement |
| **Build Integration** | Assemble and load into emulator |

### IEditorService Interface

```csharp
/// <summary>
/// Provides assembly language editing and compilation.
/// </summary>
public interface IEditorService
{
    /// <summary>Creates a new source file.</summary>
    ISourceDocument CreateDocument(string name, SourceLanguage language);
    
    /// <summary>Opens an existing source file.</summary>
    Task<ISourceDocument> OpenDocumentAsync(string path);
    
    /// <summary>Assembles a document.</summary>
    Task<AssemblyResult> AssembleAsync(ISourceDocument document, AssemblyOptions options);
    
    /// <summary>Loads assembled code into a machine.</summary>
    Task LoadIntoMachineAsync(AssemblyResult result, IMachine machine, Addr loadAddress);
    
    /// <summary>Gets available assembler dialects.</summary>
    IReadOnlyList<AssemblerDialect> Dialects { get; }
}

public enum SourceLanguage
{
    Assembly6502,       // 6502 assembly
    Assembly65C02,      // 65C02 with extensions
    Assembly65816,      // 65816 native/emulation
    Assembly65832,      // 65832 full mode
    BasicApplesoft,     // Applesoft BASIC (future)
}
```

### Editor View

```
???????????????????????????????????????????????????????????????????
? Assembly Editor: game.asm                              [?][?][×] ?
???????????????????????????????????????????????????????????????????
? [New] [Open] [Save] [Build] [Run] | [65C02 ?] [Merlin ?]       ?
???????????????????????????????????????????????????????????????????
? ?? Source ??????????????????????? ?? Symbols ????????????????? ?
? ?  1 ?         ORG $0800        ? ? MAIN          $0800      ? ?
? ?  2 ?                          ? ? LOOP          $0805      ? ?
? ?  3 ? MAIN    LDA #$00         ? ? COUNTER       $0820      ? ?
? ?  4 ?         STA COUNTER      ? ?                          ? ?
? ?  5 ? LOOP    INC COUNTER      ? ?                          ? ?
? ?  6 ?         LDA COUNTER      ? ?                          ? ?
? ?  7 ?         CMP #$FF         ? ?                          ? ?
? ?  8 ?         BNE LOOP         ? ?                          ? ?
? ?  9 ?         RTS              ? ?                          ? ?
? ? 10 ?                          ? ?                          ? ?
? ? 11 ? COUNTER DFB $00          ? ?                          ? ?
? ????????????????????????????????? ???????????????????????????? ?
?                                                                 ?
? ?? Output ?????????????????????????????????????????????????????? ?
? ? Assembling game.asm...                                       ? ?
? ? Pass 1: 11 lines, 5 symbols                                  ? ?
? ? Pass 2: Code generation complete                             ? ?
? ? Output: 18 bytes at $0800-$0811                              ? ?
? ? Assembly successful.                                         ? ?
? ???????????????????????????????????????????????????????????????? ?
???????????????????????????????????????????????????????????????????
```

---

## Future Considerations: Hypervisor and Virtual CPU Hosting

As we approach 65832 implementation, the UI must support:

### Multi-Instance Management

```csharp
/// <summary>
/// Manages multiple virtual machines under hypervisor control.
/// </summary>
public interface IHypervisorService
{
    /// <summary>Gets the host (65832) machine.</summary>
    IMachine HostMachine { get; }
    
    /// <summary>Gets all guest machines.</summary>
    IReadOnlyList<IMachine> GuestMachines { get; }
    
    /// <summary>Creates a guest machine (IIe/IIc/IIgs personality).</summary>
    Task<IMachine> CreateGuestAsync(MachinePersonality personality);
    
    /// <summary>Destroys a guest machine.</summary>
    Task DestroyGuestAsync(IMachine guest);
    
    /// <summary>Gets resource allocation for a guest.</summary>
    GuestResources GetResources(IMachine guest);
}
```

### UI Considerations for Hypervisor Mode

1. **Machine Switcher** - Quick switch between host and guest displays
2. **Resource Monitor** - CPU time, memory usage per guest
3. **Guest Console** - Debug console per guest machine
4. **Shared Storage** - Disk images accessible to multiple guests

---

## Styling and Theming

### Design Principles

1. **Retro-Modern** - Clean modern UI with retro accent colors
2. **Dark Theme Default** - Easier on eyes for long sessions
3. **Customizable** - User-selectable themes
4. **Accessible** - High contrast options

### Color Palette

| Element | Light Theme | Dark Theme |
|---------|-------------|------------|
| Background | #FFFFFF | #1E1E1E |
| Surface | #F5F5F5 | #252526 |
| Primary | #007ACC | #0E639C |
| Secondary | #6B9F00 | #4EC9B0 |
| Error | #D32F2F | #F44747 |
| Text | #212121 | #D4D4D4 |

### Fonts

| Usage | Font | Fallback |
|-------|------|----------|
| UI Text | Segoe UI / SF Pro | System default |
| Code/Terminal | Cascadia Mono | Consolas, monospace |
| Apple II Display | Custom (ROM-based) | N/A |

---

## Configuration and Persistence

### Settings Storage

```csharp
/// <summary>
/// Application settings model.
/// </summary>
public record AppSettings
{
    public string LibraryPath { get; init; } = "./Library";
    public string Theme { get; init; } = "Dark";
    public DisplaySettings Display { get; init; } = new();
    public InputSettings Input { get; init; } = new();
    public DebugSettings Debug { get; init; } = new();
    public EditorSettings Editor { get; init; } = new();
    public IReadOnlyList<MachineProfile> Profiles { get; init; } = [];
}
```

### File Formats

| Data | Format | Location |
|------|--------|----------|
| Settings | JSON | ~/.backpocket/settings.json |
| Profiles | JSON | ~/.backpocket/profiles/ |
| Disk Library | Directory | ~/.backpocket/disks/ |
| ROM Library | Directory | ~/.backpocket/roms/ |

---

## Testing Strategy

### Unit Tests

1. **ViewModel tests** - Command execution, property binding
2. **Service tests** - Machine lifecycle, storage operations
3. **Converter tests** - Value conversion correctness

### Integration Tests

1. **Machine creation** - Profile ? running instance
2. **Storage roundtrip** - Create ? save ? reload
3. **Debug session** - Attach ? command ? state inspection

### UI Tests

1. **Avalonia Headless** - Automated UI testing
2. **Snapshot testing** - Visual regression detection

---

## Implementation Phases

### Phase 1: Foundation (Target: 4 weeks)

| Task | Priority | Estimate |
|------|----------|----------|
| Project setup with Avalonia | High | 2 days |
| MainWindow shell | High | 2 days |
| Machine Manager (basic) | High | 1 week |
| VideoDisplay control | High | 1 week |
| Input handling (keyboard) | High | 3 days |
| Settings persistence | Medium | 2 days |

### Phase 2: Storage & Debug (Target: 3 weeks)

| Task | Priority | Estimate |
|------|----------|----------|
| Storage Manager | High | 1 week |
| Disk image creation | High | 3 days |
| Debug Console integration | High | 1 week |
| Register/memory views | Medium | 3 days |

### Phase 3: Editor & Polish (Target: 3 weeks)

| Task | Priority | Estimate |
|------|----------|----------|
| Assembly editor | Medium | 1 week |
| Syntax highlighting | Medium | 3 days |
| Build integration | Medium | 3 days |
| Theming | Low | 2 days |
| Documentation | Medium | 2 days |

### Phase 4: PocketGS/PocketME (Target: Future)

| Task | Priority | Estimate |
|------|----------|----------|
| Super Hi-Res display | Medium | 1 week |
| ADB input model | Medium | 3 days |
| 65832 mode support | Medium | 2 weeks |
| Hypervisor UI | Low | 2 weeks |

---

## Dependencies

### NuGet Packages

```xml
<!-- Avalonia -->
<PackageReference Include="Avalonia" Version="11.*" />
<PackageReference Include="Avalonia.Desktop" Version="11.*" />
<PackageReference Include="Avalonia.Themes.Fluent" Version="11.*" />
<PackageReference Include="Avalonia.Fonts.Inter" Version="11.*" />

<!-- MVVM -->
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />

<!-- Rendering -->
<PackageReference Include="SkiaSharp" Version="2.*" />

<!-- Editor (future) -->
<PackageReference Include="AvaloniaEdit" Version="11.*" />

<!-- DI -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.*" />
```

---

## Open Questions

### 1. Should the display be a separate window or docked?

**Recommendation:** Offer both. Default to docked with option to pop out.

### 2. How to handle multiple monitors?

**Recommendation:** Allow display pop-out to any monitor. Save position per profile.

### 3. Audio output approach?

**Recommendation:** Use NAudio on Windows, SDL on Linux/macOS. Abstract behind IAudioService.

### 4. Gamepad/joystick support?

**Recommendation:** Defer to Phase 2. Map analog stick to paddle values.

---

*Document last updated: 2025-01-13*
