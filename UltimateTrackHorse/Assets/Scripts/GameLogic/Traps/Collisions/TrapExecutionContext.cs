using GameLogic.Traps.Core;
using UnityEngine;

namespace GameLogic.Traps.Collisions
{
    /// <summary>
    /// Immutable data bundle passed to trap effects during activation.
    /// </summary>
    public sealed class TrapExecutionContext
    {
        /// <summary>
        /// Runtime instance that triggered the current execution.
        /// </summary>
        public TrapRuntime Trap { get; }

        /// <summary>
        /// Game object that caused the trap to activate.
        /// </summary>
        public GameObject Instigator { get; }

        /// <summary>
        /// Collision information for physics-based activations, if available.
        /// </summary>
        public Collision Collision { get; }

        /// <summary>
        /// Trigger information for trigger-based activations, if available.
        /// </summary>
        public Collider Trigger { get; }

        /// <summary>
        /// Shared services exposed by the trap runtime.
        /// </summary>
        public TrapServices Services => Trap.Services;

        /// <summary>
        /// Creates a new trap execution context.
        /// </summary>
        /// <param name="trap">Runtime instance handling the activation.</param>
        /// <param name="instigator">Game object that activated the trap.</param>
        /// <param name="collision">Collision payload when activated by collision.</param>
        /// <param name="trigger">Trigger payload when activated by trigger entry.</param>
        public TrapExecutionContext(
            TrapRuntime trap,
            GameObject instigator,
            Collision collision = null,
            Collider trigger = null)
        {
            Trap = trap;
            Instigator = instigator;
            Collision = collision;
            Trigger = trigger;
        }
    }
}
