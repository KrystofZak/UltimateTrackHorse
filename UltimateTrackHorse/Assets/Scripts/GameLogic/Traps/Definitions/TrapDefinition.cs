using UnityEngine;

namespace GameLogic.Traps.Definitions
{
    /// <summary>
    /// High-level grouping used to categorize trap prefabs.
    /// </summary>
    public enum TrapCategory
    {
        /// <summary>
        /// A trap that behaves like a wall or obstacle.
        /// </summary>
        Wall,

        /// <summary>
        /// A trap that obstructs visibility or creates a fog-like effect.
        /// </summary>
        Fog,

        /// <summary>
        /// TODO: Add refactored logic to this shit
        /// </summary>
        Surfaces
    }

    /// <summary>
    /// Determines whether a trap can activate once or repeatedly.
    /// </summary>
    public enum TrapActivationMode
    {
        /// <summary>
        /// The trap activates a single time and then becomes consumed.
        /// </summary>
        SingleUse,

        /// <summary>
        /// The trap can activate multiple times with an optional cooldown.
        /// </summary>
        Repeatable
    }

    /// <summary>
    /// Asset describing a trap's category, activation rules, and effect sequence.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Trap Definition")]
    public class TrapDefinition : ScriptableObject
    {
        /// <summary>
        /// Category used to classify the trap.
        /// </summary>
        public TrapCategory category;

        /// <summary>
        /// Activation behavior used by the runtime.
        /// </summary>
        public TrapActivationMode activationMode = TrapActivationMode.SingleUse;

        /// <summary>
        /// Minimum time in seconds before a repeatable trap can activate again.
        /// </summary>
        [Min(0f)] public float repeatCooldown = 0f;

        /// <summary>
        /// Effects executed in order when the trap activates.
        /// </summary>
        public TrapEffectDefinition[] effects;
    }
}
