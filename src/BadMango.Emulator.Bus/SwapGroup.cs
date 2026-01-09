// <copyright file="SwapGroup.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Represents a swap group for mutually-exclusive bank switching.
/// </summary>
/// <remarks>
/// <para>
/// Swap groups model hardware bank switching where multiple memory regions
/// can occupy the same virtual address range, but only one is active at a time.
/// This is distinct from layers (which stack); swap groups are mutually exclusive
/// variants within a layer.
/// </para>
/// <para>
/// The Language Card has two 4KB banks for D000-DFFF; only one is active at a time.
/// Auxiliary memory has similar patterns. Swap groups implement this behavior.
/// </para>
/// </remarks>
public sealed class SwapGroup
{
    /// <summary>
    /// The variants registered in this swap group, keyed by variant name.
    /// </summary>
    private readonly Dictionary<string, SwapVariant> variants = new(StringComparer.Ordinal);

    /// <summary>
    /// Lock object for thread-safe operations.
    /// </summary>
    private readonly object syncLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SwapGroup"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this swap group.</param>
    /// <param name="name">The name of this swap group.</param>
    /// <param name="virtualBase">The starting virtual address for the swap group's address range.</param>
    /// <param name="size">The size of the address range in bytes.</param>
    public SwapGroup(uint id, string name, Addr virtualBase, Addr size)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        VirtualBase = virtualBase;
        Size = size;
    }

    /// <summary>
    /// Gets the unique identifier for this swap group.
    /// </summary>
    public uint Id { get; }

    /// <summary>
    /// Gets the name of this swap group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the starting virtual address for the swap group's address range.
    /// </summary>
    public Addr VirtualBase { get; }

    /// <summary>
    /// Gets the size of the address range in bytes.
    /// </summary>
    public Addr Size { get; }

    /// <summary>
    /// Gets the ending virtual address of this swap group (exclusive).
    /// </summary>
    public Addr VirtualEnd => VirtualBase + Size;

    /// <summary>
    /// Gets the name of the currently active variant, or <see langword="null"/> if no variant is active.
    /// </summary>
    public string? ActiveVariantName { get; private set; }

    /// <summary>
    /// Gets all variant names in this swap group.
    /// </summary>
    public IEnumerable<string> VariantNames => variants.Keys;

    /// <summary>
    /// Gets the number of variants in this swap group.
    /// </summary>
    public int VariantCount => variants.Count;

    /// <summary>
    /// Checks if the specified address falls within this swap group's address range.
    /// </summary>
    /// <param name="address">The address to check.</param>
    /// <returns><see langword="true"/> if the address is within the swap group's range; otherwise, <see langword="false"/>.</returns>
    public bool ContainsAddress(Addr address) => address >= VirtualBase && address < VirtualEnd;

    /// <summary>
    /// Adds a variant to this swap group.
    /// </summary>
    /// <param name="variant">The variant to add.</param>
    /// <exception cref="ArgumentException">Thrown when a variant with the same name already exists.</exception>
    public void AddVariant(SwapVariant variant)
    {
        lock (syncLock)
        {
            if (variants.ContainsKey(variant.Name))
            {
                throw new ArgumentException($"A variant with name '{variant.Name}' already exists in swap group '{Name}'.", nameof(variant));
            }

            variants[variant.Name] = variant;
        }
    }

    /// <summary>
    /// Gets a variant by name.
    /// </summary>
    /// <param name="variantName">The name of the variant to retrieve.</param>
    /// <returns>The variant if found.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no variant with the specified name exists.</exception>
    public SwapVariant GetVariant(string variantName)
    {
        lock (syncLock)
        {
            if (!variants.TryGetValue(variantName, out var variant))
            {
                throw new KeyNotFoundException($"Variant '{variantName}' not found in swap group '{Name}'.");
            }

            return variant;
        }
    }

    /// <summary>
    /// Tries to get a variant by name.
    /// </summary>
    /// <param name="variantName">The name of the variant to retrieve.</param>
    /// <param name="variant">When this method returns, contains the variant if found, or default if not.</param>
    /// <returns><see langword="true"/> if the variant was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetVariant(string variantName, out SwapVariant variant)
    {
        lock (syncLock)
        {
            return variants.TryGetValue(variantName, out variant);
        }
    }

    /// <summary>
    /// Sets the active variant name.
    /// </summary>
    /// <param name="variantName">The name of the variant to set as active.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no variant with the specified name exists.</exception>
    public void SetActiveVariant(string variantName)
    {
        lock (syncLock)
        {
            if (!variants.ContainsKey(variantName))
            {
                throw new KeyNotFoundException($"Variant '{variantName}' not found in swap group '{Name}'.");
            }

            ActiveVariantName = variantName;
        }
    }

    /// <summary>
    /// Gets the starting page index for this swap group.
    /// </summary>
    /// <param name="pageShift">The page shift value (e.g., 12 for 4KB pages).</param>
    /// <returns>The index of the first page covered by this swap group.</returns>
    public int GetStartPage(int pageShift) => (int)(VirtualBase >> pageShift);

    /// <summary>
    /// Gets the number of pages covered by this swap group.
    /// </summary>
    /// <param name="pageShift">The page shift value (e.g., 12 for 4KB pages).</param>
    /// <returns>The number of pages in this swap group.</returns>
    public int GetPageCount(int pageShift) => (int)(Size >> pageShift);
}