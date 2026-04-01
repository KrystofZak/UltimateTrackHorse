using UnityEngine;

namespace GameLogic.Obstacles
{
    public class BarrelGroup : MonoBehaviour
    {
        public GameObject effectPrefab;
        public Transform effectSpawnPoint;
        public float t = 0.5f;

        private bool hasTriggered = false;

        public void TriggerEffect()
        {
            if(hasTriggered) return;
            
            hasTriggered = true;
            
            // If there is custom position for effect
            var spawnPos = effectSpawnPoint != null
                ? effectSpawnPoint.position
                : transform.position;

            // Spawn effect
            Instantiate(effectPrefab, spawnPos, Quaternion.identity);
            
            // Destroy barrels
            Destroy(gameObject, t);
        }
    }
}