using UnityEngine;
using UnityEngine.Audio;

namespace GameLogic.Audio
{
    public class AudioMixerController : MonoBehaviour
    {
        [SerializeField] private AudioMixer mixer;

        [Header("Exposed Parameters")]
        [SerializeField] private string masterVolumeParameter = "MasterVolume";
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SfxVolume";
        [SerializeField] private string uiVolumeParameter = "UiVolume";

        private const string MasterKey = "audio.master";
        private const string MusicKey = "audio.music";
        private const string SfxKey = "audio.sfx";
        private const string UiKey = "audio.ui";

        private void Start()
        {
            LoadVolumes();
        }

        public void SetMasterVolume(float normalized)
        {
            SetVolume(masterVolumeParameter, normalized);
            PlayerPrefs.SetFloat(MasterKey, normalized);
        }

        public void SetMusicVolume(float normalized)
        {
            SetVolume(musicVolumeParameter, normalized);
            PlayerPrefs.SetFloat(MusicKey, normalized);
        }

        public void SetSfxVolume(float normalized)
        {
            SetVolume(sfxVolumeParameter, normalized);
            PlayerPrefs.SetFloat(SfxKey, normalized);
        }

        public void SetUiVolume(float normalized)
        {
            SetVolume(uiVolumeParameter, normalized);
            PlayerPrefs.SetFloat(UiKey, normalized);
        }

        public float GetSavedMasterVolume() => PlayerPrefs.GetFloat(MasterKey, 1f);
        public float GetSavedMusicVolume() => PlayerPrefs.GetFloat(MusicKey, 1f);
        public float GetSavedSfxVolume() => PlayerPrefs.GetFloat(SfxKey, 1f);
        public float GetSavedUiVolume() => PlayerPrefs.GetFloat(UiKey, 1f);

        private void LoadVolumes()
        {
            SetVolume(masterVolumeParameter, GetSavedMasterVolume());
            SetVolume(musicVolumeParameter, GetSavedMusicVolume());
            SetVolume(sfxVolumeParameter, GetSavedSfxVolume());
            SetVolume(uiVolumeParameter, GetSavedUiVolume());
        }

        private void SetVolume(string parameterName, float normalized)
        {
            if (!mixer) return;

            normalized = Mathf.Clamp(normalized, 0.0001f, 1f);
            var decibels = Mathf.Log10(normalized) * 20f;
            mixer.SetFloat(parameterName, decibels);
        }
    }
}