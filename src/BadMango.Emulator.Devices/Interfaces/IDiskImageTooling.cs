// <copyright file="IDiskImageTooling.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Devices.Interfaces;

/// <summary>
/// Host-side tooling API for disk image creation and format conversion.
/// </summary>
/// <remarks>
/// <para>
/// This interface intentionally excludes filesystem inspection APIs, which are planned for a future issue.
/// </para>
/// <para>
/// Reference specifications:
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Disk%20Image%20Abstraction%20API%20Spec.md">Disk Image Abstraction API Spec</see>,
/// <see href="https://github.com/Bad-Mango-Solutions/back-pocket-basic/blob/main/specs/os/Unified%20Block%20Device%20Backing%20API%20for%20Apple%20II%20Emulator.md">Unified Block Device Backing API for Apple II Emulator</see>.
/// </para>
/// </remarks>
public interface IDiskImageTooling
{
    /// <summary>
    /// Creates a blank image using the supplied options.
    /// </summary>
    /// <param name="options">The image creation options.</param>
    /// <returns>The created media abstraction.</returns>
    IMedia CreateBlankImage(DiskImageCreationOptions options);

    /// <summary>
    /// Converts source media into a target format and writes the result to the output stream.
    /// </summary>
    /// <param name="source">The source media to convert.</param>
    /// <param name="targetFormat">The destination format identifier.</param>
    /// <param name="output">The output stream that receives converted image bytes.</param>
    void ConvertImage(IMedia source, string targetFormat, Stream output);
}