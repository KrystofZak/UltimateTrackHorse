using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameLogic.Traps
{
    /// <summary>
    /// Replaces tagged placeholder objects in the scene with trap prefabs.
    /// </summary>
    public class TrapSpawner : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private TrapCatalog catalog;
        [SerializeField] private TrapFactory trapFactory;
        [SerializeField] private string placeholderTag = "Obstacle";
        [SerializeField] private bool keepParent = true;
        [SerializeField] private bool keepScale = true;

        [Header("Testing")]
        [SerializeField] private bool enableTestingInputs = true;
        [SerializeField] private GameObject specificTrapPrefab;

        [Header("Testing Inputs")]
        [SerializeField] private KeyCode replaceWithRandomKey = KeyCode.F5;
        [SerializeField] private KeyCode replaceWithFogKey = KeyCode.F6;
        [SerializeField] private KeyCode replaceWithWallKey = KeyCode.F7;
        [SerializeField] private KeyCode replaceWithSurfaceKey = KeyCode.F8;
        [SerializeField] private KeyCode replaceWithSpecificTrapKey = KeyCode.F9;

        private GameObject[] placeholders = Array.Empty<GameObject>();

        private void Update()
        {
            if (!enableTestingInputs)
                return;

            if (Input.GetKeyDown(replaceWithRandomKey))
            {
                ReplaceAllWithRandom();
            }
            else if (Input.GetKeyDown(replaceWithFogKey))
            {
                ReplaceAllWithFog();
            }
            else if (Input.GetKeyDown(replaceWithWallKey))
            {
                ReplaceAllWithWall();
            }
            else if (Input.GetKeyDown(replaceWithSurfaceKey))
            {
                ReplaceAllWithSurface();
            }
            else if (Input.GetKeyDown(replaceWithSpecificTrapKey))
            {
                ReplaceAllWithSpecificTrap();
            }
        }

        /// <summary>
        /// Finds placeholders, shuffles them, and replaces up to <paramref name="count"/> of them with random traps.
        /// </summary>
        /// <param name="count">Maximum number of placeholders to replace.</param>
        public void SpawnNewTraps(int count)
        {
            RefreshPlaceholders();

            count = Mathf.Clamp(count, 0, placeholders.Length);
            if (count == 0)
                return;

            Shuffle(placeholders);

            for (int i = 0; i < count; i++)
            {
                ReplacePlaceholder(placeholders[i], catalog ? catalog.GetRandomAny() : null);
            }
        }

        private void ReplaceAllWithRandom()
        {
            RefreshPlaceholders();

            foreach (var placeholder in placeholders)
            {
                ReplacePlaceholder(placeholder, catalog ? catalog.GetRandomAny() : null);
            }
        }

        private void ReplaceAllWithFog()
        {
            RefreshPlaceholders();

            foreach (var placeholder in placeholders)
            {
                ReplacePlaceholder(placeholder, catalog ? catalog.GetRandomFog() : null);
            }
        }

        private void ReplaceAllWithWall()
        {
            RefreshPlaceholders();

            foreach (var placeholder in placeholders)
            {
                ReplacePlaceholder(placeholder, catalog ? catalog.GetRandomWall() : null);
            }
        }

        private void ReplaceAllWithSurface()
        {
            RefreshPlaceholders();

            foreach (var placeholder in placeholders)
            {
                ReplacePlaceholder(placeholder, catalog ? catalog.GetRandomSurface() : null);
            }
        }

        private void ReplaceAllWithSpecificTrap()
        {
            RefreshPlaceholders();

            if (!specificTrapPrefab)
            {
                Debug.LogWarning("[TrapSpawner] Specific trap prefab is not assigned.");
                return;
            }

            foreach (var placeholder in placeholders)
            {
                ReplacePlaceholder(placeholder, specificTrapPrefab);
            }
        }

        private void RefreshPlaceholders()
        {
            placeholders = GameObject.FindGameObjectsWithTag(placeholderTag) ?? Array.Empty<GameObject>();
        }

        /// <summary>
        /// Replaces a placeholder object with a spawned trap instance.
        /// </summary>
        /// <param name="placeholder">Scene object to replace.</param>
        /// <param name="trapPrefab">Trap prefab to spawn.</param>
        /// <returns>True when replacement succeeded; otherwise false.</returns>
        private bool ReplacePlaceholder(GameObject placeholder, GameObject trapPrefab)
        {
            if (!placeholder)
                return false;

            if (!trapPrefab)
            {
                Debug.LogWarning("[TrapSpawner] Cannot replace placeholder. Trap prefab is null.");
                return false;
            }

            if (!trapFactory)
            {
                Debug.LogError("[TrapSpawner] Cannot replace placeholder. TrapFactory reference is missing.");
                return false;
            }

            if (!trapFactory.Services || !trapFactory.Services.ObstacleManager)
            {
                Debug.LogError("[TrapSpawner] Cannot replace placeholder. ObstacleManager is missing from TrapServices.");
                return false;
            }

            var parent = keepParent ? placeholder.transform.parent : null;

            var spawnRotation = placeholder.transform.rotation * trapPrefab.transform.rotation;
            var spawnPosition = placeholder.transform.position + trapPrefab.transform.position;

            Vector3? scaleToApply = null;
            if (keepScale)
            {
                scaleToApply = keepParent
                    ? placeholder.transform.localScale
                    : placeholder.transform.lossyScale;
            }

            var spawned = trapFactory.SpawnTrap(
                trapPrefab,
                spawnPosition,
                spawnRotation,
                parent,
                scaleToApply);

            if (!spawned)
                return false;

            trapFactory.Services.ObstacleManager.RegisterObstacle(
                trapPrefab,
                spawned.transform.position,
                spawned.transform.rotation,
                spawned.transform.localScale,
                parent,
                spawned);

            Destroy(placeholder);
            return true;
        }

        private static void Shuffle(GameObject[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                int randomIndex = Random.Range(i, array.Length);
                (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
            }
        }
    }
}