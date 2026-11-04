// <copyright file="DebugCommandsTests.BreakWatchPersistence.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Infrastructure.Tests;

/// <summary>
/// Tests for <see cref="BreakCommand"/> and <see cref="WatchCommand"/> save/load path resolution.
/// </summary>
public partial class DebugCommandsTests
{
    // ─── BreakCommand path resolution ────────────────────────────────────────

    /// <summary>
    /// Verifies that bp save resolves library:// paths via the path resolver.
    /// </summary>
    [Test]
    public void BreakCommand_Save_ResolvesLibraryPath_WhenResolverConfigured()
    {
        string tempLibrary = Path.Combine(Path.GetTempPath(), "bp-save-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempLibrary);
        try
        {
            debugContext.AttachPathResolver(new DebugPathResolver(tempLibrary));
            WriteByte(bus, 0x1000, 0xEA);
            cpu.Reset();

            // Add a breakpoint so there is something to save.
            new BreakCommand().Execute(debugContext, ["add", "$1000"]);

            var result = new BreakCommand().Execute(debugContext, ["save", "library://bp-test.json"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(File.Exists(Path.Combine(tempLibrary, "bp-test.json")), Is.True);
            });
        }
        finally
        {
            Directory.Delete(tempLibrary, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that bp save returns an error when library root is not configured.
    /// </summary>
    [Test]
    public void BreakCommand_Save_ReturnsError_WhenLibraryRootNotConfigured()
    {
        debugContext.AttachPathResolver(new DebugPathResolver(null));
        var result = new BreakCommand().Execute(debugContext, ["save", "library://bp.json"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Cannot resolve path"));
        });
    }

    /// <summary>
    /// Verifies that bp load resolves library:// paths via the path resolver.
    /// </summary>
    [Test]
    public void BreakCommand_Load_ResolvesLibraryPath_WhenResolverConfigured()
    {
        string tempLibrary = Path.Combine(Path.GetTempPath(), "bp-load-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempLibrary);
        try
        {
            debugContext.AttachPathResolver(new DebugPathResolver(tempLibrary));
            WriteByte(bus, 0x1000, 0xEA);
            cpu.Reset();

            // Save first so we have a valid file to load back.
            new BreakCommand().Execute(debugContext, ["add", "$1000"]);
            new BreakCommand().Execute(debugContext, ["save", "library://bp-load.json"]);
            new BreakCommand().Execute(debugContext, ["clear"]);

            var result = new BreakCommand().Execute(debugContext, ["load", "library://bp-load.json"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(outputWriter.ToString(), Does.Contain("library://bp-load.json"));
            });
        }
        finally
        {
            Directory.Delete(tempLibrary, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that bp load returns an error when the resolved file does not exist.
    /// </summary>
    [Test]
    public void BreakCommand_Load_ReturnsError_WithResolvedPath_WhenFileNotFound()
    {
        debugContext.AttachPathResolver(new DebugPathResolver(TestLibraryRootForLoadSave));
        var result = new BreakCommand().Execute(debugContext, ["load", "library://nonexistent-bp.json"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
            Assert.That(result.Message, Does.Contain("library://nonexistent-bp.json"));
            Assert.That(result.Message, Does.Contain("resolved to"));
        });
    }

    /// <summary>
    /// Verifies that bp load returns an error when library root is not configured.
    /// </summary>
    [Test]
    public void BreakCommand_Load_ReturnsError_WhenLibraryRootNotConfigured()
    {
        debugContext.AttachPathResolver(new DebugPathResolver(null));
        var result = new BreakCommand().Execute(debugContext, ["load", "library://bp.json"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Cannot resolve path"));
        });
    }

    // ─── WatchCommand path resolution ────────────────────────────────────────

    /// <summary>
    /// Verifies that watch save resolves library:// paths via the path resolver.
    /// </summary>
    [Test]
    public void WatchCommand_Save_ResolvesLibraryPath_WhenResolverConfigured()
    {
        string tempLibrary = Path.Combine(Path.GetTempPath(), "wp-save-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempLibrary);
        try
        {
            debugContext.AttachPathResolver(new DebugPathResolver(tempLibrary));
            WriteByte(bus, 0x1000, 0xEA);
            cpu.Reset();

            // Add a watchpoint so there is something to save.
            new WatchCommand().Execute(debugContext, ["add", "$3D0", "rw"]);

            var result = new WatchCommand().Execute(debugContext, ["save", "library://wp-test.json"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(File.Exists(Path.Combine(tempLibrary, "wp-test.json")), Is.True);
            });
        }
        finally
        {
            Directory.Delete(tempLibrary, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that watch save returns an error when library root is not configured.
    /// </summary>
    [Test]
    public void WatchCommand_Save_ReturnsError_WhenLibraryRootNotConfigured()
    {
        debugContext.AttachPathResolver(new DebugPathResolver(null));
        var result = new WatchCommand().Execute(debugContext, ["save", "library://wp.json"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Cannot resolve path"));
        });
    }

    /// <summary>
    /// Verifies that watch load resolves library:// paths via the path resolver.
    /// </summary>
    [Test]
    public void WatchCommand_Load_ResolvesLibraryPath_WhenResolverConfigured()
    {
        string tempLibrary = Path.Combine(Path.GetTempPath(), "wp-load-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempLibrary);
        try
        {
            debugContext.AttachPathResolver(new DebugPathResolver(tempLibrary));
            WriteByte(bus, 0x1000, 0xEA);
            cpu.Reset();

            // Save first so we have a valid file to load.
            new WatchCommand().Execute(debugContext, ["add", "$3D0", "rw"]);
            new WatchCommand().Execute(debugContext, ["save", "library://wp-load.json"]);
            new WatchCommand().Execute(debugContext, ["clear"]);

            var result = new WatchCommand().Execute(debugContext, ["load", "library://wp-load.json"]);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.True);
                Assert.That(outputWriter.ToString(), Does.Contain("library://wp-load.json"));
            });
        }
        finally
        {
            Directory.Delete(tempLibrary, recursive: true);
        }
    }

    /// <summary>
    /// Verifies that watch load returns an error when the resolved file does not exist.
    /// </summary>
    [Test]
    public void WatchCommand_Load_ReturnsError_WithResolvedPath_WhenFileNotFound()
    {
        debugContext.AttachPathResolver(new DebugPathResolver(TestLibraryRootForLoadSave));
        var result = new WatchCommand().Execute(debugContext, ["load", "library://nonexistent-wp.json"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("File not found"));
            Assert.That(result.Message, Does.Contain("library://nonexistent-wp.json"));
            Assert.That(result.Message, Does.Contain("resolved to"));
        });
    }

    /// <summary>
    /// Verifies that watch load returns an error when library root is not configured.
    /// </summary>
    [Test]
    public void WatchCommand_Load_ReturnsError_WhenLibraryRootNotConfigured()
    {
        debugContext.AttachPathResolver(new DebugPathResolver(null));
        var result = new WatchCommand().Execute(debugContext, ["load", "library://wp.json"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Does.Contain("Cannot resolve path"));
        });
    }
}