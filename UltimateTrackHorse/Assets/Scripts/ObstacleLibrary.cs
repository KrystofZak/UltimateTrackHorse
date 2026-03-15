using UnityEngine;

public class ObstacleLibrary : MonoBehaviour
{
    [Header("Loaded prefabs")]
    public GameObject[] walls;
    public GameObject[] fogs;
    public GameObject[] surfaces;

    private void Awake()
    {
        LoadAllPrefabs();
    }

    [ContextMenu("Load All Prefabs")]
    public void LoadAllPrefabs()
    {
        walls = Resources.LoadAll<GameObject>("Obstacles/Walls");
        fogs = Resources.LoadAll<GameObject>("Obstacles/Fogs");
        surfaces = Resources.LoadAll<GameObject>("Obstacles/Surfaces");

        Debug.Log($"Loaded {walls.Length} wall prefabs");
        Debug.Log($"Loaded {fogs.Length} fog prefabs");
        Debug.Log($"Loaded {surfaces.Length} surface prefabs");
    }
}