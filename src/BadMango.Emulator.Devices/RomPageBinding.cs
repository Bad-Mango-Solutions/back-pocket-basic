// <copyright file="RomPageBinding.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices;

using BadMango.Emulator.Bus;
using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Describes a single page-aligned ROM segment used by <see cref="LanguageCardSplitTarget"/>
/// to route reads in split mode. Each binding covers exactly one 4 KB page.
/// </summary>
/// <param name="VirtualPageBase">
/// The virtual start address of this 4 KB page (for example, <c>0xE000</c> or <c>0xF000</c>).
/// </param>
/// <param name="Target">The ROM bus target that backs this page.</param>
/// <param name="PhysicalBase">
/// The physical base address within <paramref name="Target"/> that corresponds to
/// <paramref name="VirtualPageBase"/>. For example, if <paramref name="Target"/> is a
/// 16 KB system ROM mapped at $C000 and this binding covers $E000, pass <c>0x2000</c>.
/// </param>
internal sealed record RomPageBinding(Addr VirtualPageBase, IBusTarget Target, Addr PhysicalBase);