using UnityEngine;

public class ObstacleLibrary : MonoBehaviour
{
    public enum ObstacleType
    {
        Wall,
        Fog,
        Surface
    }

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

    public GameObject GetPrefab(ObstacleType type, int index)
    {
        var sourceArray = type switch
        {
            ObstacleType.Wall => walls,
            ObstacleType.Fog => fogs,
            ObstacleType.Surface => surfaces,
            _ => null
        };

        if (sourceArray == null || sourceArray.Length == 0)
        {
            Debug.LogError($"{type} array is empty. Make sure prefabs were loaded.");
            return null;
        }

        if (index >= 0 && index < sourceArray.Length) return sourceArray[index];
        
        Debug.LogError($"{type} index {index} is out of range. Valid range: 0 to {sourceArray.Length - 1}");
        return null;

    }
}