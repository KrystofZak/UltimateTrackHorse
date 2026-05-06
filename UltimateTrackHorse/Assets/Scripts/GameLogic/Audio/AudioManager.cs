using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace GameLogic.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [Header("Persistent Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Mixer Routing")]
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup uiMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup vehicleMixerGroup;

        [Header("Optional Parent")]
        [SerializeField] private Transform oneShotRoot;

        private void Awake()
        {
            if (oneShotRoot == null)
            {
                var root = new GameObject("OneShotAudio");
                root.transform.SetParent(transform);
                oneShotRoot = root.transform;
            }

            ConfigurePersistentSource(musicSource, musicMixerGroup, true);
            ConfigurePersistentSource(uiSource, uiMixerGroup, false);
        }

        public void PlayMusic(AudioCue cue)
        {
            if (!cue || musicSource == null) return;

            var clip = cue.GetRandomClip();
            if (!clip) return;

            musicSource.outputAudioMixerGroup = musicMixerGroup;
            musicSource.clip = clip;
            musicSource.volume = cue.GetRandomVolume();
            musicSource.pitch = cue.GetRandomPitch();
            musicSource.loop = cue.Loop;
            musicSource.spatialBlend = 0f;
            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlayUI(AudioCue cue)
        {
            if (!cue || uiSource == null) return;

            var clip = cue.GetRandomClip();
            if (!clip) return;

            uiSource.outputAudioMixerGroup = uiMixerGroup;
            uiSource.PlayOneShot(clip, cue.GetRandomVolume());
        }

        public void PlaySfx2D(AudioCue cue)
        {
            if (!cue) return;

            var clip = cue.GetRandomClip();
            if (!clip) return;

            var source = CreateTempSource("Sfx2D", Vector3.zero, false);
            ApplyCue(source, cue, clip);
            source.spatialBlend = 0f;
            source.Play();

            Destroy(source.gameObject, clip.length / Mathf.Max(0.01f, source.pitch));
        }

        public void PlaySfx3D(AudioCue cue, Vector3 position)
        {
            if (!cue) return;

            var clip = cue.GetRandomClip();
            if (!clip) return;

            var source = CreateTempSource("Sfx3D", position, true);
            ApplyCue(source, cue, clip);
            source.Play();

            Destroy(source.gameObject, clip.length / Mathf.Max(0.01f, source.pitch));
        }

        public AudioSource CreateManagedLoop(AudioCue cue, Transform followTarget, string objectName = "ManagedLoop")
        {
            if (cue == null || followTarget == null)
                return null;

            var clip = cue.GetRandomClip();
            if (!clip)
                return null;

            var go = new GameObject(objectName);
            go.transform.SetParent(followTarget, false);
            go.transform.localPosition = Vector3.zero;

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;

            ApplyCue(source, cue, clip);
            source.loop = true;
            source.volume = 0f;

            source.Play();
            return source;
        }

        public void SetManagedLoopState(AudioSource source, float normalizedVolume, float pitch)
        {
            if (source == null) return;

            source.volume = Mathf.Clamp01(normalizedVolume);
            source.pitch = Mathf.Max(0.01f, pitch);
        }

        public void StopManagedLoop(AudioSource source, float fadeOutDuration = 0f)
        {
            if (source == null) return;

            if (fadeOutDuration <= 0f)
            {
                source.Stop();
                Destroy(source.gameObject);
                return;
            }

            StartCoroutine(FadeOutAndDestroy(source, fadeOutDuration));
        }

        private IEnumerator FadeOutAndDestroy(AudioSource source, float duration)
        {
            if (source == null) yield break;

            float startVolume = source.volume;
            float time = 0f;

            while (time < duration && source != null)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / duration);
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (source != null)
            {
                source.Stop();
                Destroy(source.gameObject);
            }
        }

        private AudioSource CreateTempSource(string objectName, Vector3 position, bool spatial)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(oneShotRoot);
            go.transform.position = position;

            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = sfxMixerGroup;
            source.spatialBlend = spatial ? 1f : 0f;

            return source;
        }

        private void ApplyCue(AudioSource source, AudioCue cue, AudioClip clip)
        {
            source.clip = clip;
            source.volume = cue.GetRandomVolume();
            source.pitch = cue.GetRandomPitch();
            source.loop = cue.Loop;
            source.spatialBlend = cue.Spatial ? cue.SpatialBlend : 0f;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.outputAudioMixerGroup = GetMixerGroup(cue.Category);
        }

        private AudioMixerGroup GetMixerGroup(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Music => musicMixerGroup,
                AudioCategory.UI => uiMixerGroup,
                AudioCategory.Vehicle => vehicleMixerGroup,
                _ => sfxMixerGroup
            };
        }

        private static void ConfigurePersistentSource(AudioSource source, AudioMixerGroup group, bool loop)
        {
            if (source == null) return;

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = group;
        }
    }
}