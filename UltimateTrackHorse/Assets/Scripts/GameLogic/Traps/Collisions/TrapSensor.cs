using UnityEngine;

namespace GameLogic.Traps
{
    /// <summary>
    /// Forwards collision and trigger callbacks to the owning <see cref="TrapRuntime"/>.
    /// </summary>
    public class TrapSensor : MonoBehaviour
    {
        [SerializeField] private TrapRuntime trap;

        private void Reset()
        {
            trap = GetComponentInParent<TrapRuntime>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            trap?.NotifyCollision(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            trap?.NotifyTrigger(other);
        }
    }
}
