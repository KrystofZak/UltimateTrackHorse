using UnityEngine;

namespace GameLogic
{
    public class SpawnObstacle : MonoBehaviour
    {
        [SerializeField] private KeyCode replaceKey = KeyCode.F;
        [SerializeField] private ObstacleLibrary library;

        [Header("What to spawn")]
        [SerializeField] private ObstacleLibrary.ObstacleType obstacleType;
        [SerializeField] private int prefabIndex;

        [Header("Options")]
        [SerializeField] private bool keepParent = true;
        [SerializeField] private bool keepScale = true;

        private bool replaced = false;

        private void Update()
        {
            if (replaced) return;

            if (Input.GetKeyDown(replaceKey))
            {
                ReplaceWithSelectedPrefab();
            }
        }

        public void SpawnNewObstacles(int n)
        {
            if (library == null)
            {
                Debug.LogError("ObstacleLibrary reference is missing.");
                return;
            }

            var cubes = GameObject.FindGameObjectsWithTag("Obstacle");

            if (cubes == null || cubes.Length == 0)
            {
                Debug.LogWarning("No objects with tag 'placeholder' were found.");
                return;
            }

            n = Mathf.Clamp(n, 0, cubes.Length);
            if (n == 0) return;

            // Shuffle that bitch
            for (int i = 0; i < n; i++)
            {
                int randomIndex = Random.Range(i, cubes.Length);
                (cubes[i], cubes[randomIndex]) = (cubes[randomIndex], cubes[i]);
            }

            for (int i = 0; i < n; i++)
            {
                var cube = cubes[i];
                var randomType = library.GetRandomType();
                var prefab = library.GetRandomPrefab(randomType);

                if (prefab == null)
                    continue;

                var parent = keepParent ? cube.transform.parent : null;

                var position = cube.transform.position;
                position.y += prefab.transform.position.y - 0.5f; 

                var spawned = Instantiate(
                    prefab,
                    position,
                    cube.transform.rotation,
                    parent
                );

                Destroy(cube);
            }
        }

        private void ReplaceWithSelectedPrefab()
        {
            if (!library)
            {
                Debug.LogError("ObstacleLibrary reference is missing.");
                return;
            }

            var prefab = library.GetPrefab(obstacleType, prefabIndex);
            if (!prefab) return;

            var parent = keepParent ? transform.parent : null;

            var spawned = Instantiate(prefab, transform.position, transform.rotation, parent);

            if (keepScale)
            {
                spawned.transform.localScale = transform.localScale;
            }

            replaced = true;
            Destroy(gameObject);
        }

        private void ReplaceAll()
        {
            if (library == null)
            {
                Debug.LogError("ObstacleLibrary reference is missing.");
                return;
            }

            var prefab = library.GetPrefab(obstacleType, prefabIndex);
            if (prefab == null) return;

            var cubes = GameObject.FindGameObjectsWithTag("placeholder");

            foreach (var cube in cubes)
            {
                var parent = keepParent ? cube.transform.parent : null;

                var spawned = Instantiate(
                    prefab,
                    cube.transform.position,
                    cube.transform.rotation,
                    parent
                );

                if (keepScale)
                {
                    spawned.transform.localScale = cube.transform.localScale;
                }

                Destroy(cube);
            }
        }
    }
}

