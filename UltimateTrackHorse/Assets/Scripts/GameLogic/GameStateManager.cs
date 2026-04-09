using UnityEngine;

namespace GameLogic
{
    public class GameStateManager : MonoBehaviour
    {
        [Header("Victory Conditions")]
        [SerializeField] private string placeholderTag = "Obstacle";
        
        [Header("Loss Conditions")]
        [SerializeField] private Rigidbody playerCarRigidbody;
        [SerializeField] private float stoppedVelocityThreshold = 0.5f;

        private int _initialPlaceholderCount;

        public bool AreAllObstaclesPlaced(int registeredObstacleCount)
        {
            if (_initialPlaceholderCount == 0) return false;
            
            return registeredObstacleCount >= _initialPlaceholderCount;
        }

        public bool IsLost(bool isTimeUp)
        {
            if (!isTimeUp) return false;

            if (playerCarRigidbody != null)
            {
                return playerCarRigidbody.linearVelocity.magnitude < stoppedVelocityThreshold;
            }

            return false;
        }

        public void InitializePlaceholderCount()
        {
            GameObject[] placeholders = GameObject.FindGameObjectsWithTag(placeholderTag);
            _initialPlaceholderCount = placeholders.Length;
            Debug.Log($"[GameStateManager] Initialized. Found {_initialPlaceholderCount} obstacle placeholders.");
        }
    }
}
