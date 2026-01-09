// <copyright file="MachineBuilderExtensions.cs" company="Bad Mango Solutions">
// Copyright (c) Bad Mango Solutions. All rights reserved.
// </copyright>

namespace BadMango.Emulator.Bus;

using BadMango.Emulator.Bus.Interfaces;

/// <summary>
/// Extension methods for <see cref="MachineBuilder"/> providing composite handler registration.
/// </summary>
/// <remarks>
/// <para>
/// These extensions provide handler factory registration for profile-based machine loading.
/// They are designed to be minimal and self-contained, registering only the factory functions
/// without creating memory layouts, device registrations, or other side effects.
/// </para>
/// <para>
/// The actual components are created on-demand when <see cref="MachineBuilder.FromProfile"/>
/// processes composite regions that reference the registered handlers.
/// </para>
/// </remarks>
public static class MachineBuilderExtensions
{
    /// <summary>
    /// Extension block for <see cref="MachineBuilder"/> providing composite handler registration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide handler factory registration for profile-based machine loading.
    /// </para>
    /// </remarks>
    extension(MachineBuilder builder)
    {
        /// <summary>
        /// Registers the composite-io handler factory for profile-based loading.
        /// </summary>
        /// <returns>This builder instance for method chaining.</returns>
        /// <remarks>
        /// <para>
        /// This method ONLY registers the "composite-io" handler factory. It does NOT:
        /// <list type="bullet">
        /// <item><description>Create memory layouts</description></item>
        /// <item><description>Register device info entries</description></item>
        /// <item><description>Create controllers or other components</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// The handler factory creates the <see cref="IOPageDispatcher"/> and
        /// <see cref="SlotManager"/> on demand when the composite region is processed
        /// by <see cref="MachineBuilder.FromProfile"/>. This keeps profile loading
        /// self-contained and avoids duplicate device registrations.
        /// </para>
        /// <para>
        /// Call this method before <see cref="MachineBuilder.FromProfile"/> when loading
        /// profiles that use the "composite-io" handler.
        /// </para>
        /// </remarks>
        public MachineBuilder RegisterCompositeIOHandler()
        {
            // Register the composite handler factory - it creates the dispatcher and
            // slot manager on demand, adding them as components for later retrieval
            return builder.RegisterCompositeHandler("composite-io", b =>
            {
                // Try to get existing components first (in case they were added elsewhere)
                var dispatcher = GetComponentInternal<IOPageDispatcher>(b);
                var slotManager = GetComponentInternal<ISlotManager>(b);

                // Create them if they don't exist
                if (dispatcher is null)
                {
                    dispatcher = new();
                    b.AddComponent(dispatcher);
                }

                if (slotManager is null)
                {
                    slotManager = new SlotManager(dispatcher);
                    b.AddComponent<ISlotManager>(slotManager);
                }

                return new CompositeIOTarget("I/O Page", dispatcher, slotManager);
            });
        }
    }

    /// <summary>
    /// Gets a component from the builder's component list.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="builder">The builder to search.</param>
    /// <returns>The component if found; otherwise, <see langword="null"/>.</returns>
    /// <remarks>
    /// This is used internally by composite handler factories to retrieve
    /// dependencies that were added to the builder.
    /// </remarks>
    internal static T? GetComponentInternal<T>(MachineBuilder builder)
        where T : class
    {
        // Access internal components list via reflection for now
        // This is a temporary solution; ideally we'd expose a method on MachineBuilder
        var componentsField = typeof(MachineBuilder).GetField(
            "components",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (componentsField?.GetValue(builder) is List<object> components)
        {
            return components.OfType<T>().FirstOrDefault();
        }

        return null;
    }
}