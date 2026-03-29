using System.Collections.Generic;
using System.Collections; // Added for Coroutines
using UnityEngine;
using Cinemachine;
using GameLogic.Obstacles;
using MapGeneration;
using UI;

namespace GameLogic
{
    /// <summary>
    /// Class that manages the game logic and interactions between the player and the map.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public GameObject playerCar;
        public MapGenerator mapGenerator;
        private int lapCount;
        private float totalTimeComplexity;
        private Timer timer;
        
        // List to hold the history of lap completion times for the current track session
        private List<float> currentMapLapTimes = new List<float>();

        [SerializeField] private SpawnObstacle spawnObstacle;
        
        // Replaced old UIManager with the new UIController
        private UIController uiController;

        /// <summary>
        /// Subscribe to the finish line event when the game manager is enabled, and unsubscribe when disabled.
        /// </summary>
        void OnEnable() 
        { 
            FinishLine.OnPlayerFinished += ResetToStart; 
            uiController = FindObjectOfType<UIController>();
        }
        void OnDisable() 
        { 
            FinishLine.OnPlayerFinished -= ResetToStart;
            if (timer != null)
            {
                timer.OnTimeUp -= HandleTimeUp;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartCurrentLap();
            }

            if (uiController == null)
            {
                uiController = FindObjectOfType<UIController>();
            }

            if (uiController != null && uiController.IsObstacleChoiceViewActive)
            {
                CarController carController = playerCar.GetComponent<CarController>();
                if (carController != null)
                {
                    carController.isInputEnabled = false;
                }
            }
        }

        public void RestartCurrentLap()
        {
            Debug.Log("Restarting lap...");
            PlaceCarOnStart();
            ObstacleManager.Instance.ResetObstacles();

            if (timer != null)
            {
                timer.ResetTimer();
                timer.SetStartTime(totalTimeComplexity, false); // Don't tick down yet!
            }
            
            // Commence the 3.. 2.. 1.. Sequence
            StartCoroutine(RaceCountdownCoroutine());
        }

        public void DestroyTrack()
        {
            
        }

        public void SetupNewTrack()
        {
            // Fully wipe the history list and hide the panel when generating a brand new track
            currentMapLapTimes.Clear();
            if (uiController != null)
            {
                uiController.HideLapHistory();
            }

            CalculateTotalTimeComplexity();
            
            timer = FindObjectOfType<Timer>();

            if (timer != null)
            {
                // When we generate an entirely brand new track, we must clear the old 
                // bonus/penalty stack completely before starting.
                timer.ResetIncrement();
                
                timer.SetStartTime(totalTimeComplexity, false); // Don't tick down yet!
                timer.OnTimeUp += HandleTimeUp;
            }
            else
            {
                Debug.LogWarning("GameManager: Timer not found in the scene! Ensure a Timer component exists.");
            }

            // A fallback to ensure UI sets to unpaused correctly
            Time.timeScale = 1f;

            // Commence the 3.. 2.. 1.. Sequence
            StartCoroutine(RaceCountdownCoroutine());
        }

        private void HandleTimeUp()
        {
            Debug.Log("game over, time is up");
            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.isInputEnabled = false;
            }
        }

        private void CalculateTotalTimeComplexity()
        {
            totalTimeComplexity = 0f;
            if (mapGenerator.GeneratedPath == null) return;

            foreach (var pos in mapGenerator.GeneratedPath)
            {
                var cell = mapGenerator.GetCell(pos.x, pos.y);
                if (cell != null && cell.CollapsedVariant != null)
                {
                    totalTimeComplexity += cell.CollapsedVariant.Data.timeComplexity;
                }
            }
            Debug.Log($"Total time to beat: {totalTimeComplexity} seconds.");
        }

        public void OnChoiceClicked()
        {
            if (timer != null)
            {
                timer.ResetTimer();
                timer.SetStartTime(totalTimeComplexity, false); // Don't tick down yet!
            }

            // Commence the 3.. 2.. 1.. Sequence
            StartCoroutine(RaceCountdownCoroutine());
        }

        /// <summary>
        /// The main Sequence Controller that perfectly hooks up holding the player input, counting down visually on screen, 
        /// and unleashing them and the official clock precisely when it strikes "GO!".
        /// </summary>
        private IEnumerator RaceCountdownCoroutine()
        {
            // Specifically disable the heavy physical car controls
            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null) carController.isInputEnabled = false;

            if (uiController != null) uiController.ShowCountdown(true);
            
            // Wait for 3, 2, 1
            for (int i = 3; i > 0; i--)
            {
                if (uiController != null) uiController.UpdateCountdownText(i.ToString());
                // Crucial to use real time just in case TimeScale somehow locked up
                yield return new WaitForSecondsRealtime(1f); 
            }

            // "GO!" state
            if (uiController != null) uiController.UpdateCountdownText("GO!");
            
            // Release the physical brakes
            if (carController != null) carController.isInputEnabled = true;
            
            // Begin counting down the real time clock!
            if (timer != null)
            {
                timer.StartTimer();
            }

            // Hold the "GO!" sign on screen for just one final second before hiding it
            yield return new WaitForSecondsRealtime(1f);
            
            if (uiController != null) uiController.ShowCountdown(false);
        }

        /// <summary>
        /// Handles the logic when the player finishes the round.
        /// Place the player's car at the starting position (1,1) on the map.
        /// </summary>
        private void ResetToStart()
        {
            if (uiController != null)
            {
                uiController.ShowObstacleChoiceView();
            }
            
            if (timer != null)
            {
                timer.StopTimer();
                Debug.Log("Lap time: " + timer.timeElapsed);
                // Record the lap time into the history tracker
                currentMapLapTimes.Add(timer.timeElapsed);
            }

            // Immediately send the updated history list to the UI Controller to be drawn
            if (uiController != null)
            {
                uiController.UpdateLapHistoryUI(currentMapLapTimes);
            }

            lapCount++;

            Debug.Log("Completed laps: " + lapCount);
            PlaceCarOnStart();
            ObstacleManager.Instance.ResetObstacles();
        }
        
        /// <summary>
        /// Place the player's car at the starting position (1,1) on the map,
        /// with the correct rotation based on the tile variant.
        /// </summary>
        public void PlaceCarOnStart()
        {
            if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator == null) 
            {
                Debug.LogError("GameManager: Cannot execute PlaceCarOnStart because MapGenerator is missing!");
                return;
            }

            var startCell = mapGenerator.GetCell(1, 1);

            if (startCell != null && startCell.CollapsedVariant != null)
            {
                float size = mapGenerator.tileSize;
                Vector3 startPos = new Vector3(1 * size, 1f, 1 * size);
                Quaternion startRot = Quaternion.Euler(0, startCell.CollapsedVariant.Rotation * 90f, 0);

                Rigidbody rb = playerCar.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // Disable physics for the player car while setting position and rotation
                    rb.isKinematic = true;
                    rb.position = startPos;
                    rb.rotation = startRot;

                    // Set position and rotation of the player car
                    playerCar.transform.SetPositionAndRotation(startPos, startRot);

                    rb.isKinematic = false;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    playerCar.transform.SetPositionAndRotation(startPos, startRot);
                }
                Physics.SyncTransforms();

                CarController carController = playerCar.GetComponent<CarController>();
                if (carController != null)
                {
                    carController.isInputEnabled = true;

                    // Small nudge just in case ground checks freeze
                    if (rb != null) rb.WakeUp();
                }

                CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();

                if (vcam != null)
                {
                    vcam.PreviousStateIsValid = false;

                    // Restart Cinemachine
                    vcam.enabled = false;
                    vcam.enabled = true;
                }
            }
            else
            {
                Debug.LogWarning("GameManager: Could not PlaceCarOnStart because start cell is null or uncollapsed.");
            }
        }
        
    }
}
