# BadMango.Emulator.UI

Avalonia-based cross-platform user interface for the BackPocket emulator.

## Overview

This project provides a graphical frontend for the BackPocket emulator family, built with Avalonia UI 12 on .NET 10.0. The UI follows the MVVM pattern using CommunityToolkit.Mvvm and uses Autofac for dependency injection with Microsoft.Extensions.Hosting patterns. The assembly name is `bpui`. It references `BadMango.Emulator.Core`, `BadMango.Emulator.Infrastructure`, and `BadMango.Emulator.UI.Abstractions`.

## Features

- **Machine Management** - Create, configure, start, and stop emulator instances
- **Theme Support** - Dark and light themes with retro-modern design
- **Navigation** - Sidebar navigation with placeholder views for future features
- **Extensible Architecture** - Service-based design for easy extension

## Architecture

### Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| UI Framework | Avalonia 12 | Cross-platform XAML UI |
| MVVM | CommunityToolkit.Mvvm | ViewModels, commands, observables |
| DI Container | Autofac | Service resolution |
| Hosting | Microsoft.Extensions.Hosting | Application lifecycle |
| Configuration | Microsoft.Extensions.Configuration | App settings |
| Logging | Serilog | Structured logging |

### Project Structure

```
BadMango.Emulator.UI/
├── App.axaml                     # Application definition
├── App.axaml.cs                  # Application code-behind
├── Program.cs                    # Entry point with hosting setup
├── appsettings.json              # Application configuration
├── Models/                       # Window layout and pop-out models
├── Services/                     # Navigation, theme, window manager, shutdown
├── ViewModels/                   # Main window, machine manager, pop-out
│   └── Settings/                 # Settings page view models
├── Views/                        # Main window, machine manager, pop-out, placeholder
│   └── Settings/                 # Settings and About views
└── Resources/
    └── Styles/                   # ColorPalette.axaml, AppStyles.axaml
```

`IThemeService` lives in `BadMango.Emulator.UI.Abstractions`. `ThemeService` is implemented in `Services/`.

## Running the Application

```bash
# From repository root
dotnet run --project src/BadMango.Emulator.UI/BadMango.Emulator.UI.csproj
```

## Configuration

Application settings are stored in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    }
  },
  "AppSettings": {
    "Theme": "Dark",
    "LibraryPath": "./Library"
  }
}
```

Environment variables can override settings using the `BACKPOCKET_` prefix.

## Status

Phase 1 (Foundation) - Complete:
- [x] Project setup with Avalonia 11.x
- [x] Main window shell with navigation
- [x] Machine Manager stub screen
- [x] Dark/light theme support
- [x] Autofac DI with Microsoft.Extensions.Hosting
- [x] Serilog logging
- [x] Unit tests for ViewModels (19 tests)
