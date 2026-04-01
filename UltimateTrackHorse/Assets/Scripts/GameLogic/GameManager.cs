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

        // Variables to hold the current respawn position and rotation, set by checkpoints
        private Vector3 currentRespawnPos;
        private Quaternion currentRespawnRot;

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
                //RestartCurrentLap();
                RespawnCar(true);
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
        /// Set the respawn position and rotation for the player's car.'
        /// </summary>
        /// <param name="pos">Position to be set</param>
        /// <param name="rot">Rotation to be set</param>
        public void SetRespawnPoint(Vector3 pos, Quaternion rot)
        {
            currentRespawnPos = pos;
            currentRespawnRot = rot;
        }
        
        /// <summary>
        /// Place the player's car at the starting position (1,1) on the map,
        /// with the correct rotation based on the tile variant.
        /// </summary>
        public void PlaceCarOnStart()
        {
            if (mapGenerator == null) mapGenerator = FindObjectOfType<MapGenerator>();
            if (mapGenerator == null) return; 

            var startCell = mapGenerator.GetCell(1, 1);

            // Check if the cell exists and has a collapsed variant, and that the generated path is valid
            if (startCell != null && startCell.CollapsedVariant != null && mapGenerator.GeneratedPath != null && mapGenerator.GeneratedPath.Count > 1)
            {
                float size = mapGenerator.tileSize;
                Vector3 startPos = new Vector3(1 * size, 0.05f, 1 * size);
        
                // Get the direction of the first segment of the generated path to determine the correct rotation
                Vector2Int gridDir = mapGenerator.GeneratedPath[1] - mapGenerator.GeneratedPath[0];
                Vector3 worldDir = new Vector3(gridDir.x, 0, gridDir.y);
        
                // Set the respawn position and rotation based on the direction of the first segment
                Quaternion startRot = Quaternion.LookRotation(worldDir);

                SetRespawnPoint(startPos, startRot);

                RespawnCar(false);
            }
        }

        public void RespawnCar(bool isMidGameRespawn)
        {
            if (playerCar == null) return;

            Rigidbody rb = playerCar.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.position = currentRespawnPos;
                rb.rotation = currentRespawnRot;
                playerCar.transform.SetPositionAndRotation(currentRespawnPos, currentRespawnRot);
                
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                playerCar.transform.SetPositionAndRotation(currentRespawnPos, currentRespawnRot);
            }
            Physics.SyncTransforms();

            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                if (rb != null) rb.WakeUp();
                carController.ResetCar();
                
                // No countdown for mid-game respawns, so we need to re-enable input immediately
                if (isMidGameRespawn) 
                {
                    carController.isInputEnabled = true;

                    // Small nudge just in case ground checks freeze
                    if (rb != null) rb.WakeUp();
                    carController.ResetCar();
                }

                // FIX: Force Cinemachine to instantly cut instead of blending from the 5000 unit diorama teleport
                if (Camera.main != null)
                {
                    CinemachineBrain brain = Camera.main.GetComponent<CinemachineBrain>();
                    if (brain != null)
                    {
                        // Temporarily disable blending for this specific jump
                        CinemachineBlendDefinition oldDef = brain.m_DefaultBlend;
                        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0f);

                        // Restore the old blend definition tightly after making the cut
                        StartCoroutine(RestoreCinemachineBlend(brain, oldDef));
                    }
                }

                CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();

                if (vcam != null)
                {
                    vcam.PreviousStateIsValid = false;

                    // Restart Cinemachine
                    vcam.enabled = false;
                    vcam.enabled = true;
                }
                
                CinemachineFreeLook freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
                if (freeLookCamera != null)
                {
                    freeLookCamera.PreviousStateIsValid = false;
                }
            }

            CinemachineVirtualCamera vcam = FindObjectOfType<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.PreviousStateIsValid = false;
                vcam.enabled = false;
                vcam.enabled = true;
            }
        }
        
        /// <summary>
        /// Coroutine to restore the original Cinemachine blend definition after making an instant cut
        /// </summary>
        private IEnumerator RestoreCinemachineBlend(CinemachineBrain brain, CinemachineBlendDefinition oldDef)
        {
            // Wait strictly for two frames so the single "Cut" update propagates completely through LateUpdate
            yield return null;
            yield return null;
            if (brain != null)
            {
                brain.m_DefaultBlend = oldDef;
            }
        }
    }
}
