using System;
using System.Collections.Generic;
using System.Collections; // Added for Coroutines
using UnityEngine;
using Cinemachine;
using MapGeneration;
using UI;
using GameLogic.Ghost;
using GameLogic.Traps.Core;
using GameLogic.Network;

namespace GameLogic
{
    /// <summary>
    /// Class that manages the game logic and interactions between the player and the map.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public GameObject playerCar;
        private GameObject currentInstantiatedPrefab;
        [Header("Car Selection")]
        [Tooltip("Add the Car Prefabs here (e.g. 0: Monster, 1: F1, 2: Muscle)")]
        public GameObject[] carPrefabs;

        [Header("Component References")]
        [SerializeField] private TrapSpawner trapSpawner;
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private ObstacleManager obstacleManager;
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private GhostSystem ghostSystem;

        public event Action<int> OnCountdownTick;
        public event Action OnCountdownGo;
        public event Action OnRaceStarted;
        public event Action OnLapFinished;
        public event Action OnVictory;
        public event Action OnDefeat;

        // Replaced old UIManager with the new UIController
        private UIController uiController;

        private int lapCount;
        private float totalTimeComplexity;
        private Timer timer;
        private bool hasRaceStartedThisSession = false; // Temp fix for Game soundtrack

        public int totalCheckpoints;
        public int crossedCheckpoints;

        // List to hold the history of lap completion times for the current track session
        private List<float> currentMapLapTimes = new List<float>();

        // Variables to hold the current respawn position and rotation, set by checkpoints
        private Vector3 currentRespawnPos;
        private Quaternion currentRespawnRot;

        // Track whether time has expired to prevent respawning
        private bool hasTimeExpired = false;
        public bool isGameActive = false;

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
            if (Input.GetKeyDown(KeyCode.R) && !hasTimeExpired)
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

            if (isGameActive)
            {
                CheckForLoss();
            }
        }

        private Coroutine countdownCoroutine;

        public void SetPlayerCarPrefab(int index)
{
    if (carPrefabs != null && carPrefabs.Length > index && carPrefabs[index] != null)
    {
       
        if (currentInstantiatedPrefab != null)
        {
            Destroy(currentInstantiatedPrefab);
        }
        else if (playerCar != null)
        {
            Destroy(playerCar.transform.root.gameObject);
        }
        currentInstantiatedPrefab = Instantiate(carPrefabs[index]);
        CarController carComponent = currentInstantiatedPrefab.GetComponentInChildren<CarController>();
        
        if (carComponent != null)
        {
            
            playerCar = carComponent.gameObject;
        }
        else
        {
            Debug.LogWarning("CarController nebyl nalezen v potomcích prefabu! Hra se pokusí použít root objekt.");
            playerCar = currentInstantiatedPrefab;
        }

        CinemachineVirtualCamera[] vcams = FindObjectsOfType<CinemachineVirtualCamera>(true);
        foreach(var vcam in vcams)
        {
            if(vcam.Name != "MenuCamera")
            {
                
                vcam.Follow = playerCar.transform;
                vcam.LookAt = playerCar.transform;
            }
        }
        
        CinemachineFreeLook freeLook = FindObjectOfType<CinemachineFreeLook>(true);
        if(freeLook != null)
        {
            freeLook.Follow = playerCar.transform;
            freeLook.LookAt = playerCar.transform;
        }

        Spedometer speedScript = FindObjectOfType<Spedometer>(true);
        if(speedScript != null)
        {
            speedScript.car = playerCar.GetComponent<Rigidbody>();
        }
        
        GhostLapRecorder ghostRecorder = FindObjectOfType<GhostLapRecorder>(true);
        if (ghostRecorder != null)
        {
            ghostRecorder.SetPlayerTransform(playerCar.transform);
        }
        if (gameStateManager != null)
        {
            gameStateManager.SetPlayerRigidbody(playerCar.GetComponent<Rigidbody>());
        }

    }
    else
    {
        Debug.LogWarning("Car Prefab missing or index out of bounds! Returning to default car if any.");
    }
}
        public void RestartCurrentLap()
        {
            Debug.Log("Restarting lap...");
            PlaceCarOnStart();
            obstacleManager.ResetObstacles();
            crossedCheckpoints = 0;
            Checkpoint[] checkpoints = FindObjectsOfType<Checkpoint>();
            foreach (var cp in checkpoints) cp.ResetCheckpoint();

            if (timer != null)
            {
                timer.ResetTimer();
                timer.SetStartTime(totalTimeComplexity, false); // Don't tick down yet!
            }

            // Commence the 3.. 2.. 1.. Sequence
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(RaceCountdownCoroutine());
        }

        public void DestroyTrack()
        {

        }

        public void SetupNewTrack()
        {
            // Fully wipe the history list and hide the panel when generating a brand new track
            currentMapLapTimes.Clear();
            hasTimeExpired = false;
            hasRaceStartedThisSession = false;

            if (uiController != null)
            {
                uiController.HideLapHistory();
            }

            lapCount = 0; // Reset lap count

            if (ghostSystem && mapGenerator)
            {
                ghostSystem.SetMapId(mapGenerator.LastUsedSeed.ToString()); // unique ID for a map
            }

            StartCoroutine(InitializeGameStateDelayed());

            CalculateTotalTimeComplexity();

            timer = FindObjectOfType<Timer>();

            if (timer != null)
            {
                timer.ResetIncrement();
                timer.SetStartTime(totalTimeComplexity, false);

                // FIX 2: Vždy se nejprve odhlásíme, abychom zabránili hromadění eventů
                timer.OnTimeUp -= HandleTimeUp;
                timer.OnTimeUp += HandleTimeUp;
            }
            else
            {
                Debug.LogWarning("GameManager: Timer not found in the scene! Ensure a Timer component exists.");
            }

            Time.timeScale = 1f;
            isGameActive = true;

            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(RaceCountdownCoroutine());
        }


        private IEnumerator InitializeGameStateDelayed()
        {
            // Wait for one frame to ensure the game state manager is fully initialized and the new map is generated before counting placeholders
            yield return new WaitForEndOfFrame();

            if (gameStateManager != null)
            {
                gameStateManager.InitializePlaceholderCount();
            }

            totalCheckpoints = 0;
            if (mapGenerator != null && mapGenerator.GeneratedPath != null && mapGenerator.targetTrackLength > 5)
            {
                for (int i = 1; i < mapGenerator.GeneratedPath.Count - 1; i++)
                {
                    if (i % 6 == 0)
                    {
                        totalCheckpoints++;
                    }
                }
            }

            crossedCheckpoints = 0;
            Debug.Log($"Total checkpoints on map: {totalCheckpoints}");
        }

        private void HandleTimeUp()
        {
            hasTimeExpired = true;
            Debug.Log("Time is up! Disabling car controls.");

            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.isInputEnabled = false;
            }
        }

        private void CheckForLoss()
        {
            if (gameStateManager != null && gameStateManager.IsLost(hasTimeExpired))
            {
                HandleLoss();
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
            hasTimeExpired = false;
            isGameActive = true;

            if (timer != null)
            {
                timer.ResetTimer();
                timer.SetStartTime(totalTimeComplexity, false); // Don't tick down yet!
            }

            // Commence the 3.. 2.. 1.. Sequence
            if (countdownCoroutine != null) StopCoroutine(countdownCoroutine);
            countdownCoroutine = StartCoroutine(RaceCountdownCoroutine());
        }

        public void StopRaceCountdown()
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
                countdownCoroutine = null;
            }
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
                OnCountdownTick?.Invoke(i);
                yield return new WaitForSecondsRealtime(1f);
            }

            // "GO!" state
            if (uiController != null)
            {
                uiController.UpdateCountdownText("GO!");
            }

            OnCountdownGo?.Invoke();

            // Release the physical brakes
            if (carController != null) carController.isInputEnabled = true;

            // Begin counting down the real time clock!
            if (timer != null)
            {
                timer.StartTimer();
            }

            // Begin recording lap for a ghost car
            if (ghostSystem != null)
            {
                ghostSystem.StartLap();
            }

            if (!hasRaceStartedThisSession)
            {
                hasRaceStartedThisSession = true;
                OnRaceStarted?.Invoke();
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
            if (!isGameActive) return;

            // If the player crosses the finish line after time has expired,
            // it's a valid lap completion, not a loss.
            if (hasTimeExpired)
            {
                Debug.Log("Crossed finish line after time was up. Lap counts!");
                // The hasTimeExpired flag will be reset in OnChoiceClicked or SetupNewTrack.
                hasTimeExpired = false; // Reset the flag
            }

            if (gameStateManager == null)
            {
                Debug.LogError("[GameManager] Victory check failed: gameStateManager is NULL.");
            }
            else if (obstacleManager == null)
            {
                Debug.LogError("[GameManager] Victory check failed: obstacleManager is NULL.");
            }
            else
            {
                Debug.Log(
                    $"[GameManager] Victory check | RegisteredObstacleCount={obstacleManager.RegisteredObstacleCount} | ActiveObstacleCount={obstacleManager.ActiveObstacleCount}");
                OnLapFinished?.Invoke();
            }

            if (gameStateManager != null && obstacleManager != null)
            {
                bool allPlaced = gameStateManager.AreAllObstaclesPlaced(obstacleManager.RegisteredObstacleCount);

                Debug.Log($"[GameManager] AreAllObstaclesPlaced returned: {allPlaced}");

                if (allPlaced)
                {
                    Debug.Log("[GameManager] Calling HandleVictory().");
                    HandleVictory();
                    return;
                }
            }

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

                // Finish recording data for ghost car
                if (ghostSystem) ghostSystem.FinishLap(timer.timeElapsed);
            }

            // Immediately send the updated history list to the UI Controller to be drawn
            if (uiController != null)
            {
                uiController.UpdateLapHistoryUI(currentMapLapTimes);
            }

            lapCount++;
            Debug.Log("Completed laps: " + lapCount);
            PlaceCarOnStart();
            obstacleManager.ResetObstacles();
            crossedCheckpoints = 0;
        }

        public void CheckpointCrossed()
        {
            crossedCheckpoints++;
            Debug.Log($"Checkpoint crossed! ({crossedCheckpoints}/{totalCheckpoints})");
        }

        public bool CanFinishLap()
        {
            return crossedCheckpoints >= totalCheckpoints;
        }

        private void HandleVictory()
        {
            if (!isGameActive) return;
            isGameActive = false;

            OnVictory?.Invoke();
            Debug.Log("Victory! All obstacles placed and lap finished!");
            if (uiController != null)
            {
                float bestLap = float.MaxValue;
                foreach (float t in currentMapLapTimes) if (t < bestLap) bestLap = t;
                if (bestLap == float.MaxValue) bestLap = 0f;

                int obsCount = obstacleManager != null ? obstacleManager.RegisteredObstacleCount : 0;
                uiController.UpdatePostGameStats(true, bestLap, obsCount, lapCount);
                uiController.ShowVictoryView();
                DiscordManager discordManager = FindObjectOfType<DiscordManager>();
                DatabaseManager databaseManager = FindObjectOfType<DatabaseManager>();
                if (discordManager != null && databaseManager != null && DiscordManager.IsLinked)
                {
                    string seed = mapGenerator.LastUsedSeed.ToString();
                    databaseManager.SendGameResult(seed, lapCount, bestLap);
                    Debug.Log($"Sent game result to database: Seed={seed}, Laps={lapCount}, BestLap={bestLap}");

                }
            }

            if (timer != null)
            {
                timer.StopTimer();
            }

            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.isInputEnabled = false;
            }
        }

        private void HandleLoss()
        {
            if (!isGameActive) return;
            isGameActive = false;

            OnDefeat?.Invoke();
            Debug.Log("Loss! The car has stopped after time ran out.");
            if (uiController != null)
            {
                float bestLap = float.MaxValue;
                foreach (float t in currentMapLapTimes) if (t < bestLap) bestLap = t;
                if (bestLap == float.MaxValue) bestLap = 0f;

                int obsCount = obstacleManager != null ? obstacleManager.ActiveObstacleCount : 0;
                uiController.UpdatePostGameStats(false, bestLap, obsCount);
                uiController.ShowDefeatView();
            }

            CarController carController = playerCar.GetComponent<CarController>();
            if (carController != null)
            {
                carController.isInputEnabled = false;
            }

            // To be absolutely sure, stop the timer again.
            if (timer != null)
            {
                timer.StopTimer();
            }
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

                CinemachineVirtualCamera[] vcams = FindObjectsOfType<CinemachineVirtualCamera>(true);
                foreach (var vcam in vcams)
                {
                    if (vcam != null)
                    {
                        vcam.PreviousStateIsValid = false;

                        // Restart Cinemachine
                        vcam.enabled = false;
                        vcam.enabled = true;
                    }
                }

                CinemachineFreeLook freeLookCamera = FindObjectOfType<CinemachineFreeLook>(true);
                if (freeLookCamera != null)
                {
                    freeLookCamera.PreviousStateIsValid = false;
                }
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
