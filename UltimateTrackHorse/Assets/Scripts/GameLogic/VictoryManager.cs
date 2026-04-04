using UnityEngine;
using GameLogic.Traps;

namespace GameLogic
{
    public class VictoryManager : MonoBehaviour
    {
        [SerializeField] private string placeholderTag = "Obstacle";
        private int _initialPlaceholderCount;

        public bool AreAllObstaclesPlaced(int activeObstacleCount)
        {
            if (_initialPlaceholderCount == 0) return false;
            
            return activeObstacleCount >= _initialPlaceholderCount;
        }

        public void InitializePlaceholderCount()
        {
            GameObject[] placeholders = GameObject.FindGameObjectsWithTag(placeholderTag);
            _initialPlaceholderCount = placeholders.Length;
            Debug.Log($"[VictoryManager] Initialized. Found {_initialPlaceholderCount} obstacle placeholders.");
        }
    }
}
