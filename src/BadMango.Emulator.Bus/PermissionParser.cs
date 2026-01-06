// <copyright file="PermissionParser.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

/// <summary>
/// Provides utility methods for parsing permission strings to <see cref="PagePerms"/> values.
/// </summary>
/// <remarks>
/// Permission strings use Unix-style characters: 'r' for read, 'w' for write, 'x' for execute,
/// and '-' for explicit no-permission placeholders. Characters can be in any order and are
/// case-insensitive.
/// </remarks>
public static class PermissionParser
{
    /// <summary>
    /// Parses a permission string to a <see cref="PagePerms"/> value.
    /// </summary>
    /// <param name="permissions">
    /// The permission string to parse (e.g., "rwx", "rx", "rw-").
    /// If <see langword="null"/> or empty, returns <see cref="PagePerms.All"/>.
    /// </param>
    /// <returns>The parsed <see cref="PagePerms"/> value.</returns>
    /// <exception cref="FormatException">
    /// Thrown when the permission string contains invalid characters.
    /// </exception>
    public static PagePerms Parse(string? permissions)
    {
        if (string.IsNullOrWhiteSpace(permissions))
        {
            return PagePerms.All;
        }

        PagePerms result = PagePerms.None;

        foreach (char c in permissions.ToLowerInvariant())
        {
            result |= c switch
            {
                'r' => PagePerms.Read,
                'w' => PagePerms.Write,
                'x' => PagePerms.Execute,
                '-' => PagePerms.None,
                _ => throw new FormatException($"Invalid permission character '{c}'"),
            };
        }

        return result;
    }

    /// <summary>
    /// Tries to parse a permission string to a <see cref="PagePerms"/> value.
    /// </summary>
    /// <param name="permissions">The permission string to parse.</param>
    /// <param name="result">
    /// When this method returns, contains the parsed value if successful;
    /// otherwise, <see cref="PagePerms.None"/>.
    /// </param>
    /// <returns><see langword="true"/> if parsing was successful; otherwise, <see langword="false"/>.</returns>
    public static bool TryParse(string? permissions, out PagePerms result)
    {
        result = PagePerms.None;

        if (string.IsNullOrWhiteSpace(permissions))
        {
            result = PagePerms.All;
            return true;
        }

        foreach (char c in permissions.ToLowerInvariant())
        {
            switch (c)
            {
                case 'r':
                    result |= PagePerms.Read;
                    break;
                case 'w':
                    result |= PagePerms.Write;
                    break;
                case 'x':
                    result |= PagePerms.Execute;
                    break;
                case '-':
                    // Explicit placeholder, no change
                    break;
                default:
                    result = PagePerms.None;
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Converts a <see cref="PagePerms"/> value to its string representation.
    /// </summary>
    /// <param name="perms">The permissions to convert.</param>
    /// <returns>A string representation (e.g., "rwx", "r-x", "---").</returns>
    public static string ToString(PagePerms perms)
    {
        char r = perms.HasFlag(PagePerms.Read) ? 'r' : '-';
        char w = perms.HasFlag(PagePerms.Write) ? 'w' : '-';
        char x = perms.HasFlag(PagePerms.Execute) ? 'x' : '-';

        return $"{r}{w}{x}";
    }
}