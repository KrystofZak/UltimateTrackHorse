using UnityEngine;

namespace GameLogic.Audio
{
    public class GameAudioController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private AudioManager audioManager;

        [Header("Countdown")]
        [SerializeField] private AudioCue countdownTickCue;
        [SerializeField] private AudioCue countdownGoCue;

        [Header("Race")]
        [SerializeField] private AudioCue lapFinishedCue;
        [SerializeField] private AudioCue victoryCue;
        [SerializeField] private AudioCue defeatCue;
        [SerializeField] private AudioCue gameplayMusicCue;

        private void OnEnable()
        {
            if (!gameManager) gameManager = FindAnyObjectByType<GameManager>();
            if (!audioManager) audioManager = FindAnyObjectByType<AudioManager>();

            if (!gameManager) return;

            gameManager.OnCountdownTick += HandleCountdownTick;
            gameManager.OnCountdownGo += HandleCountdownGo;
            gameManager.OnRaceStarted += HandleRaceStarted;
            gameManager.OnLapFinished += HandleLapFinished;
            gameManager.OnVictory += HandleVictory;
            gameManager.OnDefeat += HandleDefeat;
        }

        private void OnDisable()
        {
            if (gameManager == null) return;

            gameManager.OnCountdownTick -= HandleCountdownTick;
            gameManager.OnCountdownGo -= HandleCountdownGo;
            gameManager.OnRaceStarted -= HandleRaceStarted;
            gameManager.OnLapFinished -= HandleLapFinished;
            gameManager.OnVictory -= HandleVictory;
            gameManager.OnDefeat -= HandleDefeat;
        }

        private void HandleCountdownTick(int _) => audioManager?.PlayUI(countdownTickCue);
        private void HandleCountdownGo() => audioManager?.PlayUI(countdownGoCue);
        private void HandleRaceStarted() => audioManager?.PlayMusic(gameplayMusicCue);
        private void HandleLapFinished() => audioManager?.PlaySfx2D(lapFinishedCue);
        private void HandleVictory() => audioManager?.PlaySfx2D(victoryCue);
        private void HandleDefeat() => audioManager?.PlaySfx2D(defeatCue);
    }
}