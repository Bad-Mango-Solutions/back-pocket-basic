// <copyright file="DiskImageCreationOptions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Storage;

/// <summary>
/// Host-side options used to create a new blank disk image.
/// </summary>
/// <param name="Format">The target image format identifier (for example, dsk, po, nib, or 2mg).</param>
/// <param name="BlockCount">The requested block count for the created image.</param>
/// <param name="BlockSize">The requested block size in bytes.</param>
/// <param name="ReadOnly">A value indicating whether the created image should be marked read-only.</param>
public readonly record struct DiskImageCreationOptions(string Format, int BlockCount, int BlockSize, bool ReadOnly);