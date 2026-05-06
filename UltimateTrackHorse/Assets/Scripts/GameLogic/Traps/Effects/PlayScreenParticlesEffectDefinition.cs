using GameLogic.Traps.Collisions;
using GameLogic.Traps.Definitions;
using UnityEngine;

namespace GameLogic.Traps.Effects
{
    /// <summary>
    /// Trap effect that plays a configured screen particle profile.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Effects/Play Screen Particles")]
    public class PlayScreenParticlesEffectDefinition : TrapEffectDefinition
    {
        [SerializeField] private ScreenParticleProfile profile;

        public override void Execute(TrapExecutionContext context)
        {
            if (!profile)
                return;

            if (context.Services == null || context.Services.ScreenParticles == null)
                return;

            context.Services.ScreenParticles.Play(profile);
        }
    }
}