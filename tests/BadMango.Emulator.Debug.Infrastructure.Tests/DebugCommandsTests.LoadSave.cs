// <copyright file="DebugCommandsTests.LoadSave.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Unit tests for <see cref="LoadCommand"/> and <see cref="SaveCommand"/>.
/// </summary>
public partial class DebugCommandsTests
{
    /// <summary>
    /// Gets a platform-appropriate test library root path for LoadSave tests.
    /// </summary>
    private static string TestLibraryRootForLoadSave => OperatingSystem.IsWindows()
        ? @"C:\tmp\test-library"
        : "/tmp/test-library";

    /// <summary>
    /// Verifies that LoadCommand has correct name.
    /// </summary>
    [Test]
    public void LoadCommand_HasCorrectName()
    {
        var command = new LoadCommand();
        Assert.That(command.Name, Is.EqualTo("load"));
    }

    /// <summary>
    /// Verifies that LoadCommand returns error when file not found.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WhenFileNotFound()
    {
        var command = new LoadCommand();
        var result = command.Execute(debugContext, ["nonexistent.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
        });
    }

    /// <summary>
    /// Verifies that LoadCommand returns error when filename missing.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WhenFilenameMissing()
    {
        var command = new LoadCommand();
        var result = command.Execute(debugContext, []);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Filename required"));
        });
    }

    /// <summary>
    /// Verifies that LoadCommand returns error with resolved path when library:// file not found.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WithResolvedPath_WhenLibraryFileNotFound()
    {
        // Attach a path resolver with a known library root (platform-appropriate)
        debugContext.AttachPathResolver(new DebugPathResolver(TestLibraryRootForLoadSave));

        var command = new LoadCommand();
        var result = command.Execute(debugContext, ["library://nonexistent.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
            Assert.That(result.Message, Does.Contain("library://nonexistent.bin"));
            Assert.That(result.Message, Does.Contain("resolved to"));
            Assert.That(result.Message, Does.Contain(TestLibraryRootForLoadSave));
        });
    }

    /// <summary>
    /// Verifies that LoadCommand returns error when library root is not configured.
    /// </summary>
    [Test]
    public void LoadCommand_ReturnsError_WhenLibraryRootNotConfigured()
    {
        // Attach a path resolver without library root
        debugContext.AttachPathResolver(new DebugPathResolver(null));

        var command = new LoadCommand();
        var result = command.Execute(debugContext, ["library://test.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Cannot resolve path"));
        });
    }

    /// <summary>
    /// Verifies that SaveCommand has correct name.
    /// </summary>
    [Test]
    public void SaveCommand_HasCorrectName()
    {
        var command = new SaveCommand();
        Assert.That(command.Name, Is.EqualTo("save"));
    }

    /// <summary>
    /// Verifies that SaveCommand returns error when arguments missing.
    /// </summary>
    [Test]
    public void SaveCommand_ReturnsError_WhenArgumentsMissing()
    {
        var command = new SaveCommand();
        var result = command.Execute(debugContext, ["test.bin"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Filename, address, and length required"));
        });
    }

    /// <summary>
    /// Verifies that SaveCommand returns error when library root is not configured.
    /// </summary>
    [Test]
    public void SaveCommand_ReturnsError_WhenLibraryRootNotConfigured()
    {
        // Attach a path resolver without library root
        debugContext.AttachPathResolver(new DebugPathResolver(null));

        var command = new SaveCommand();
        var result = command.Execute(debugContext, ["library://output.bin", "$0", "$10"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Cannot resolve path"));
        });
    }

    /// <summary>
    /// Verifies that SaveCommand can save to library:// path when library root is configured.
    /// </summary>
    [Test]
    public void SaveCommand_SavesToLibraryPath_WhenLibraryRootConfigured()
    {
        // Create a temporary directory for the library root
        string tempLibrary = Path.Combine(Path.GetTempPath(), "test-library-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempLibrary);

        try
        {
            // Attach a path resolver with the temp library root
            debugContext.AttachPathResolver(new DebugPathResolver(tempLibrary));

            var command = new SaveCommand();
            var result = command.Execute(debugContext, ["library://output.bin", "$0", "$10"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(File.Exists(Path.Combine(tempLibrary, "output.bin")), Is.True);
            });
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempLibrary))
            {
                Directory.Delete(tempLibrary, recursive: true);
            }
        }
    }

    /// <summary>
    /// Verifies that LoadCommand can load from library:// path when file exists.
    /// </summary>
    [Test]
    public void LoadCommand_LoadsFromLibraryPath_WhenFileExists()
    {
        // Create a temporary directory for the library root
        string tempLibrary = Path.Combine(Path.GetTempPath(), "test-library-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempLibrary);
        string testFile = Path.Combine(tempLibrary, "test.bin");
        byte[] testData = [0x01, 0x02, 0x03, 0x04];
        File.WriteAllBytes(testFile, testData);

        try
        {
            // Attach a path resolver with the temp library root
            debugContext.AttachPathResolver(new DebugPathResolver(tempLibrary));

            var command = new LoadCommand();
            var result = command.Execute(debugContext, ["library://test.bin", "$100"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);

                // Verify data was loaded to memory
                Assert.That(ReadByte(bus, 0x100), Is.EqualTo(0x01));
                Assert.That(ReadByte(bus, 0x101), Is.EqualTo(0x02));
                Assert.That(ReadByte(bus, 0x102), Is.EqualTo(0x03));
                Assert.That(ReadByte(bus, 0x103), Is.EqualTo(0x04));
            });
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempLibrary))
            {
                Directory.Delete(tempLibrary, recursive: true);
            }
        }
    }
}