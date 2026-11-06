// <copyright file="DebugLogWindowSinkTests.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Tests;

using Serilog.Events;
using Serilog.Parsing;

/// <summary>
/// Unit tests for <see cref="DebugLogWindowSink"/>.
/// </summary>
[TestFixture]
public class DebugLogWindowSinkTests
{
    private static readonly MessageTemplateParser Parser = new();

    /// <summary>
    /// Verifies warning events are forwarded to the debug log stream.
    /// </summary>
    [Test]
    public void Emit_WarningEvent_ForwardsToStream()
    {
        var stream = new DebugLogEventStream();
        var sink = new DebugLogWindowSink(stream);
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Warning,
            null,
            Parser.Parse("Disk warning"),
            []);

        sink.Emit(logEvent);

        var read = stream.TryDequeue(out var entry);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.True);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Level, Is.EqualTo(DebugLogLevel.Warning));
            Assert.That(entry.Message, Is.EqualTo("Disk warning"));
        });
    }

    /// <summary>
    /// Verifies fatal events map to fatal UI level.
    /// </summary>
    [Test]
    public void Emit_FatalEvent_MapsToFatalLevel()
    {
        var stream = new DebugLogEventStream();
        var sink = new DebugLogWindowSink(stream);
        var logEvent = new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Fatal,
            null,
            Parser.Parse("Boom"),
            []);

        sink.Emit(logEvent);

        var read = stream.TryDequeue(out var entry);

        Assert.Multiple(() =>
        {
            Assert.That(read, Is.True);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.Level, Is.EqualTo(DebugLogLevel.Fatal));
        });
    }
}