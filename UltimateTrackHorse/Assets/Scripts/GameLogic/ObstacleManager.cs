using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class ObstacleManager : MonoBehaviour
    {
        private class ObstacleState
        {
            public GameObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Transform Parent;
        }

        private readonly List<ObstacleState> _initialObstacleStates = new List<ObstacleState>();
        private readonly List<GameObject> _activeObstacles = new List<GameObject>();

        public int ActiveObstacleCount
        {
            get
            {
                // Clean up list from any null references that might occur if an obstacle is destroyed by other means.
                _activeObstacles.RemoveAll(item => item == null);
                return _activeObstacles.Count;
            }
        }

        public void RegisterObstacle(GameObject prefab, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent, GameObject instance)
        {
            var obstacleState = new ObstacleState
            {
                Prefab = prefab,
                Position = position,
                Rotation = rotation,
                Scale = scale,
                Parent = parent
            };
            _initialObstacleStates.Add(obstacleState);
            _activeObstacles.Add(instance);
        }

        public void ClearAllObstacles()
        {
            foreach (var obstacle in _activeObstacles)
            {
                if (obstacle != null)
                {
                    Destroy(obstacle);
                }
            }
            _activeObstacles.Clear();
            _initialObstacleStates.Clear();
        }

        public void ResetObstacles()
        {
            foreach (var obstacle in _activeObstacles)
            {
                if (obstacle != null)
                {
                    Destroy(obstacle);
                }
            }
            _activeObstacles.Clear();

            // Find the ScreenTintController once OUTSIDE the loop for much better performance
            var gmObject = GameObject.Find("GameManager");

            foreach (var state in _initialObstacleStates)
            {
                var spawned = Instantiate(state.Prefab, state.Position, state.Rotation, state.Parent);
                spawned.transform.localScale = state.Scale;
                _activeObstacles.Add(spawned);
            }
        }
    }
}
