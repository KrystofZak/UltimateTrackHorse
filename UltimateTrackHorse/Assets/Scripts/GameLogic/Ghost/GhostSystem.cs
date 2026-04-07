using UnityEngine;

namespace GameLogic.Ghost
{
    public class GhostSystem : MonoBehaviour
    {
        [SerializeField] private GhostLapRecorder recorder;
        [SerializeField] private GhostReplayController replayController;
        [SerializeField] private string currentMapId;

        private GhostLapData currentBestLap;

        private void Start()
        {
            currentBestLap = GhostPersistence.LoadBestLap(currentMapId);

            if (currentBestLap != null)
            {
                replayController.Play(currentBestLap);
            }
            else
            {
                replayController.StopReplay();
            }
        }

        public void StartLap()
        {
            recorder.BeginRecording();

            if (currentBestLap != null)
                replayController.Play(currentBestLap);
        }

        public void FinishLap(float lapTimeFromRaceSystem)
        {
            GhostLapData newLap = recorder.EndRecording(currentMapId);
            if (newLap == null)
                return;

            newLap.lapTime = lapTimeFromRaceSystem;

            bool isNewBest = currentBestLap == null || lapTimeFromRaceSystem < currentBestLap.lapTime;

            if (!isNewBest) return;
            currentBestLap = newLap;
            GhostPersistence.SaveBestLap(currentBestLap);
        }

        public void SetMapId(string mapId)
        {
            currentMapId = mapId;
            currentBestLap = GhostPersistence.LoadBestLap(currentMapId);

            if (currentBestLap != null)
                replayController.Play(currentBestLap);
            else
                replayController.StopReplay();
        }
    }
}