using UnityEngine;

namespace GameLogic.Traps
{
    /// <summary>
    /// Creates fully initialized trap instances.
    /// This is the single authoritative place for trap instantiation.
    /// </summary>
    public class TrapFactory : MonoBehaviour
    {
        [SerializeField] private TrapServices services;

        /// <summary>
        /// Shared trap services injected into all spawned trap runtimes.
        /// </summary>
        public TrapServices Services => services;

        /// <summary>
        /// Spawns a trap prefab and injects all TrapRuntime components found in the instance.
        /// </summary>
        /// <param name="prefab">Trap prefab to spawn.</param>
        /// <param name="position">World position to spawn at.</param>
        /// <param name="rotation">World rotation to spawn with.</param>
        /// <param name="parent">Optional parent transform.</param>
        /// <param name="localScale">
        /// Optional local scale to apply after spawn.
        /// Pass null to keep the prefab's original scale.
        /// </param>
        /// <returns>The spawned trap instance, or null if creation failed.</returns>
        public GameObject SpawnTrap(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation,
            Transform parent = null,
            Vector3? localScale = null)
        {
            if (!prefab)
            {
                Debug.LogWarning("[TrapFactory] Cannot spawn trap. Prefab is null.");
                return null;
            }

            if (!services)
            {
                Debug.LogError("[TrapFactory] Cannot spawn trap. TrapServices reference is missing.");
                return null;
            }

            var spawned = Instantiate(prefab, position, rotation, parent);

            if (localScale.HasValue)
            {
                spawned.transform.localScale = localScale.Value;
            }

            InjectServices(spawned);
            return spawned;
        }

        /// <summary>
        /// Injects shared services into all TrapRuntime components in the supplied hierarchy.
        /// </summary>
        /// <param name="instance">Root object whose trap runtimes should be initialized.</param>
        public void InjectServices(GameObject instance)
        {
            if (!instance)
            {
                Debug.LogWarning("[TrapFactory] Cannot inject services. Instance is null.");
                return;
            }

            if (!services)
            {
                Debug.LogError("[TrapFactory] Cannot inject services. TrapServices reference is missing.");
                return;
            }

            var runtimes = instance.GetComponentsInChildren<TrapRuntime>(true);

            foreach (var runtime in runtimes)
            {
                runtime.Initialize(services);
            }
        }
    }
}