using GameLogic.Traps.Collisions;
using GameLogic.Traps.Definitions;
using UnityEngine;

namespace GameLogic.Traps.Effects
{
    /// <summary>
    /// Trap effect that plays a configured screen overlay profile through the overlay service.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Effects/Play Screen Overlay")]
    public class PlayScreenOverlayEffectDefinition : TrapEffectDefinition
    {
        [SerializeField] private ScreenOverlayProfile profile;

        /// <summary>
        /// Plays the configured overlay profile if one is assigned.
        /// </summary>
        /// <param name="context">Execution data for the trap activation.</param>
        public override void Execute(TrapExecutionContext context)
        {
            if (!profile) return;
            context.Services.ScreenOverlay.Play(profile);
        }
    }
}
