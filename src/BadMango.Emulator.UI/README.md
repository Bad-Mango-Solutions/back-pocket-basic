# BadMango.Emulator.UI

Avalonia-based cross-platform user interface for the BackPocket emulator.

## Overview

This project provides a graphical frontend for the BackPocket emulator family, built with Avalonia UI 11.x on .NET 10.0. The UI follows the MVVM pattern using CommunityToolkit.Mvvm and uses Autofac for dependency injection with Microsoft.Extensions.Hosting patterns.

## Features

- **Machine Management** - Create, configure, start, and stop emulator instances
- **Theme Support** - Dark and light themes with retro-modern design
- **Navigation** - Sidebar navigation with placeholder views for future features
- **Extensible Architecture** - Service-based design for easy extension

## Architecture

### Technology Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| UI Framework | Avalonia 11.x | Cross-platform XAML UI |
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
│
├── Converters/                   # Value converters
│
├── Models/                       # UI data models
│
├── Services/                     # UI services
│   ├── IThemeService.cs          # Theme management interface
│   ├── ThemeService.cs           # Theme implementation
│   ├── INavigationService.cs     # Navigation interface
│   └── NavigationService.cs      # Navigation implementation
│
├── ViewModels/                   # MVVM ViewModels
│   ├── ViewModelBase.cs          # Base ViewModel class
│   ├── MainWindowViewModel.cs    # Main window logic
│   ├── MachineManagerViewModel.cs # Machine management
│   ├── MachineProfileViewModel.cs # Profile data
│   ├── MachineInstanceViewModel.cs # Instance data
│   ├── NavigationItemViewModel.cs # Navigation items
│   └── PlaceholderViewModel.cs   # Placeholder for future views
│
├── Views/                        # AXAML Views
│   ├── MainWindow.axaml          # Main application window
│   ├── MachineManagerView.axaml  # Machine management view
│   └── PlaceholderView.axaml     # Placeholder view
│
└── Resources/                    # Assets
    └── Styles/                   # AXAML styles
        ├── ColorPalette.axaml    # Theme colors per spec
        └── AppStyles.axaml       # Common styles
```

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
