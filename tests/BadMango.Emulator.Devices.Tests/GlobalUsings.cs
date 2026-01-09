// <copyright file="GlobalUsings.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

#pragma warning disable SA1200 // Using directives should be placed correctly

global using BadMango.Emulator.Bus;
global using BadMango.Emulator.Devices;
global using BadMango.Emulator.Devices.Interfaces;
global using BadMango.Emulator.Devices.Speaker;

global using NUnit.Framework;

// Global type aliases matching the Bus project
global using Addr = uint;  // Address type - 32-bit for future flat addressing
global using DWord = uint; // Double word - 32-bit unsigned integer
global using Word = ushort;  // Word - 16-bit unsigned integer