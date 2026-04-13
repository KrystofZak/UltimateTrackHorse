using GameLogic.Traps.Collisions;
using UnityEngine;

namespace GameLogic.Traps.Definitions
{
    /// <summary>
    /// Base asset for a single action performed when a trap activates.
    /// </summary>
    public abstract class TrapEffectDefinition : ScriptableObject
    {
        /// <summary>
        /// Executes the effect using the provided trap activation context.
        /// </summary>
        /// <param name="context">Execution data for the current trap activation.</param>
        public abstract void Execute(TrapExecutionContext context);
    }
}
