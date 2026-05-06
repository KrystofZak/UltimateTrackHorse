using UnityEngine;

namespace GameLogic.Traps.Definitions
{
    /// <summary>
    /// Configuration asset for screen-space particle effects such as smoke.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Screen Particle Profile")]
    public class ScreenParticleProfile : ScriptableObject
    {
        [Header("Prefab")]
        public ParticleSystem particlePrefab;

        [Header("Tint")]
        public Color tint = new Color(0.65f, 0.65f, 0.65f, 0.9f);

        [Header("Placement relative to camera")]
        public Vector3 localPosition = new Vector3(0f, -1.8f, 4f);
        public Vector3 localEulerAngles = Vector3.zero;
        public Vector3 localScale = Vector3.one;

        [Header("Lifetime")]
        [Min(0.1f)] public float cleanupDelay = 4f;
        [Min(0f)] public float interruptFadeOut = 0.15f;
    }
}