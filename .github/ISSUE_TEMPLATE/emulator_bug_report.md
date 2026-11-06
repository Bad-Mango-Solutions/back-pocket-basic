---
name: Emulator Bug Report
about: Report a bug or unexpected behavior in the 65C02 emulator
title: '[EMULATOR BUG] '
labels: bug, emulator
assignees: ''
---

## Describe the Bug
A clear and concise description of what the bug is.

## To Reproduce
Steps to reproduce the behavior:
1. Set up emulator with '...'
2. Execute instruction/opcode '...'
3. Check register/memory state '...'
4. Observe error '...'

## Expected Behavior
A clear and concise description of what you expected to happen (including expected register values, memory state, flags, etc.).

## Actual Behavior
What actually happened (including actual register values, memory state, flags, etc.).

## Assembly/Machine Code Sample
If applicable, provide the assembly code or machine code that triggers the bug:

```assembly
; Example 65C02 assembly
LDA #$42
STA $0200
```

Or provide hexadecimal machine code:
```
A9 42 8D 00 02
```

## Emulator Component
Which component(s) of the emulator are affected?
- [ ] CPU opcodes/instruction execution
- [ ] CPU flags/status register
- [ ] Memory addressing modes
- [ ] Memory subsystem
- [ ] Devices/peripherals
- [ ] System timing/cycles
- [ ] Other (please specify)

## Environment
- OS: [e.g., Windows 11, Ubuntu 22.04, macOS 14]
- .NET Version: [e.g., .NET 10.0]
- BackPocket Emulator Version/Commit: [e.g., main branch, commit abc123]
- CPU Type: [e.g., 65C02, 65816]

## Error Messages
If applicable, include any error messages or stack traces:

```
Paste error messages here
```

## Register/Memory State
If applicable, provide relevant register and memory state information:

```
A: $00  X: $00  Y: $00  SP: $FF
PC: $0000  Status: NV-BDIZC
                    00110000
Memory at $0200: 00 00 00 00
```

## Hardware Reference
If you have documentation showing correct hardware behavior, please link or describe it here.

## Screenshots
If applicable, add screenshots to help explain your problem.

## Additional Context
Add any other context about the problem here.
