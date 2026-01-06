// <copyright file="PocketWatchConfig.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Core.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Configuration for the PocketWatch real-time clock card.
/// </summary>
/// <remarks>
/// <para>
/// PocketWatch is a Thunderclock-compatible real-time clock card that provides
/// time services to the emulated system. It supports multiple time sources:
/// <list type="bullet">
/// <item><description>"host" - Uses the host system's clock (UTC with timezone offset)</description></item>
/// <item><description>"frozen" - Uses a static datetime specified in the profile</description></item>
/// <item><description>"ntp" - Uses NTP server polling with offset from host clock</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class PocketWatchConfig
{
    /// <summary>
    /// The default NTP poll interval in seconds (1 hour).
    /// </summary>
    public const int DefaultNtpPollIntervalSeconds = 3600;
    /// <summary>
    /// Gets or sets the time source for the clock.
    /// </summary>
    /// <remarks>
    /// Valid values: "host", "frozen", "ntp". Defaults to "host".
    /// </remarks>
    [JsonPropertyName("timeSource")]
    public string TimeSource { get; set; } = "host";

    /// <summary>
    /// Gets or sets the timezone offset in minutes from UTC.
    /// </summary>
    /// <remarks>
    /// Used with "host" and "ntp" time sources. For example, -300 for EST (UTC-5),
    /// +60 for CET (UTC+1). Defaults to 0 (UTC).
    /// </remarks>
    [JsonPropertyName("timezoneOffset")]
    public int TimezoneOffset { get; set; } = 0;

    /// <summary>
    /// Gets or sets the frozen time value for "frozen" time source.
    /// </summary>
    /// <remarks>
    /// An ISO 8601 datetime string (e.g., "1986-01-01T00:00:00").
    /// Only used when <see cref="TimeSource"/> is "frozen".
    /// </remarks>
    [JsonPropertyName("frozenTime")]
    public string? FrozenTime { get; set; }

    /// <summary>
    /// Gets or sets the NTP server address for "ntp" time source.
    /// </summary>
    /// <remarks>
    /// The hostname or IP address of an NTP server.
    /// Only used when <see cref="TimeSource"/> is "ntp".
    /// </remarks>
    [JsonPropertyName("ntpServer")]
    public string? NtpServer { get; set; }

    /// <summary>
    /// Gets or sets the NTP poll interval in seconds.
    /// </summary>
    /// <remarks>
    /// How often to query the NTP server for time updates.
    /// Defaults to 3600 seconds (1 hour).
    /// Only used when <see cref="TimeSource"/> is "ntp".
    /// </remarks>
    [JsonPropertyName("ntpPollInterval")]
    public int NtpPollInterval { get; set; } = DefaultNtpPollIntervalSeconds;
}