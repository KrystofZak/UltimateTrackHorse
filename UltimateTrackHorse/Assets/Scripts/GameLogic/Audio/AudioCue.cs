using UnityEngine;

namespace GameLogic.Audio
{
    public enum AudioCategory
    {
        Music,
        UI,
        Sfx
    }

    [CreateAssetMenu(menuName = "Game/Audio/Audio Cue")]
    public class AudioCue : ScriptableObject
    {
        [Header("Clips")]
        [SerializeField] private AudioClip[] clips;

        [Header("Playback")]
        [SerializeField] private AudioCategory category = AudioCategory.Sfx;
        [SerializeField] private bool spatial = false;
        [SerializeField] private bool loop = false;

        [Header("Randomization")]
        [SerializeField] private Vector2 volumeRange = new(1f, 1f);
        [SerializeField] private Vector2 pitchRange = new(1f, 1f);

        [Header("3D Settings")]
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 20f;

        public AudioCategory Category => category;
        public bool Spatial => spatial;
        public bool Loop => loop;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0)
                return null;

            return clips[Random.Range(0, clips.Length)];
        }

        public float GetRandomVolume()
        {
            return Random.Range(volumeRange.x, volumeRange.y);
        }

        public float GetRandomPitch()
        {
            return Random.Range(pitchRange.x, pitchRange.y);
        }
    }
}