# Emulator UI Specification

**Document Purpose:** Design specification for the Avalonia-based emulator user interface.  
**Version:** 1.1  
**Date:** 2025-12-27  
**Target Framework:** Avalonia UI 11.x on .NET 10.0

---

## Overview

The emulator UI provides a graphical frontend for the BackPocket emulator family, supporting:

1. **Machine Management** - Profile definition, instance lifecycle, bus runtime
2. **Storage Management** - Disk and ROM image creation/manipulation
3. **Display and Input** - Video rendering in all supported modes, keyboard/mouse translation
4. **Debug Integration** - Live debugging of running instances
5. **Development Tools** - Assembly editor/compiler, BASIC editor (future)
6. **Pop-Out Windows** - Detachable windows for display, debug console, and editors
7. **Settings Management** - Comprehensive configuration with extensible settings panels

### Machine Personalities

| Personality | Display Modes | Input Model | Status |
|-------------|--------------|-------------|--------|
| **Pocket2e** | Text 40/80, Lo-Res, Hi-Res, Double Hi-Res | Apple IIe keyboard, mouse | Phase 1 |
| **PocketGS** | All IIe modes + Super Hi-Res (320/640�200) | ADB keyboard/mouse | Phase 2 |
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
├ App.axaml                     # Application definition
├ App.axaml.cs                  # Application code-behind
│
├ Converters/                   # Value converters
│   ├ BoolToVisibilityConverter.cs
│   ├ AddressFormatConverter.cs
│   └ MachineStateConverter.cs
│
├ Models/                       # UI data models
│   ├ MachineProfile.cs         # Profile definition
│   ├ DiskImage.cs              # Disk image metadata
│   ├ RomImage.cs               # ROM image metadata
│   ├ DisplaySettings.cs        # Display configuration
│   ├ AppSettings.cs            # Application settings model
│   ├ WindowStateInfo.cs        # Window state persistence
│   └ WindowLayoutState.cs      # Complete window layout
│
├ Services/                     # UI services
│   ├ IMachineService.cs        # Machine lifecycle
│   ├ IStorageService.cs        # Disk/ROM management
│   ├ IDisplayService.cs        # Video rendering
│   ├ IInputService.cs          # Keyboard/mouse handling
│   ├ IDebugService.cs          # Debug integration
│   ├ ISettingsService.cs       # Settings management
│   ├ IWindowManager.cs         # Pop-out window management
│   ├ IEventAggregator.cs       # Inter-window communication
│   ├ IPathValidator.cs         # Path validation
│   └ ISettingsMigrator.cs      # Settings schema migration
│
├ ViewModels/                   # MVVM ViewModels
│   ├ MainWindowViewModel.cs
│   ├ MachineManagerViewModel.cs
│   ├ StorageManagerViewModel.cs
│   ├ DisplayViewModel.cs
│   ├ DebugConsoleViewModel.cs
│   ├ EditorViewModel.cs
│   └ Settings/                 # Settings ViewModels
│       ├ SettingsWindowViewModel.cs
│       ├ GeneralSettingsViewModel.cs
│       ├ LibrarySettingsViewModel.cs
│       ├ DisplaySettingsViewModel.cs
│       ├ InputSettingsViewModel.cs
│       ├ DebugSettingsViewModel.cs
│       ├ EditorSettingsViewModel.cs
│       └ AboutViewModel.cs
│
├ Views/                        # AXAML Views
│   ├ MainWindow.axaml
│   ├ MachineManagerView.axaml
│   ├ StorageManagerView.axaml
│   ├ DisplayView.axaml
│   ├ DebugConsoleView.axaml
│   ├ EditorView.axaml
│   ├ PopOutWindow.axaml        # Generic pop-out container
│   └ Settings/                 # Settings Views
│       ├ SettingsWindow.axaml
│       ├ GeneralSettingsView.axaml
│       ├ LibrarySettingsView.axaml
│       ├ DisplaySettingsView.axaml
│       ├ InputSettingsView.axaml
│       ├ DebugSettingsView.axaml
│       ├ EditorSettingsView.axaml
│       └ AboutView.axaml
│
├ Controls/                     # Custom controls
│   ├ VideoDisplay.axaml        # Apple II display rendering
│   ├ DebugTerminal.axaml       # Debug console terminal
│   ├ HexEditor.axaml           # Memory/disk hex editor
│   ├ RegisterPanel.axaml       # CPU register display
│   ├ DisassemblyView.axaml     # Live disassembly
│   └ PathBrowser.axaml         # Directory browser control
│
└ Resources/                    # Assets
    ├ Fonts/                    # Apple II character ROMs
    ├ Icons/                    # UI icons
    ├ Styles/                   # AXAML styles
    └ Schemas/                  # JSON schemas
        └ settings-v1.json      # Settings schema definition
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
? Machine Manager                                           [?][?][�] ?
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
? Disk Editor: MyGame.dsk                                   [?][?][�] ?
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
| **Text 40** | 40�24 | 16 (MouseText) | $0400-$07FF |
| **Text 80** | 80�24 | 16 | $0400-$07FF + aux |
| **Lo-Res** | 40�48 | 16 | $0400-$07FF |
| **Hi-Res** | 280�192 | 6 | $2000-$3FFF |
| **Double Hi-Res** | 560�192 | 16 | $2000-$3FFF + aux |
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
| **Integer** | Exact 2�, 3�, 4� | Crisp pixels |
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
? Assembly Editor: game.asm                              [?][?][�] ?
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

## Module 6: Pop-Out Window Architecture

### Purpose

Provide a flexible window management system that allows users to detach key UI components into
separate windows for multi-monitor workflows, improved focus, or personal preference.

### Supported Pop-Out Windows

| Component | Default State | Pop-Out Supported |
|-----------|--------------|-------------------|
| **Video Display** | Docked | Yes |
| **Debug Console** | Docked | Yes |
| **Assembly Editor** | Docked | Yes |
| **Hex Editor** | Docked | Yes |
| **Register Panel** | Docked (Debug Console) | No (embedded) |
| **Storage Manager** | Docked | No |

### Window Management Pattern

```csharp
/// <summary>
/// Manages pop-out window lifecycle and state.
/// </summary>
public interface IWindowManager
{
    /// <summary>Gets all active pop-out windows.</summary>
    IReadOnlyList<IPopOutWindow> PopOutWindows { get; }

    /// <summary>Creates a pop-out window for the specified component.</summary>
    Task<IPopOutWindow> CreatePopOutAsync(PopOutComponent component, IMachine? machine = null);

    /// <summary>Docks a pop-out window back into the main window.</summary>
    Task DockWindowAsync(IPopOutWindow window);

    /// <summary>Restores all saved window states for a profile.</summary>
    Task RestoreWindowStatesAsync(string profileId);

    /// <summary>Saves current window states for a profile.</summary>
    Task SaveWindowStatesAsync(string profileId);

    /// <summary>Event raised when a pop-out window is created.</summary>
    event EventHandler<PopOutWindowEventArgs>? WindowCreated;

    /// <summary>Event raised when a pop-out window is closed.</summary>
    event EventHandler<PopOutWindowEventArgs>? WindowClosed;
}

/// <summary>
/// Represents a detached pop-out window.
/// </summary>
public interface IPopOutWindow
{
    /// <summary>Gets the unique identifier for this window instance.</summary>
    string WindowId { get; }

    /// <summary>Gets the component type displayed in this window.</summary>
    PopOutComponent ComponentType { get; }

    /// <summary>Gets or sets the associated machine (for display/debug windows).</summary>
    IMachine? Machine { get; set; }

    /// <summary>Gets the current window state.</summary>
    WindowState State { get; }

    /// <summary>Brings the window to the foreground.</summary>
    void BringToFront();

    /// <summary>Closes the window and optionally docks content.</summary>
    Task CloseAsync(bool dockContent = false);
}

public enum PopOutComponent
{
    VideoDisplay,
    DebugConsole,
    AssemblyEditor,
    HexEditor,
}
```

### Window State Persistence

Window positions, sizes, and states are saved per user profile and per machine profile:

```csharp
/// <summary>
/// Persisted window state information.
/// </summary>
public record WindowStateInfo
{
    /// <summary>Gets the component type.</summary>
    public required PopOutComponent ComponentType { get; init; }

    /// <summary>Gets whether the window is popped out or docked.</summary>
    public bool IsPopOut { get; init; }

    /// <summary>Gets the window position (screen coordinates).</summary>
    public Point? Position { get; init; }

    /// <summary>Gets the window size.</summary>
    public Size? Size { get; init; }

    /// <summary>Gets the monitor identifier (for multi-monitor setups).</summary>
    public string? MonitorId { get; init; }

    /// <summary>Gets whether the window is maximized.</summary>
    public bool IsMaximized { get; init; }

    /// <summary>Gets the associated machine profile ID (if machine-specific).</summary>
    public string? MachineProfileId { get; init; }
}

/// <summary>
/// Complete window layout state for a profile.
/// </summary>
public record WindowLayoutState
{
    /// <summary>Gets the layout version for migration support.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Gets window states for this layout.</summary>
    public IReadOnlyList<WindowStateInfo> Windows { get; init; } = [];

    /// <summary>Gets the main window state.</summary>
    public WindowStateInfo? MainWindow { get; init; }
}
```

### Inter-Window Communication

Pop-out windows maintain live communication with the main window and running instances:

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Event Bus (IEventAggregator)                │
└─────────────────────────────────────────────────────────────────────┘
           │                    │                    │
           ▼                    ▼                    ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│   Main Window    │  │ Video Display    │  │ Debug Console    │
│                  │  │ (Pop-Out)        │  │ (Pop-Out)        │
│ ┌──────────────┐ │  │                  │  │                  │
│ │ Machine Mgr  │ │  │ ┌──────────────┐ │  │ ┌──────────────┐ │
│ │              │ │  │ │ IDisplaySvc  │ │  │ │ IDebugSvc    │ │
│ │  IMachine────┼─┼──┼─┼──────────────┼─┼──┼─┼──────────────┘ │
│ └──────────────┘ │  │ └──────────────┘ │  │ └──────────────┐ │
│                  │  │                  │  │ │ IDebugSession│ │
└──────────────────┘  └──────────────────┘  └─┴──────────────┴─┘
```

```csharp
/// <summary>
/// Coordinates communication between windows.
/// </summary>
public interface IEventAggregator
{
    /// <summary>Publishes an event to all subscribers.</summary>
    void Publish<TEvent>(TEvent eventData) where TEvent : class;

    /// <summary>Subscribes to events of a specific type.</summary>
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
}

// Key events for inter-window communication
public record MachineStateChangedEvent(IMachine Machine, MachineState NewState);
public record BreakpointHitEvent(IMachine Machine, Addr Address);
public record DisplayModeChangedEvent(IMachine Machine, DisplayMode NewMode);
public record WindowFocusRequestEvent(PopOutComponent ComponentType, string? MachineId);
```

### Main Window Close Behavior

When the main window is closed:

1. **Running Instances**: All running machine instances are gracefully stopped
2. **Pop-Out Windows**: All pop-out windows are closed (with optional save prompt)
3. **State Persistence**: Current window layout is saved to the active profile
4. **Unsaved Work**: User is prompted to save any unsaved editor content

```csharp
/// <summary>
/// Handles application shutdown sequence.
/// </summary>
public interface IShutdownCoordinator
{
    /// <summary>Initiates graceful shutdown.</summary>
    Task<bool> RequestShutdownAsync();

    /// <summary>Checks for unsaved work across all windows.</summary>
    Task<IReadOnlyList<UnsavedWorkItem>> GetUnsavedWorkAsync();

    /// <summary>Forces immediate shutdown (data may be lost).</summary>
    void ForceShutdown();
}
```

### UX Best Practices for Pop-Out Windows

#### Creating Pop-Outs

| Method | Description |
|--------|-------------|
| **Menu** | View → Pop Out → [Component] |
| **Context Menu** | Right-click tab → "Pop Out" |
| **Drag** | Drag tab outside main window bounds |
| **Keyboard** | Ctrl+Shift+P (with component focused) |

#### Docking Pop-Outs

| Method | Description |
|--------|-------------|
| **Menu** | Window menu in pop-out → "Dock to Main" |
| **Drag** | Drag pop-out title bar into main window dock zones |
| **Close Button** | Close pop-out (prompts for dock vs. close) |
| **Keyboard** | Ctrl+Shift+D (in pop-out window) |

#### Visual Feedback

```
┌────────────────────────────────────────────────────────────────────┐
│ BackPocket Emulator                                   [─][□][×]    │
├────────────────────────────────────────────────────────────────────┤
│ ┌─Machines─┬─Display─┬─Debug─┐                                     │
│ │          │ ⎆       │       │   ← Pop-out indicator icon          │
│ │          │         │       │                                     │
│ └──────────┴─────────┴───────┘                                     │
│                                                                    │
│ ┌────────────────────────────────────────┐                         │
│ │                                        │                         │
│ │           [Video Display]              │  ← Docked state         │
│ │                                        │                         │
│ │          ┌─────────────────┐           │                         │
│ │          │  ⎆ Pop Out      │           │  ← Right-click menu     │
│ │          │  ⚙ Settings     │           │                         │
│ │          └─────────────────┘           │                         │
│ └────────────────────────────────────────┘                         │
└────────────────────────────────────────────────────────────────────┘
```

#### Window Management Toolbar

Pop-out windows include a minimal toolbar:

```
┌────────────────────────────────────────────────────────────────────┐
│ Video Display - My Apple IIe                    [⎆][─][□][×]       │
│────────────────────────────────────────────────────────────────────│
│ │ Machine: [My Apple IIe ▾] │ [Dock] [Settings]                    │
│────────────────────────────────────────────────────────────────────│
│                                                                    │
│                    [Display Content]                               │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## Module 7: Settings Panel Design

### Purpose

Provide a comprehensive, organized, and extensible settings interface for configuring all aspects
of the emulator application.

### Settings Panel Structure

The Settings panel uses a tree-view navigation with grouped categories:

```
┌────────────────────────────────────────────────────────────────────┐
│ Settings                                              [─][□][×]    │
├────────────────────────────────────────────────────────────────────┤
│ ┌──────────────┬───────────────────────────────────────────────────│
│ │ ▼ General    │                                                   │
│ │   Startup    │  General Settings                                 │
│ │   Updates    │  ─────────────────────────────────────────────    │
│ │              │                                                   │
│ │ ▼ Library    │  Startup:                                         │
│ │   Paths      │  ┌─────────────────────────────────────────────┐  │
│ │   Auto-scan  │  │ ☑ Load last profile on startup              │  │
│ │              │  │ ☐ Start emulator paused                     │  │
│ │ ▼ Display    │  │ ☑ Restore window layout                     │  │
│ │   Video      │  └─────────────────────────────────────────────┘  │
│ │   Scaling    │                                                   │
│ │   Colors     │  Language:                                        │
│ │              │  ┌─────────────────────────────────────────────┐  │
│ │ ▼ Input      │  │ English (US)                             ▾ │  │
│ │   Keyboard   │  └─────────────────────────────────────────────┘  │
│ │   Joystick   │                                                   │
│ │              │  Theme:                                           │
│ │ ▼ Debug      │  ┌─────────────────────────────────────────────┐  │
│ │   Console    │  │ ○ Light  ● Dark  ○ System                   │  │
│ │   Breakpts   │  └─────────────────────────────────────────────┘  │
│ │              │                                                   │
│ │ ▼ Editor     │                                                   │
│ │   Fonts      │                                                   │
│ │   Syntax     │                                                   │
│ │              │                                                   │
│ │   About      │  [Reset to Defaults]        [Apply] [Cancel] [OK] │
│ └──────────────┴───────────────────────────────────────────────────│
└────────────────────────────────────────────────────────────────────┘
```

### Settings Categories

#### General Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Load last profile | Boolean | true | Auto-load last used profile on startup |
| Start paused | Boolean | false | Start emulator in paused state |
| Restore window layout | Boolean | true | Restore pop-out windows on startup |
| Language | Enum | System | UI language preference |
| Theme | Enum | Dark | Light, Dark, or System theme |
| Check for updates | Boolean | true | Auto-check for updates |
| Telemetry | Boolean | false | Send anonymous usage data |

#### Library Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Library root | Path | ~/.backpocket | Base directory for all storage |
| Disk images directory | Path | {Library}/disks | Location of disk images |
| ROM images directory | Path | {Library}/roms | Location of ROM images |
| Log files directory | Path | {Library}/logs | Location of log files |
| Save state directory | Path | {Library}/saves | Location of save states (future) |
| Auto-scan on startup | Boolean | true | Scan library for changes at startup |
| Watch for changes | Boolean | true | Monitor library for external changes |

#### Display Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Scaling mode | Enum | Integer | Integer, Aspect-correct, Fill, Custom |
| Scale factor | Integer | 2 | Display scale (1-8) |
| Scanline effect | Boolean | false | Simulate CRT scanlines |
| Color palette | Enum | NTSC | NTSC, RGB, Monochrome, Amber, Custom |
| Frame rate cap | Integer | 60 | Maximum frame rate |
| VSync | Boolean | true | Enable vertical sync |
| Full screen monitor | Integer | 0 | Monitor for full screen mode |

#### Input Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Keyboard mapping | Enum | Standard | Standard, Positional, Custom |
| Custom key map file | Path | null | Path to custom key mapping file |
| Mouse capture | Boolean | false | Auto-capture mouse in display |
| Joystick enabled | Boolean | false | Enable joystick/gamepad input |
| Joystick device | String | Auto | Selected joystick device |
| Paddle sensitivity | Integer | 50 | Analog paddle sensitivity (1-100) |

#### Debug Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Auto-attach debugger | Boolean | false | Attach debugger on instance start |
| Break on reset | Boolean | false | Break into debugger on reset |
| Log level | Enum | Info | Minimum log level |
| Log to file | Boolean | true | Write logs to file |
| Max log file size | Integer | 10 | Maximum log file size (MB) |
| Trace instructions | Boolean | false | Enable instruction tracing |
| Show cycle count | Boolean | true | Display cycle count in debugger |

#### Editor Settings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| Font family | String | Cascadia Mono | Editor font family |
| Font size | Integer | 12 | Editor font size |
| Tab size | Integer | 4 | Spaces per tab |
| Insert spaces | Boolean | true | Use spaces instead of tabs |
| Auto-complete | Boolean | true | Enable auto-completion |
| Syntax highlighting | Boolean | true | Enable syntax highlighting |
| Line numbers | Boolean | true | Show line numbers |
| Word wrap | Boolean | false | Wrap long lines |
| Assembler dialect | Enum | Merlin | Default assembler dialect |

### About Panel

```
┌────────────────────────────────────────────────────────────────────┐
│ About BackPocket Emulator                                          │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│                      [BackPocket Logo]                             │
│                                                                    │
│              BackPocket Emulator v1.0.0                            │
│                                                                    │
│    An Apple II family emulator with modern enhancements            │
│                                                                    │
│  ─────────────────────────────────────────────────────────────     │
│                                                                    │
│  Build Information:                                                │
│    Version:     1.0.0                                              │
│    Build Date:  2025-01-13                                         │
│    Commit:      abc123def456                                       │
│    Branch:      main                                               │
│                                                                    │
│  System Information:                                               │
│    OS:          Windows 11 (23H2)                                  │
│    .NET:        10.0.0                                             │
│    Avalonia:    11.0.0                                             │
│    Memory:      16.0 GB                                            │
│                                                                    │
│  ─────────────────────────────────────────────────────────────     │
│                                                                    │
│  Credits:                                                          │
│    Lead Developer: [Name]                                          │
│    Contributors:   See CONTRIBUTORS.md                             │
│                                                                    │
│  ─────────────────────────────────────────────────────────────     │
│                                                                    │
│  License: MIT License                                              │
│  [View License] [View on GitHub] [Check for Updates]               │
│                                                                    │
│  ─────────────────────────────────────────────────────────────     │
│                                                                    │
│            [Copy System Info]                   [Close]            │
└────────────────────────────────────────────────────────────────────┘
```

### Path Validation

All path settings are validated before saving:

```csharp
/// <summary>
/// Validates and normalizes path settings.
/// </summary>
public interface IPathValidator
{
    /// <summary>Validates a path for the specified purpose.</summary>
    PathValidationResult Validate(string path, PathPurpose purpose);

    /// <summary>Normalizes a path (expands variables, resolves relative paths).</summary>
    string Normalize(string path);

    /// <summary>Ensures a directory exists, creating it if necessary.</summary>
    Task<bool> EnsureDirectoryExistsAsync(string path);
}

public enum PathPurpose
{
    LibraryRoot,
    DiskImages,
    RomImages,
    LogFiles,
    SaveStates,
}

public record PathValidationResult
{
    public bool IsValid { get; init; }
    public string? NormalizedPath { get; init; }
    public string? ErrorMessage { get; init; }
    public PathValidationWarning[] Warnings { get; init; } = [];
}

public enum PathValidationWarning
{
    DirectoryDoesNotExist,
    InsufficientPermissions,
    LowDiskSpace,
    NetworkPath,
    RelativePath,
}
```

### Settings Extensibility

The settings system is designed for future extensibility:

```csharp
/// <summary>
/// Marker interface for settings page ViewModels.
/// </summary>
public interface ISettingsPage
{
    /// <summary>Gets the display name of this settings page.</summary>
    string DisplayName { get; }

    /// <summary>Gets the icon for this settings page.</summary>
    string IconKey { get; }

    /// <summary>Gets the parent category (null for root categories).</summary>
    string? ParentCategory { get; }

    /// <summary>Gets the sort order within the parent category.</summary>
    int SortOrder { get; }

    /// <summary>Loads settings into the page.</summary>
    Task LoadAsync();

    /// <summary>Saves settings from the page.</summary>
    Task SaveAsync();

    /// <summary>Resets settings to defaults.</summary>
    Task ResetToDefaultsAsync();

    /// <summary>Gets whether the page has unsaved changes.</summary>
    bool HasChanges { get; }
}

/// <summary>
/// Registry for settings pages, supporting plugin extensibility.
/// </summary>
public interface ISettingsPageRegistry
{
    /// <summary>Registers a settings page.</summary>
    void Register<TPage>() where TPage : class, ISettingsPage;

    /// <summary>Gets all registered settings pages.</summary>
    IReadOnlyList<ISettingsPage> GetPages();

    /// <summary>Gets settings pages for a category.</summary>
    IReadOnlyList<ISettingsPage> GetPagesForCategory(string category);
}
```

### Advanced Settings Hooks (Future)

The following advanced features are planned for future implementation:

| Feature | Description | Priority |
|---------|-------------|----------|
| **Per-Profile Overrides** | Override global settings per machine profile | Medium |
| **Import/Export Settings** | Export settings to JSON, import from file | Low |
| **Reset Defaults** | Reset individual categories or all settings | Medium |
| **Settings Search** | Search for settings by name or description | Low |
| **Settings Sync** | Sync settings across devices (cloud) | Future |

---

## Configuration and Persistence

### Settings Storage

```csharp
/// <summary>
/// Application settings model with versioning support.
/// </summary>
public record AppSettings
{
    /// <summary>Gets the settings schema version for migration.</summary>
    public int Version { get; init; } = 1;

    /// <summary>Gets or sets the general settings.</summary>
    public GeneralSettings General { get; init; } = new();

    /// <summary>Gets or sets the library path settings.</summary>
    public LibrarySettings Library { get; init; } = new();

    /// <summary>Gets or sets the display settings.</summary>
    public DisplaySettings Display { get; init; } = new();

    /// <summary>Gets or sets the input settings.</summary>
    public InputSettings Input { get; init; } = new();

    /// <summary>Gets or sets the debug settings.</summary>
    public DebugSettings Debug { get; init; } = new();

    /// <summary>Gets or sets the editor settings.</summary>
    public EditorSettings Editor { get; init; } = new();

    /// <summary>Gets or sets the window layout state.</summary>
    public WindowLayoutState WindowLayout { get; init; } = new();

    /// <summary>Gets or sets the machine profiles.</summary>
    public IReadOnlyList<MachineProfile> Profiles { get; init; } = [];

    /// <summary>Gets the ID of the last active profile.</summary>
    public string? LastProfileId { get; init; }
}

/// <summary>
/// General application settings.
/// </summary>
public record GeneralSettings
{
    public bool LoadLastProfile { get; init; } = true;
    public bool StartPaused { get; init; } = false;
    public bool RestoreWindowLayout { get; init; } = true;
    public string Language { get; init; } = "en-US";
    public string Theme { get; init; } = "Dark";
    public bool CheckForUpdates { get; init; } = true;
    public bool EnableTelemetry { get; init; } = false;
}

/// <summary>
/// Library path settings.
/// </summary>
public record LibrarySettings
{
    public string LibraryRoot { get; init; } = "~/.backpocket";
    public string DiskImagesPath { get; init; } = "{Library}/disks";
    public string RomImagesPath { get; init; } = "{Library}/roms";
    public string LogFilesPath { get; init; } = "{Library}/logs";
    public string SaveStatesPath { get; init; } = "{Library}/saves";
    public bool AutoScanOnStartup { get; init; } = true;
    public bool WatchForChanges { get; init; } = true;
}
```

### JSON Settings Schema

Settings are stored in a versioned JSON format with forward/backward compatibility:

```json
{
  "$schema": "https://backpocket.dev/schemas/settings-v1.json",
  "version": 1,
  "general": {
    "loadLastProfile": true,
    "startPaused": false,
    "restoreWindowLayout": true,
    "language": "en-US",
    "theme": "Dark",
    "checkForUpdates": true,
    "enableTelemetry": false
  },
  "library": {
    "libraryRoot": "~/.backpocket",
    "diskImagesPath": "{Library}/disks",
    "romImagesPath": "{Library}/roms",
    "logFilesPath": "{Library}/logs",
    "saveStatesPath": "{Library}/saves",
    "autoScanOnStartup": true,
    "watchForChanges": true
  },
  "display": {
    "scalingMode": "Integer",
    "scaleFactor": 2,
    "scanlineEffect": false,
    "colorPalette": "NTSC",
    "frameRateCap": 60,
    "vSync": true,
    "fullScreenMonitor": 0
  },
  "input": {
    "keyboardMapping": "Standard",
    "customKeyMapFile": null,
    "mouseCapture": false,
    "joystickEnabled": false,
    "joystickDevice": "Auto",
    "paddleSensitivity": 50
  },
  "debug": {
    "autoAttachDebugger": false,
    "breakOnReset": false,
    "logLevel": "Info",
    "logToFile": true,
    "maxLogFileSizeMB": 10,
    "traceInstructions": false,
    "showCycleCount": true
  },
  "editor": {
    "fontFamily": "Cascadia Mono",
    "fontSize": 12,
    "tabSize": 4,
    "insertSpaces": true,
    "autoComplete": true,
    "syntaxHighlighting": true,
    "lineNumbers": true,
    "wordWrap": false,
    "assemblerDialect": "Merlin"
  },
  "windowLayout": {
    "version": 1,
    "mainWindow": {
      "position": { "x": 100, "y": 100 },
      "size": { "width": 1200, "height": 800 },
      "isMaximized": false
    },
    "windows": []
  },
  "profiles": [],
  "lastProfileId": null
}
```

### Settings Migration

When the settings schema evolves between versions, the migration system handles upgrades:

```csharp
/// <summary>
/// Handles settings schema migrations between versions.
/// </summary>
public interface ISettingsMigrator
{
    /// <summary>Gets the current settings schema version.</summary>
    int CurrentVersion { get; }

    /// <summary>Migrates settings from an older version.</summary>
    AppSettings Migrate(JsonElement oldSettings, int fromVersion);

    /// <summary>Checks if migration is needed.</summary>
    bool NeedsMigration(int settingsVersion);
}

/// <summary>
/// Individual migration step.
/// </summary>
public interface ISettingsMigrationStep
{
    /// <summary>Gets the source version this migration handles.</summary>
    int FromVersion { get; }

    /// <summary>Gets the target version after migration.</summary>
    int ToVersion { get; }

    /// <summary>Applies the migration.</summary>
    JsonElement Apply(JsonElement settings);
}
```

### File Formats

| Data | Format | Location |
|------|--------|----------|
| Settings | JSON | ~/.backpocket/settings.json |
| Profiles | JSON | ~/.backpocket/profiles/*.json |
| Window Layout | JSON | (embedded in settings.json) |
| Disk Library | Directory | {Library}/disks/ |
| ROM Library | Directory | {Library}/roms/ |
| Log Files | Text | {Library}/logs/ |
| Save States | Binary | {Library}/saves/ (future) |

### Settings API

The settings system exposes a public API for programmatic access:

```csharp
/// <summary>
/// Main settings service interface.
/// </summary>
public interface ISettingsService
{
    /// <summary>Gets the current settings.</summary>
    AppSettings Current { get; }

    /// <summary>Loads settings from disk.</summary>
    Task<AppSettings> LoadAsync();

    /// <summary>Saves settings to disk.</summary>
    Task SaveAsync(AppSettings settings);

    /// <summary>Resets all settings to defaults.</summary>
    Task<AppSettings> ResetToDefaultsAsync();

    /// <summary>Exports settings to a file.</summary>
    Task ExportAsync(string path);

    /// <summary>Imports settings from a file.</summary>
    Task<AppSettings> ImportAsync(string path);

    /// <summary>Gets a specific setting value.</summary>
    T GetValue<T>(string key);

    /// <summary>Sets a specific setting value.</summary>
    void SetValue<T>(string key, T value);

    /// <summary>Event raised when settings change.</summary>
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}

/// <summary>
/// Settings change event arguments.
/// </summary>
public record SettingsChangedEventArgs
{
    /// <summary>Gets the keys of settings that changed.</summary>
    public IReadOnlyList<string> ChangedKeys { get; init; } = [];

    /// <summary>Gets whether this was a full reload.</summary>
    public bool IsFullReload { get; init; }
}
```

### Headless Mode Support

For command-line and automation scenarios, settings can be overridden:

```csharp
/// <summary>
/// Command-line settings overrides for headless operation.
/// </summary>
public record HeadlessOptions
{
    /// <summary>Path to settings file (overrides default location).</summary>
    public string? SettingsPath { get; init; }

    /// <summary>Individual setting overrides (key=value format).</summary>
    public IReadOnlyDictionary<string, string> Overrides { get; init; }
        = new Dictionary<string, string>();

    /// <summary>Disable all GUI (pure headless).</summary>
    public bool NoGui { get; init; }

    /// <summary>Profile to auto-load.</summary>
    public string? ProfileId { get; init; }
}
```

---

## Testing Strategy

### Unit Tests

1. **ViewModel tests** - Command execution, property binding
2. **Service tests** - Machine lifecycle, storage operations
3. **Converter tests** - Value conversion correctness
4. **Settings tests** - Settings load/save, migration, validation
5. **Path validation tests** - Path normalization, validation rules

### Integration Tests

1. **Machine creation** - Profile → running instance
2. **Storage roundtrip** - Create → save → reload
3. **Debug session** - Attach → command → state inspection
4. **Settings migration** - Upgrade from older schema versions
5. **Window state persistence** - Save → close → restore layout

### UI Tests

1. **Avalonia Headless** - Automated UI testing
2. **Snapshot testing** - Visual regression detection
3. **Pop-out window tests** - Create, dock, restore window states
4. **Settings panel tests** - Navigate, modify, save/cancel changes

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
| Settings service & persistence | High | 3 days |
| Settings panel (General, Library) | Medium | 2 days |

### Phase 2: Storage & Debug (Target: 3 weeks)

| Task | Priority | Estimate |
|------|----------|----------|
| Storage Manager | High | 1 week |
| Disk image creation | High | 3 days |
| Debug Console integration | High | 1 week |
| Register/memory views | Medium | 3 days |
| Settings panel (Display, Input, Debug) | Medium | 2 days |

### Phase 3: Editor & Polish (Target: 3 weeks)

| Task | Priority | Estimate |
|------|----------|----------|
| Assembly editor | Medium | 1 week |
| Syntax highlighting | Medium | 3 days |
| Build integration | Medium | 3 days |
| Pop-out window support | Medium | 3 days |
| Window state persistence | Medium | 2 days |
| Settings panel (Editor, About) | Medium | 1 day |
| Theming | Low | 2 days |
| Documentation | Medium | 2 days |

### Phase 4: PocketGS/PocketME (Target: Future)

| Task | Priority | Estimate |
|------|----------|----------|
| Super Hi-Res display | Medium | 1 week |
| ADB input model | Medium | 3 days |
| 65832 mode support | Medium | 2 weeks |
| Hypervisor UI | Low | 2 weeks |
| Per-profile settings overrides | Low | 3 days |
| Settings import/export | Low | 2 days |

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

**Resolution:** Both are supported. Default to docked with option to pop out. See [Module 6: Pop-Out Window Architecture](#module-6-pop-out-window-architecture) for details.

### 2. How to handle multiple monitors?

**Resolution:** Window state persistence includes monitor identification. Pop-out windows can be placed on any monitor and their positions are saved per profile. See [Window State Persistence](#window-state-persistence).

### 3. Audio output approach?

**Recommendation:** Use NAudio on Windows, SDL on Linux/macOS. Abstract behind IAudioService.

### 4. Gamepad/joystick support?

**Recommendation:** Defer to Phase 2. Map analog stick to paddle values. Configuration available in Input settings.

### 5. Settings sync across devices?

**Recommendation:** Planned for future release. Settings import/export provides manual sync capability in the interim.

---

*Document last updated: 2025-12-27*
