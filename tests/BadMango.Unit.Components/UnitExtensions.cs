// <copyright file="UnitExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Unit.Components;

using System;

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

/// <summary>Defines extensions for helper functionality.</summary>
public static class UnitExtensions
{
    /// <param name="sinkConfiguration">Logger sink configuration.</param>
    extension(LoggerSinkConfiguration sinkConfiguration)
    {
        /// <summary>Configures the current logger to output to the NUnit test output.</summary>
        /// <param name="restrictedToMinimumLevel">The minimum level for
        /// events passed through the sink. Ignored when <paramref name="levelSwitch" /> is specified.</param>
        /// <param name="levelSwitch">A switch allowing the pass-through minimum level
        /// to be changed at runtime.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public LoggerConfiguration NUnit(
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            LoggingLevelSwitch? levelSwitch = null,
            string outputTemplate = "{Timestamp:HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception",
            IFormatProvider? formatProvider = null)
        {
            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
            return sinkConfiguration.Sink(new NUnitContextSink(formatter), restrictedToMinimumLevel, levelSwitch);
        }
    }
}