# BadMango.Emulator.Debug.UI

Avalonia-based popup window infrastructure for the console debugger REPL.

## Overview

This project provides the infrastructure for launching Avalonia UI popup windows from the debug console, enabling visual debugging tools like About windows, video displays, and text editors to coexist with the command-line REPL.

## Architecture

### Design Principles

1. **Separation of Concerns**: The debug console infrastructure (`BadMango.Emulator.Debug.Infrastructure`) depends only on the `IDebugWindowManager` abstraction, not on Avalonia or this project directly.

2. **No Circular Dependencies**: UI implementations depend on infrastructure abstractions, never the other way around.

3. **Non-Blocking REPL**: All window operations use async patterns to keep the REPL responsive while windows are being created or shown.

4. **On-Demand Avalonia**: Avalonia is only initialized when a window is first requested, allowing the console to start quickly.

### Threading Model

The debugger operates in a multi-threaded environment:

```
┌─────────────────────┐     ┌─────────────────────┐
│   Main Thread       │     │   Avalonia UI       │
│   (Console REPL)    │     │   Thread            │
├─────────────────────┤     ├─────────────────────┤
│ - Read commands     │     │ - Window lifecycle  │
│ - Execute handlers  │────▶│ - User input        │
│ - Display output    │     │ - Rendering         │
└─────────────────────┘     └─────────────────────┘
         │                           │
         ▼                           ▼
┌─────────────────────────────────────────────────┐
│        AvaloniaBootstrapper                     │
│   (Manages Avalonia lifecycle on bg thread)    │
├─────────────────────────────────────────────────┤
│            IDebugWindowManager                  │
│   (Thread-safe window management)               │
└─────────────────────────────────────────────────┘
```

**Key Points:**

- **Background Avalonia Thread**: The `AvaloniaBootstrapper` starts Avalonia on a dedicated background thread when the first window is requested
- **Main Thread for REPL**: The console REPL continues running on the main thread, accepting commands while windows are displayed
- **Thread-Safe Dispatch**: All UI operations are dispatched to the Avalonia UI thread via `Dispatcher.UIThread.InvokeAsync()`
- **Concurrent Window Tracking**: Window state is tracked in thread-safe collections (`ConcurrentDictionary`)
- **Non-Blocking Commands**: Commands fire-and-forget window operations to avoid blocking the REPL
- **Independent Lifecycle**: Windows can be closed independently without affecting the console

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

The `BadMango.Emulator.Debug` console application registers both modules:

```csharp
// In Program.cs
builder.RegisterModule<DebugConsoleModule>();
builder.RegisterModule<DebugUiModule>();
```

### Using the About Command

When running the debug console, use the `about` command to open the About window:

```
> about
Opening About window...
```

The About window will appear in a new popup window while the console remains active for further commands.

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

## Key Components

### AvaloniaBootstrapper

Manages the Avalonia UI thread lifecycle:

- **Thread-Safe Initialization**: Uses double-checked locking to ensure single initialization
- **Background Thread**: Runs Avalonia message loop on a dedicated thread
- **Explicit Shutdown Mode**: Uses `ShutdownMode.OnExplicitShutdown` so Avalonia doesn't exit when windows close
- **Synchronization**: Uses `ManualResetEventSlim` to block until Avalonia is fully initialized

### DebugWindowManager

Implements `IDebugWindowManager` to manage window lifecycle:

- **Auto-Initialization**: Calls `AvaloniaBootstrapper.EnsureInitialized()` when a window is requested
- **Window Tracking**: Maintains a concurrent dictionary of open windows
- **Bring to Front**: Reactivates existing windows instead of creating duplicates

### DebugApp

Minimal Avalonia Application:

- **No Main Window**: Unlike the full UI app, doesn't create a main window on startup
- **On-Demand Windows**: Windows are created by `DebugWindowManager` when requested

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
