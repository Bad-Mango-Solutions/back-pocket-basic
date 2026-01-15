// <copyright file="IExtended80ColumnDevice.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Interface for the Extended 80-Column Card device providing auxiliary RAM access
/// for 80-column text rendering and double hi-res graphics.
/// </summary>
/// <remarks>
/// <para>
/// The Extended 80-Column Card provides 64KB of auxiliary RAM that is used for:
/// </para>
/// <list type="bullet">
/// <item><description>80-column text display (even columns from aux, odd from main)</description></item>
/// <item><description>Double hi-res graphics (interleaved aux/main bytes)</description></item>
/// <item><description>Alternate zero page and stack</description></item>
/// </list>
/// <para>
/// This interface exposes the auxiliary memory reading functionality needed by
/// the video renderer to display 80-column text and double hi-res modes.
/// </para>
/// </remarks>
public interface IExtended80ColumnDevice : IMotherboardDevice
{
    /// <summary>
    /// Gets a value indicating whether 80STORE mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if 80STORE mode is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When 80STORE is enabled, the PAGE2 soft switch controls whether display memory
    /// accesses go to auxiliary or main memory.
    /// </remarks>
    bool Is80StoreEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether 80-column display mode is enabled.
    /// </summary>
    /// <value><see langword="true"/> if 80-column mode is enabled; otherwise, <see langword="false"/>.</value>
    /// <remarks>
    /// When 80-column mode is enabled, the text display alternates characters between
    /// main memory (odd columns) and auxiliary memory (even columns).
    /// </remarks>
    bool Is80ColumnEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether PAGE2 is selected for 80STORE memory switching.
    /// </summary>
    /// <value><see langword="true"/> if PAGE2 is selected; otherwise, <see langword="false"/>.</value>
    bool IsPage2Selected { get; }

    /// <summary>
    /// Reads a byte from auxiliary RAM at the specified address.
    /// </summary>
    /// <param name="address">The address within auxiliary RAM (0x0000-0xFFFF).</param>
    /// <returns>The byte value at the specified address.</returns>
    /// <remarks>
    /// This method provides direct read access to auxiliary RAM for video rendering.
    /// It does not go through the memory bus and does not trigger soft switches.
    /// </remarks>
    byte ReadAuxRam(ushort address);

    /// <summary>
    /// Gets the auxiliary RAM as a span for direct memory access.
    /// </summary>
    /// <value>A span covering the 64KB auxiliary RAM.</value>
    /// <remarks>
    /// This property provides direct access to auxiliary RAM for efficient
    /// bulk rendering operations. The span covers the full 64KB auxiliary RAM space.
    /// </remarks>
    Span<byte> AuxiliaryRam { get; }

    /// <summary>
    /// Called when PAGE2 state changes (from VideoDevice).
    /// </summary>
    /// <param name="selected">Whether PAGE2 is selected.</param>
    /// <remarks>
    /// <para>
    /// This method is called by <see cref="IVideoDevice"/> when the PAGE2 soft switch
    /// ($C054/$C055) is toggled. The Extended 80-Column device uses this to update
    /// memory banking when 80STORE mode is enabled.
    /// </para>
    /// <para>
    /// When 80STORE is enabled and PAGE2 changes, the auxiliary text layer
    /// ($0400-$07FF) is activated or deactivated accordingly.
    /// </para>
    /// </remarks>
    void SetPage2(bool selected);

    /// <summary>
    /// Called when HIRES state changes (from VideoDevice).
    /// </summary>
    /// <param name="enabled">Whether HIRES mode is enabled.</param>
    /// <remarks>
    /// <para>
    /// This method is called by <see cref="IVideoDevice"/> when the HIRES soft switch
    /// ($C056/$C057) is toggled. The Extended 80-Column device uses this to update
    /// memory banking when 80STORE mode is enabled.
    /// </para>
    /// <para>
    /// When 80STORE is enabled, PAGE2 is set, and HIRES changes, the auxiliary hi-res
    /// layer ($2000-$3FFF) is activated or deactivated accordingly.
    /// </para>
    /// </remarks>
    void SetHiRes(bool enabled);
}
