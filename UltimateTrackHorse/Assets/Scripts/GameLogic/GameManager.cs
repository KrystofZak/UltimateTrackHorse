using System.Collections.Generic;
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
                timer.SetStartTime(totalTimeComplexity);
            }
            
            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.isInputEnabled = true;
            }
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
                
                timer.SetStartTime(totalTimeComplexity);
                timer.OnTimeUp += HandleTimeUp;
            }
            else
            {
                Debug.LogWarning("GameManager: Timer not found in the scene! Ensure a Timer component exists.");
            }

            // A fallback to ensure UI sets to unpaused correctly
            Time.timeScale = 1f;

            // Enforce car controls correctly initialized
            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.isInputEnabled = true;
            }
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
                timer.SetStartTime(totalTimeComplexity);
            }

            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.isInputEnabled = true;
            }
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
