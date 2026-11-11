// <copyright file="DebugEnvelope.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Debug.Abstractions;

/// <summary>
/// An envelope used to wrap debug payloads in a versioned JSON schema.
/// </summary>
/// <typeparam name="T">The payload type.</typeparam>
/// <param name="Schema">The schema identifier.</param>
/// <param name="Payload">The wrapped payload.</param>
/// <param name="Timestamp">The time the envelope was generated.</param>
/// <param name="CorrelationId">An optional correlation identifier.</param>
public sealed record DebugEnvelope<T>(
    string Schema,
    T Payload,
    DateTimeOffset Timestamp,
    string? CorrelationId = null)
{
    /// <summary>
    /// The versioned envelope schema for emudbg payloads.
    /// </summary>
    public const string SchemaValue = "emudbg/v1";

    /// <summary>
    /// Initializes a new instance of the <see cref="DebugEnvelope{T}"/> record.
    /// </summary>
    /// <param name="payload">The wrapped payload.</param>
    /// <param name="timestamp">The timestamp for the payload.</param>
    /// <param name="correlationId">An optional correlation identifier.</param>
    public DebugEnvelope(T payload, DateTimeOffset? timestamp = null, string? correlationId = null)
        : this(SchemaValue, payload, timestamp ?? DateTimeOffset.UtcNow, correlationId)
    {
    }
}