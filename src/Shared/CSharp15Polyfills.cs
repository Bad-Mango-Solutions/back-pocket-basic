// <copyright file="CSharp15Polyfills.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

#pragma warning disable SA1402 // File may only contain a single type; these compiler-required polyfills are intentionally shared in one included file.
#pragma warning disable SA1649 // File name should match first type name; the source is intentionally shared across all assemblies.
#pragma warning disable CS0436 // The compiler polyfill types are intentionally duplicated across assemblies to satisfy the C# 15 contract while keeping the runtime on net10.0.

namespace System.Runtime.CompilerServices;

/// <summary>
/// Internal polyfill for the compiler-required union marker interface.
/// </summary>
internal interface IUnion
{
    /// <summary>
    /// Gets the boxed payload associated with the union case.
    /// </summary>
    object? Value { get; }
}

/// <summary>
/// Internal polyfill for C# 15 union support when compiling for a net10.0 target with the .NET 11 SDK.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class UnionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnionAttribute"/> class.
    /// </summary>
    public UnionAttribute()
    {
    }
}

/// <summary>
/// Internal polyfill for C# 15 closed hierarchy exhaustiveness checking.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class IsClosedTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IsClosedTypeAttribute"/> class.
    /// </summary>
    public IsClosedTypeAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IsClosedTypeAttribute"/> class.
    /// </summary>
    /// <param name="derivedTypes">The known derived types.</param>
    public IsClosedTypeAttribute(params Type[]? derivedTypes)
    {
        this.DerivedTypes = derivedTypes;
    }

    /// <summary>
    /// Gets or sets the known derived types for the closed hierarchy.
    /// </summary>
    public Type[]? DerivedTypes { get; set; }
}

#pragma warning restore CS0436