using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameLogic.Traps
{
    /// <summary>
    /// Replaces tagged placeholder objects in the scene with randomly selected trap prefabs.
    /// </summary>
    public class TrapSpawner : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private TrapCatalog catalog;
        [SerializeField] private TrapServices services;
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

        private void ReplaceAllWithRandom()
        {
            RefreshPlaceholders();

            foreach (var cube in placeholders)
            {
                ReplaceObject(cube, catalog.GetRandomAny());
            }
        }

        private void ReplaceAllWithFog()
        {
            RefreshPlaceholders();

            foreach (var cube in placeholders)
            {
                ReplaceObject(cube, catalog.GetRandomFog());
            }
        }

        private void ReplaceAllWithWall()
        {
            RefreshPlaceholders();

            foreach (var cube in placeholders)
            {
                ReplaceObject(cube, catalog.GetRandomWall());
            }
        }

        private void ReplaceAllWithSurface()
        {
            RefreshPlaceholders();

            foreach (var cube in placeholders)
            {
                ReplaceObject(cube, catalog.GetRandomSurface());
            }
        }

        private void ReplaceAllWithSpecificTrap()
        {
            RefreshPlaceholders();

            if (!specificTrapPrefab)
            {
                Debug.LogWarning("[TrapSpawner] Specific trap prefab is not assigned. Check Trap catalog");
                return;
            }

            foreach (var cube in placeholders)
            {
                ReplaceObject(cube, specificTrapPrefab);
            }
        }

        
        /// <summary>
        /// Finds placeholders, shuffles them, and replaces up to <paramref name="count"/> of them with traps.
        /// </summary>
        /// <param name="count">Maximum number of placeholders to replace.</param>
        public void SpawnNewTraps(int count)
        {
            RefreshPlaceholders();

            count = Mathf.Clamp(count, 0, placeholders.Length);
            if (count == 0) return;

            Shuffle(placeholders);

            for (int i = 0; i < count; i++)
            {
                GameObject prefab = catalog.GetRandomAny();
                ReplaceObject(placeholders[i], prefab);
            }
        }

        private void RefreshPlaceholders()
        {
            placeholders = GameObject.FindGameObjectsWithTag(placeholderTag) ?? Array.Empty<GameObject>();
        }

        private bool ReplaceObject(GameObject source, GameObject prefab)
        {
            if (!source || !prefab) return false;

            var parent = keepParent ? source.transform.parent : null;
            var rotation = source.transform.rotation * prefab.transform.rotation;
            var position = source.transform.position + prefab.transform.position;

            GameObject spawned = Instantiate(prefab, position, rotation, parent);

            if (keepScale)
            {
                spawned.transform.localScale = keepParent
                    ? source.transform.localScale
                    : source.transform.lossyScale;
            }

            foreach (var trap in spawned.GetComponentsInChildren<TrapRuntime>(true))
            {
                trap.Initialize(services);
            }

            Destroy(source);
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
