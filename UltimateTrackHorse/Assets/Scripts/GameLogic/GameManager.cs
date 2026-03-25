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

        [SerializeField] private SpawnObstacle spawnObstacle;
        [SerializeField] private UIManager uiManager;

        /// <summary>
        /// Subscribe to the finish line event when the game manager is enabled, and unsubscribe when disabled.
        /// </summary>
        void OnEnable() 
        { 
            FinishLine.OnPlayerFinished += ResetToStart; 
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
            if (uiManager.obstacleChoiceView.active)
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
            CalculateTotalTimeComplexity();
            
            timer = FindFirstObjectByType<Timer>();
            if (timer != null)
            {
                timer.SetStartTime(totalTimeComplexity);
                timer.OnTimeUp += HandleTimeUp;
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
            uiManager.obstacleChoiceView.SetActive(true);
            timer.StopTimer();
            lapCount++;

            Debug.Log("Completed laps: " + lapCount);
            PlaceCarOnStart();
            ObstacleManager.Instance.ResetObstacles();
            Debug.Log("Lap time: " + timer.timeElapsed);
            
        }
        
        /// <summary>
        /// Place the player's car at the starting position (1,1) on the map,
        /// with the correct rotation based on the tile variant.
        /// </summary>
        public void PlaceCarOnStart()
        {
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
                }

                CinemachineVirtualCamera vcam = FindFirstObjectByType<CinemachineVirtualCamera>();

                if (vcam != null)
                {
                    vcam.PreviousStateIsValid = false;

                    // Restart Cinemachine
                    vcam.enabled = false;
                    vcam.enabled = true;
                }
            }
        }
        
    }
    
    
    
}
