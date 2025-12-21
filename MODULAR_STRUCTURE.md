# Modular Solution Layout

This document describes the modular project structure created for the 65832/Back Pocket BASIC Evolution.

## Project Structure

```
/src/
├── BadMango.Emulator.Core/              # Core interfaces
│   ├── ICpu.cs                          # CPU emulator interface
│   ├── IMemory.cs                       # Memory management interface
│   ├── IBus.cs                          # System bus interface
│   └── IPeripheral.cs                   # Peripheral device interface
│
├── BadMango.Emulator.Emulation/         # CPU & memory emulation
│   ├── Cpu/                             # CPU implementations
│   │   ├── Cpu65C02.cs                  # WDC 65C02 (placeholder)
│   │   ├── Cpu65816.cs                  # WDC 65816/Apple IIgs (placeholder)
│   │   └── Cpu65832.cs                  # Hypothetical 65832 (placeholder)
│   └── MemoryModels/                    # Memory model implementations
│
├── BadMango.Emulator.Devices/           # Device implementations
│   ├── Video/                           # Video subsystem
│   ├── Keyboard/                        # Keyboard input
│   ├── Timer/                           # Timer devices
│   └── MMU/                             # Memory Management Unit
│
├── BadMango.Emulator.Systems/           # Complete system configurations
│   ├── AppleII/                         # Apple II system
│   ├── IIgs/                            # Apple IIgs system (planned)
│   └── Cpu65832/                        # 65832 system (planned)
│
├── BadMango.Emulator.UI/                # User interface
│   └── AvaloniaApp/                     # Avalonia-based UI (planned)
│
└── BadMango.Emulator.Interop/           # Native/Rust interop
    └── RustBridge/                      # Rust integration (planned)
```

## Design Principles

### Naming Conventions

- **Namespace Pattern**: `BadMango.Emulator.*` (not `BadMango.Basic.*`)
- **CPU Naming**: Use `Cpu65C02`, `Cpu65816`, `Cpu65832` (no dots or special characters)
  - ✓ Correct: `Cpu65C02`, `Cpu65816`
  - ✗ Incorrect: `Cpu.65C02`, `Cpu-65816`

### Modularity

Each project has a specific responsibility:

1. **Core** - Defines contracts and extension points
2. **Emulation** - Implements CPU and memory behavior
3. **Devices** - Implements peripheral hardware
4. **Systems** - Integrates components into complete systems
5. **UI** - Provides user-facing interfaces
6. **Interop** - Enables performance-critical native code

### Extension Points

The `IPeripheral` interface allows pluggable devices:
```csharp
public interface IPeripheral
{
    string Name { get; }
    void Reset();
    byte? ReadIO(int address);
    bool WriteIO(int address, byte value);
}
```

This enables future additions like:
- Disk controllers
- Network cards
- Printer interfaces
- Expansion slot cards

## Current Status

- ✅ All projects created with proper structure
- ✅ Core interfaces defined
- ✅ CPU placeholder classes created
- ✅ Subdirectories organized
- ✅ Solution file updated
- ✅ All projects build with 0 warnings
- ✅ All existing tests pass (429/429)

## Next Steps

Future implementations will populate these projects with:
1. Complete CPU emulators (65C02, 65816, 65832)
2. Memory models (64KB, banked, extended)
3. Device implementations (video, keyboard, etc.)
4. System integrations (Apple II, IIgs configurations)
5. Avalonia-based graphical UI
6. Rust interop for performance-critical code

## Benefits

This modular structure provides:
- **Scalability** - Easy to add new CPU architectures
- **Maintainability** - Clear separation of concerns
- **Testability** - Each component can be tested independently
- **Extensibility** - Well-defined extension points for devices
- **Flexibility** - Support multiple system configurations
