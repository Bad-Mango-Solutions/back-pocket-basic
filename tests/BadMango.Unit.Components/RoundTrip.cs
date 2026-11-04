// <copyright file="RoundTrip.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Unit.Components;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>Defines methods to round-trip serialize objects.</summary>
public static class RoundTrip
{
    /// <summary>Performs JSON serialization and deserialization of an object.</summary>
    /// <typeparam name="TData">The object type.</typeparam>
    /// <param name="input">The object being processed.</param>
    /// <param name="serializer">An optional serializer to use in round-trip conversion.</param>
    /// <returns>The result data.</returns>
    public static TData? Json<TData>(TData input, JsonSerializer? serializer = null)
    {
        serializer ??= JsonSerializer.CreateDefault();
        var json = JObject.FromObject(input!, serializer);
        TestContext.Out.WriteLine(json);
        return json.ToObject<TData>(serializer);
    }
}