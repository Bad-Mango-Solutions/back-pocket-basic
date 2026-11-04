# BadMango.Emulator.Devices

Peripheral implementations for the Pocket2e / Apple IIe-class machine.

This project references `BadMango.Emulator.Bus` and `BadMango.Emulator.Storage`. Peripheral contracts (`IPeripheral`) live in `BadMango.Emulator.Bus`.

Sources are mostly flat in this directory, with `Speaker/`, `Interfaces/`, and `Resources/` beside them. The implementations include:

- Video, character ROM, and the extended 80-column card
- Keyboard and game I/O
- Speaker
- Language card
- Disk II controller
- Thunderclock and PocketWatch cards
