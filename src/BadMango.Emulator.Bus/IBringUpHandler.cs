// <copyright file="IBringUpHandler.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Handles machine bring-up by receiving a provisioning bundle and producing a running machine.
/// </summary>
/// <remarks>
/// <para>
/// Machine types register a bring-up handler that knows the machine's rules. The handler
/// receives a provisioning bundle and decides:
/// <list type="bullet">
/// <item><description>How to allocate physical memory pools</description></item>
/// <item><description>Where regions should be mapped</description></item>
/// <item><description>What the initial page table looks like</description></item>
/// <item><description>How to handle machine-specific quirks (bank switching, soft switches, etc.)</description></item>
/// </list>
/// </para>
/// <para>
/// There is no hard-coded machine definition. The same handler works for different
/// configurations (e.g., 128KB or 8MB RAM) within machine limits.
/// </para>
/// </remarks>
public interface IBringUpHandler
{
    /// <summary>
    /// Gets the machine constants defining limits and defaults for this machine type.
    /// </summary>
    /// <value>The machine constants.</value>
    IMachineConstants Constants { get; }

    /// <summary>
    /// Validates that a provisioning bundle is compatible with this machine type.
    /// </summary>
    /// <param name="bundle">The provisioning bundle to validate.</param>
    /// <returns>
    /// A validation result indicating success or describing the validation errors.
    /// </returns>
    BringUpValidationResult Validate(IProvisioningBundle bundle);

    /// <summary>
    /// Performs machine bring-up using the provided provisioning bundle.
    /// </summary>
    /// <param name="bundle">The provisioning bundle describing the desired configuration.</param>
    /// <returns>The result of the bring-up process.</returns>
    /// <remarks>
    /// <para>
    /// This method should:
    /// <list type="number">
    /// <item><description>Validate the bundle (or assume pre-validated)</description></item>
    /// <item><description>Allocate physical memory pools</description></item>
    /// <item><description>Create memory regions and map them appropriately</description></item>
    /// <item><description>Load ROM images into physical memory</description></item>
    /// <item><description>Configure devices and their memory-mapped regions</description></item>
    /// <item><description>Build the initial page table</description></item>
    /// <item><description>Return the configured machine components</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    IBringUpResult BringUp(IProvisioningBundle bundle);
}