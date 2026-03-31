using UnityEngine;

namespace GameLogic.Traps
{
    /// <summary>
    /// Asset containing categorized trap prefab pools and helper methods for random selection.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Trap Catalog")]
    public class TrapCatalog : ScriptableObject
    {
        /// <summary>
        /// Prefabs intended to be spawned as wall traps.
        /// </summary>
        public GameObject[] wallPrefabs;

        /// <summary>
        /// Prefabs intended to be spawned as fog traps.
        /// </summary>
        public GameObject[] fogPrefabs;

        /// <summary>
        /// Prefabs intended to be spawned as surface traps.
        /// </summary>
        public GameObject[] surfacePrefabs;

        /// <summary>
        /// Returns a random wall trap prefab.
        /// </summary>
        /// <returns>A randomly selected wall prefab.</returns>
        public GameObject GetRandomWall() =>
            wallPrefabs[Random.Range(0, wallPrefabs.Length)];

        /// <summary>
        /// Returns a random fog trap prefab.
        /// </summary>
        /// <returns>A randomly selected fog prefab.</returns>
        public GameObject GetRandomFog() =>
            fogPrefabs[Random.Range(0, fogPrefabs.Length)];

        /// <summary>
        /// Returns a random surface trap prefab.
        /// </summary>
        /// <returns>A randomly selected surface prefab.</returns>
        public GameObject GetRandomSurface() =>
            surfacePrefabs[Random.Range(0, surfacePrefabs.Length)];

        /// <summary>
        /// Returns a random prefab from all configured trap categories.
        /// </summary>
        /// <returns>
        /// A randomly selected prefab, or <see langword="null"/> when the catalog is empty.
        /// </returns>
        public GameObject GetRandomAny()
        {
            int total = wallPrefabs.Length + fogPrefabs.Length + surfacePrefabs.Length;
            if (total == 0) return null;

            int index = Random.Range(0, total);

            if (index < wallPrefabs.Length) return wallPrefabs[index];
            index -= wallPrefabs.Length;

            if (index < fogPrefabs.Length) return fogPrefabs[index];
            index -= fogPrefabs.Length;

            return surfacePrefabs[index];
        }
    }
}
