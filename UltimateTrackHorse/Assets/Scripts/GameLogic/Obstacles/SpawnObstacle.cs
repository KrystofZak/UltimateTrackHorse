using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameLogic.Obstacles
{
    public class SpawnObstacle : MonoBehaviour
    {
        [Header("Input")] [SerializeField] private KeyCode replaceKey = KeyCode.F;
        [SerializeField] private KeyCode resetKey = KeyCode.R;

        [Header("References")] 
        [SerializeField] private ObstacleLibrary library;
        [SerializeField] private ScreenTintController screenTintController;

        [SerializeField] private string placeholderTag = "Obstacle";

        [Header("What to spawn")] 
        [SerializeField] private ObstacleLibrary.ObstacleType obstacleType;
        [SerializeField] private int prefabIndex;

        [Header("Options")] 
        [SerializeField] private bool keepParent = true;
        [SerializeField] private bool keepScale = true;

        private bool replaced;
        private GameObject[] placeholders = Array.Empty<GameObject>();

        private void Update()
        {
            if (replaced) return;

            if (Input.GetKeyDown(replaceKey))
            {
                ReplaceAllWithSelectedPrefab();
            }
            if (Input.GetKeyDown(resetKey))
            {
                ObstacleManager.Instance.ResetObstacles();
            }
        }

        private void RefreshPlaceholders()
        {
            placeholders = GameObject.FindGameObjectsWithTag(placeholderTag);

            if (placeholders != null && placeholders.Length != 0) return;

            Debug.LogWarning($"No objects with tag '{placeholderTag}' were found.");
            placeholders = Array.Empty<GameObject>();
        }

        public void SpawnNewObstacles(int count)
        {
            if (!library)
            {
                Debug.LogError("ObstacleLibrary reference is missing.");
                return;
            }

            RefreshPlaceholders();

            count = Mathf.Clamp(count, 0, placeholders.Length);
            if (count == 0) return;

            Shuffle(placeholders);

            for (var i = 0; i < count; i++)
            {
                var prefab = library.GetRandomPrefab();
                ReplaceObject(placeholders[i], prefab);
            }
        }

        private void ReplaceAllWithSelectedPrefab()
        {
            RefreshPlaceholders();

            foreach (var placeholder in placeholders)
            {
                var prefab = GetSelectedPrefab();
                if (!prefab) return;

                ReplaceObject(placeholder, prefab);
            }

            replaced = true;
        }

        private GameObject GetSelectedPrefab()
        {
            if (library) return library.GetPrefab(obstacleType, prefabIndex);

            Debug.LogError("ObstacleLibrary reference is missing.");
            return null;
        }

        private bool ReplaceObject(GameObject source, GameObject prefab)
        {
            if (!source || !prefab) return false;

            var parent = keepParent ? source.transform.parent : null;

            var rotation = source.transform.rotation * prefab.transform.rotation;
            var position = source.transform.position + prefab.transform.position;

            var spawned = Instantiate(prefab, position, rotation, parent);

            if (keepScale)
            {
                spawned.transform.localScale = keepParent
                    ? source.transform.localScale
                    : source.transform.lossyScale;
            }

            InjectServices(spawned);

            ObstacleManager.Instance.RegisterObstacle(prefab, spawned.transform.position, spawned.transform.rotation, spawned.transform.localScale, parent, spawned);

            Destroy(source);
            return true;
        }

        private void InjectServices(GameObject spawned)
        {
            if (!screenTintController) return;

            foreach (var injectable in spawned.GetComponentsInChildren<IScreenTintInjectable>(true))
            {
                injectable.InjectScreenTint(screenTintController);
            }
        }

        private static void Shuffle(GameObject[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                var randomIndex = Random.Range(i, array.Length);
                (array[i], array[randomIndex]) = (array[randomIndex], array[i]);
            }
        }
    }
}