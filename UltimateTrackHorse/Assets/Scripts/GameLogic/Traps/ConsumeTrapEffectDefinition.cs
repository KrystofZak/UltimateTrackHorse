using UnityEngine;

namespace GameLogic.Traps
{
    /// <summary>
    /// Trap effect that marks the trap as consumed and destroys its runtime object.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Effects/Consume Trap")]
    public class ConsumeTrapEffectDefinition : TrapEffectDefinition
    {
        [SerializeField] private float destroyDelay = 0f;

        /// <summary>
        /// Consumes the trap and schedules its destruction.
        /// </summary>
        /// <param name="context">Execution data for the trap activation.</param>
        public override void Execute(TrapExecutionContext context)
        {
            context.Trap.ConsumeAndDestroy(destroyDelay);
        }
    }
}
