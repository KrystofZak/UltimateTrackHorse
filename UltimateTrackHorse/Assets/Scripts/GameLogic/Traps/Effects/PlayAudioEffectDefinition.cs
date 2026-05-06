using GameLogic.Audio;
using GameLogic.Traps.Collisions;
using GameLogic.Traps.Definitions;
using UnityEngine;

namespace GameLogic.Traps.Effects
{
    public enum TrapAudioPlaybackMode
    {
        TrapPosition,
        InstigatorPosition,
        Screen2D
    }

    [CreateAssetMenu(menuName = "Game/Traps/Effects/Play Audio")]
    public class PlayAudioEffectDefinition : TrapEffectDefinition
    {
        [SerializeField] private AudioCue cue;
        [SerializeField] private TrapAudioPlaybackMode playbackMode = TrapAudioPlaybackMode.TrapPosition;

        public override void Execute(TrapExecutionContext context)
        {
            if (!cue || !context.Services.Audio){
                Debug.LogWarning($"{nameof(PlayAudioEffectDefinition)}: cue is null");
                return;
            }

            switch (playbackMode)
            {
                case TrapAudioPlaybackMode.Screen2D:
                    context.Services.Audio.PlaySfx2D(cue);
                    break;

                case TrapAudioPlaybackMode.InstigatorPosition:
                    if (context.Instigator != null)
                        context.Services.Audio.PlaySfx3D(cue, context.Instigator.transform.position);
                    break;

                case TrapAudioPlaybackMode.TrapPosition:
                default:
                    context.Services.Audio.PlaySfx3D(cue, context.Trap.transform.position);
                    break;
            }
        }
    }
}