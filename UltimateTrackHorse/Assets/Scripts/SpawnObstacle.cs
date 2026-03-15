using UnityEngine;

public class SpawnObstacle : MonoBehaviour
{
    // připravit rozhraní pro Obstacles, možná 
    // GameObject.FindGameObjectsWithTag -> get all cubes
    [SerializeField] private KeyCode replaceKey = KeyCode.F;
    [SerializeField] private ObstacleLibrary library;
    [SerializeField] private int wallIndex = 0;

    private bool replaced = false;

    private void Update()
    {
        if (replaced) return;

        if (Input.GetKeyDown(replaceKey))
        {
            ReplaceWithWall();
        }
    }

    private void ReplaceWithWall()
    {
        if (library == null)
        {
            Debug.LogError("ObstacleLibrary reference is missing.");
            return;
        }

        if (library.walls == null || library.walls.Length == 0)
        {
            Debug.LogError("Walls array is empty. Make sure prefabs were loaded.");
            return;
        }

        if (wallIndex < 0 || wallIndex >= library.walls.Length)
        {
            Debug.LogError("wallIndex is out of range.");
            return;
        }

        GameObject prefab = library.walls[wallIndex];
        GameObject spawned = Instantiate(prefab, transform.position, transform.rotation);
        // GameObject spawned = Instantiate(prefab, transform.position, transform.rotation * prefab.transform.rotation);
        // pokud by bylo potřeba sčítat rotace prefabu a placeholderu => násobení quaternionu

        replaced = true;
        Destroy(gameObject);
    }
}