using UnityEngine;

namespace GameLogic.Ghost
{
    public class GhostSystem : MonoBehaviour
    {
        [SerializeField] private GhostLapRecorder recorder;
        [SerializeField] private GhostReplayController replayController;
        private string currentMapId;

        private GhostLapData currentBestLap;

        private void Start()
        {
            LoadAndApplyCurrentMapGhost();
            LoadReferences();
        }

        private void LoadReferences()
        {
            if (!recorder)
            {
                recorder = FindAnyObjectByType<GhostLapRecorder>();
                Debug.LogWarning("Recorder is not assigned. Loaded from scene.");
            }

            if (!replayController)
            {
                replayController = FindAnyObjectByType<GhostReplayController>();
                Debug.LogWarning("ReplayController is not assigned. Loaded from scene.");
            }
        }

        public void StartLap()
        {
            Debug.Log($"Ghost StartLap currentBestLap null? {currentBestLap == null}");
            
            if (!recorder)
            {
                Debug.LogWarning("GhostSystem: Recorder is not assigned.", this);
                return;
            }

            recorder.BeginRecording();

            if (!replayController) return;
            
            if (currentBestLap != null)
                replayController.Play(currentBestLap);
            else
                replayController.StopReplay();
        }

        public void FinishLap(float lapTimeFromRaceSystem)
        {
            
            GhostLapData newLap = recorder.EndRecording(currentMapId);
            if (newLap == null)
                return;

            newLap.lapTime = lapTimeFromRaceSystem;

            bool isNewBest = currentBestLap == null || lapTimeFromRaceSystem < currentBestLap.lapTime;

            
            Debug.Log($"Ghost FinishLap mapId={currentMapId}, lap={lapTimeFromRaceSystem}");
            Debug.Log($"Ghost new best? {isNewBest}");
            Debug.Log($"Ghost currentBestLap null? {currentBestLap == null}");
            
            if (!isNewBest) return;

            currentBestLap = newLap;
            GhostPersistence.SaveBestLap(currentBestLap);
            
            if (replayController != null)
            {
                replayController.Play(currentBestLap);
            }
        }

        public void SetMapId(string mapId)
        {
            currentMapId = mapId;
            LoadAndApplyCurrentMapGhost();
        }

        private void LoadAndApplyCurrentMapGhost()
        {
            if (string.IsNullOrWhiteSpace(currentMapId))
            {
                currentBestLap = null;

                if (replayController != null)
                    replayController.StopReplay();

                return;
            }

            currentBestLap = GhostPersistence.LoadBestLap(currentMapId);

            if (replayController == null)
                return;

            if (currentBestLap != null)
                replayController.Play(currentBestLap);
            else
                replayController.StopReplay();
        }
    }
}