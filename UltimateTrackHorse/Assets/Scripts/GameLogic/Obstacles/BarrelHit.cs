using UnityEngine;

namespace GameLogic.Obstacles
{
    public class BarrelHit : MonoBehaviour
    {
        public string playerTag = "Player";
        private BarrelGroup barrelGroup;
        
        private void Start()
        {
            barrelGroup = GetComponentInParent<BarrelGroup>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag(playerTag))
            {
                barrelGroup.TriggerEffect();
            }
        }
    }
}