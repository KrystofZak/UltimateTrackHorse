using System.Diagnostics;
using UnityEngine;
using UnityEngine.Audio;
using Debug = UnityEngine.Debug;

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
        [SerializeField] private string carVolumeParameter = "CarVolume";


        private const string MasterKey = "audio.master";
        private const string MusicKey = "audio.music";
        private const string SfxKey = "audio.sfx";
        private const string UiKey = "audio.ui";
        private const string CarKey = "audio.car";

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
        public void SetCarVolume(float normalized)
        {
            SetVolume(carVolumeParameter, normalized);
            PlayerPrefs.SetFloat(CarKey, normalized);
        }

        public float GetSavedMasterVolume() => PlayerPrefs.GetFloat(MasterKey, 1f);
        public float GetSavedMusicVolume() => PlayerPrefs.GetFloat(MusicKey, 1f);
        public float GetSavedSfxVolume() => PlayerPrefs.GetFloat(SfxKey, 1f);
        public float GetSavedUiVolume() => PlayerPrefs.GetFloat(UiKey, 1f);
        public float GetSavedCarVolume() => PlayerPrefs.GetFloat(CarKey, 1f);

        private void LoadVolumes()
        {
            SetVolume(masterVolumeParameter, GetSavedMasterVolume());
            SetVolume(musicVolumeParameter, GetSavedMusicVolume());
            SetVolume(sfxVolumeParameter, GetSavedSfxVolume());
            SetVolume(uiVolumeParameter, GetSavedUiVolume());
            SetVolume(carVolumeParameter, GetSavedCarVolume());
        }

        private void SetVolume(string parameterName, float normalized)
        {
            if (!mixer)
            {
                Debug.Log("Mixer not set");
                return;
            }

            normalized = Mathf.Clamp(normalized, 0.0001f, 1f);
            var decibels = Mathf.Log10(normalized) * 20f;
            mixer.SetFloat(parameterName, decibels);
        }
    }
}