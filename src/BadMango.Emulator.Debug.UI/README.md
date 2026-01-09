# BadMango.Emulator.Debug.UI

Avalonia-based popup window infrastructure for the console debugger REPL.

## Overview

This project provides the infrastructure for launching Avalonia UI popup windows from the debug console, enabling visual debugging tools like About windows, video displays, and text editors to coexist with the command-line REPL.

## Architecture

### Design Principles

1. **Separation of Concerns**: The debug console infrastructure (`BadMango.Emulator.Debug.Infrastructure`) depends only on the `IDebugWindowManager` abstraction, not on Avalonia or this project directly.

2. **No Circular Dependencies**: UI implementations depend on infrastructure abstractions, never the other way around.

3. **Non-Blocking REPL**: All window operations use async patterns to keep the REPL responsive while windows are being created or shown.

### Threading Model

The debugger operates in a multi-threaded environment:

```
┌─────────────────────┐     ┌─────────────────────┐
│   Console Thread    │     │   Avalonia UI       │
│   (REPL Input)      │     │   Thread            │
├─────────────────────┤     ├─────────────────────┤
│ - Read commands     │     │ - Window lifecycle  │
│ - Execute handlers  │────▶│ - User input        │
│ - Display output    │     │ - Rendering         │
└─────────────────────┘     └─────────────────────┘
         │                           │
         ▼                           ▼
┌─────────────────────────────────────────────────┐
│            IDebugWindowManager                  │
│   (Thread-safe window management)               │
└─────────────────────────────────────────────────┘
```

**Key Points:**

- All UI operations are dispatched to the Avalonia UI thread via `Dispatcher.UIThread.InvokeAsync()`
- Window state is tracked in thread-safe collections (`ConcurrentDictionary`)
- Commands fire-and-forget window operations to avoid blocking the REPL
- Windows can be closed independently without affecting the console

### Component Types

The `DebugWindowComponent` enum defines available window types:

| Component | Description | Status |
|-----------|-------------|--------|
| `About` | Version and copyright information | ✅ Implemented |
| `VideoDisplay` | Emulator graphics output | 🔲 Reserved |
| `TextEditor` | Source code editing | 🔲 Reserved |
| `MemoryViewer` | Memory inspection/editing | 🔲 Reserved |

## Usage

### Dependency Injection Setup

Register the module with Autofac when UI support is desired:

```csharp
// In your container configuration
builder.RegisterModule<DebugUiModule>();
```

### REPL Integration

Create a REPL with window support:

```csharp
// Get the window manager from DI
var windowManager = container.Resolve<IDebugWindowManager>();

// Create REPL with window support
var repl = DebugRepl.CreateConsoleRepl(windowManager);
repl.Run();
```

### Using the About Command

```
> about
Opening About window...
```

If Avalonia is not running, the command falls back to console output:

```
> about
═══════════════════════════════════════════════════════════════════════
  BackPocket BASIC - Emulator Debug Console
  Version: 1.0.0
  ...
═══════════════════════════════════════════════════════════════════════
```

## Extension Points

### Adding New Window Types

1. Add a new value to `DebugWindowComponent` enum
2. Create the window XAML and code-behind in `Views/`
3. Update `DebugWindowManager.CreateWindow()` to handle the new type
4. Update `DebugWindowManager.AvailableTypes` array
5. Optionally, create a new REPL command to open the window

### Example: Adding a Video Window

```csharp
// In DebugWindowComponent.cs
public enum DebugWindowComponent
{
    About,
    VideoDisplay,  // Already reserved
    // ...
}

// In DebugWindowManager.CreateWindow()
private Window? CreateWindow(string windowType)
{
    return windowType.ToUpperInvariant() switch
    {
        "ABOUT" => new AboutWindow(),
        "VIDEODISPLAY" => new VideoDisplayWindow(),  // Add this
        _ => null,
    };
}
```

## References

- [Pocket2e Debug Video Window Specification](../../specs/video/Pocket2e%20Debug%20Video%20Window%20(Avalonia)%20%E2%80%94%20Specification.md)
- [Emulator UI Specification - Module 6: Pop-Out Window Architecture](../../specs/Emulator%20UI%20Specification.md#module-6-pop-out-window-architecture)
- [Architecture Overview - Dependency Injection](../../wiki/Architecture-Overview.md#dependency-injection)

## Project Dependencies

- `BadMango.Emulator.Debug.Infrastructure` - For `IDebugWindowManager` interface
- `Avalonia` 11.2.7 - UI framework
- `Avalonia.Desktop` 11.2.7 - Desktop platform support
- `Avalonia.Themes.Fluent` 11.2.7 - Modern theme
- `Autofac` 8.3.0 - Dependency injection
