using UnityEngine;
using Cinemachine;
using MapGeneration;

namespace GameLogic
{

    /// <summary>
    /// Class that manages the game logic and interactions between the player and the map.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public GameObject playerCar;
        public MapGenerator mapGenerator;
        private int lapCount = 0;

        [SerializeField] private SpawnObstacle spawnObstacle;

        /// <summary>
        /// Subscribe to the finish line event when the game manager is enabled, and unsubscribe when disabled.
        /// </summary>
        void OnEnable() { FinishLine.OnPlayerFinished += ResetToStart; }
        void OnDisable() { FinishLine.OnPlayerFinished -= ResetToStart; }

        /// <summary>
        /// Handles the logic when the player finishes the round.
        /// Place the player's car at the starting position (1,1) on the map.
        /// </summary>
        private void ResetToStart()
        {
            lapCount++;
            Debug.Log("Completed laps: " + lapCount);
            PlaceCarOnStart();
            spawnObstacle.SpawnNewObstacles(1); 
            
            Timer timer = FindObjectOfType<Timer>();

            Debug.Log("Lap time: " + timer.timeElapsed);

            if (timer != null)
            {
                timer.ResetTimer();
            }

            
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
                    // Vypneme fyziku, teleportujeme a zase zapneme
                    rb.isKinematic = true; 
                    rb.position = startPos;
                    rb.rotation = startRot;
            
                    // Důležité: Resetujeme transformaci i skrze Rigidbody
                    playerCar.transform.SetPositionAndRotation(startPos, startRot);
            
                    rb.isKinematic = false; 
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                else
                {
                    playerCar.transform.SetPositionAndRotation(startPos, startRot);
                }

            }
        }
        
    }
    
    
    
}
