using UnityEngine;

public class SpawnObstacle : MonoBehaviour
{
    [SerializeField] private KeyCode replaceKey = KeyCode.F;
    [SerializeField] private ObstacleLibrary library;

    [Header("What to spawn")]
    [SerializeField] private ObstacleLibrary.ObstacleType obstacleType;
    [SerializeField] private int prefabIndex = 0;

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

        // TODO: tady pak místo všech vybrat jen pár
        foreach (var cube in cubes)
        {
            var spawned = Instantiate(
                prefab,
                cube.transform.position,
                cube.transform.rotation,
                cube.transform.parent
            );

            spawned.transform.localScale = cube.transform.localScale;
            Destroy(cube);
        }
    }
}