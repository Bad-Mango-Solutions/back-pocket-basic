// <copyright file="NUnitContextSink.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Unit.Components;

using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

/// <summary>Defines a sink that outputs to the current NUnit test.</summary>
public class NUnitContextSink : ILogEventSink
{
    private readonly MessageTemplateTextFormatter formatter;

    /// <summary>Initializes a new instance of the <see cref="NUnitContextSink"/> class.</summary>
    /// <param name="formatter">The formatter for message output.</param>
    public NUnitContextSink(MessageTemplateTextFormatter formatter) => this.formatter = formatter;

    /// <inheritdoc cref="ILogEventSink.Emit(LogEvent)"/>
    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        formatter.Format(logEvent, writer);
        TestContext.Out.WriteLine(writer.ToString());
    }
}