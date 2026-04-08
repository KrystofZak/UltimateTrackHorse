using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameLogic.Traps;

namespace GameLogic
{
    /// <summary>
    /// Tracks active trap instances and restores them when a round is reset.
    /// </summary>
    public class ObstacleManager : MonoBehaviour
    {
        [SerializeField] private TrapFactory trapFactory;

        /// <summary>
        /// Data required to respawn an obstacle exactly as it was originally spawned.
        /// </summary>
        private class ObstacleState
        {
            public GameObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public Transform Parent;
        }

        private readonly List<ObstacleState> initialObstacleStates = new();
        private readonly List<GameObject> activeObstacles = new();

        /// <summary>
        /// Number of currently active tracked obstacles.
        /// Null references are cleaned up before counting.
        /// </summary>
        public int ActiveObstacleCount
        {
            get
            {
                activeObstacles.RemoveAll(item => item == null);
                return activeObstacles.Count;
            }
        }

        /// <summary>
        /// Registers a newly spawned obstacle so it can be tracked and reset later.
        /// </summary>
        /// <param name="prefab">Original prefab used to spawn the obstacle.</param>
        /// <param name="position">World position of the spawned obstacle.</param>
        /// <param name="rotation">World rotation of the spawned obstacle.</param>
        /// <param name="scale">Local scale of the spawned obstacle.</param>
        /// <param name="parent">Parent transform of the spawned obstacle.</param>
        /// <param name="instance">Live instance in the scene.</param>
        public void RegisterObstacle(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Transform parent,
            GameObject instance)
        {
            if (!prefab)
            {
                Debug.LogWarning("[ObstacleManager] Cannot register obstacle. Prefab is null.");
                return;
            }

            if (!instance)
            {
                Debug.LogWarning("[ObstacleManager] Cannot register obstacle. Instance is null.");
                return;
            }

            initialObstacleStates.Add(new ObstacleState
            {
                Prefab = prefab,
                Position = position,
                Rotation = rotation,
                Scale = scale,
                Parent = parent
            });

            activeObstacles.Add(instance);
        }

        /// <summary>
        /// Destroys all tracked obstacle instances and clears both runtime and reset state.
        /// </summary>
        public void ClearAllObstacles()
        {
            DestroyTrackedInstances();
            activeObstacles.Clear();
            initialObstacleStates.Clear();
        }

        /// <summary>
        /// Destroys all currently active tracked obstacles and respawns them
        /// from the originally registered obstacle states.
        /// </summary>
        public void ResetObstacles()
        {
            if (!trapFactory)
            {
                Debug.LogError("[ObstacleManager] Cannot reset obstacles. TrapFactory reference is missing.");
                return;
            }

            DestroyTrackedInstances();
            activeObstacles.Clear();

            foreach (var spawned in from state in initialObstacleStates
                     where state?.Prefab != null
                     select trapFactory.SpawnTrap(
                         state.Prefab,
                         state.Position,
                         state.Rotation,
                         state.Parent,
                         state.Scale) into spawned where spawned != null select spawned)
            {
                activeObstacles.Add(spawned);
            }
        }

        private void DestroyTrackedInstances()
        {
            foreach (var obstacle in activeObstacles)
            {
                if (obstacle != null)
                {
                    Destroy(obstacle);
                }
            }
        }
    }
}