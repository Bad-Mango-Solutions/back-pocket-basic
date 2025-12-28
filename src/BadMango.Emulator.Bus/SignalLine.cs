// <copyright file="SignalLine.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Identifies signal lines in the signal fabric.
/// </summary>
/// <remarks>
/// Signal lines represent the hardware interrupt and control signals
/// in the emulated system. Devices assert and deassert these lines,
/// and the CPU samples them at defined boundaries to determine when
/// to service interrupts or enter wait states.
/// </remarks>
public enum SignalLine : byte
{
    /// <summary>
    /// Interrupt Request signal.
    /// </summary>
    /// <remarks>
    /// IRQ is a maskable interrupt that can be disabled by setting the
    /// CPU's I (Interrupt Disable) flag. Multiple devices can assert IRQ
    /// simultaneously; the CPU must poll to determine the source.
    /// </remarks>
    IRQ,

    /// <summary>
    /// Non-Maskable Interrupt signal.
    /// </summary>
    /// <remarks>
    /// NMI is edge-triggered and cannot be masked by the I flag.
    /// It has higher priority than IRQ and is typically used for
    /// critical events like power failure notification.
    /// </remarks>
    NMI,

    /// <summary>
    /// Reset signal.
    /// </summary>
    /// <remarks>
    /// When asserted, forces the CPU to its reset state.
    /// The CPU will begin execution from the reset vector when the
    /// signal is released.
    /// </remarks>
    Reset,

    /// <summary>
    /// Ready signal.
    /// </summary>
    /// <remarks>
    /// When deasserted (low), indicates that the CPU should wait.
    /// Used by slow devices and for single-step debugging.
    /// The CPU halts instruction execution until RDY is asserted.
    /// </remarks>
    RDY,

    /// <summary>
    /// DMA Request signal.
    /// </summary>
    /// <remarks>
    /// When asserted, a DMA controller is requesting bus mastership.
    /// The CPU should release the bus at the end of the current
    /// cycle and wait for the DMA transfer to complete.
    /// </remarks>
    DmaReq,
}