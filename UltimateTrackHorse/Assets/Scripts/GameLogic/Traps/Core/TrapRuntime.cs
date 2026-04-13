using GameLogic.Traps.Collisions;
using GameLogic.Traps.Definitions;
using UnityEngine;

namespace GameLogic.Traps.Core
{
    /// <summary>
    /// Runtime component responsible for validating and executing a trap definition.
    /// </summary>
    public class TrapRuntime : MonoBehaviour
    {
        [SerializeField] private TrapDefinition definition;
        [SerializeField] private string playerTag = "Player";

        private bool consumed;
        private float nextAllowedTime;

        /// <summary>
        /// Definition asset that controls this runtime's behavior.
        /// </summary>
        public TrapDefinition Definition => definition;

        /// <summary>
        /// Shared services available to trap effects.
        /// </summary>
        public TrapServices Services { get; private set; }

        /// <summary>
        /// Injects the shared services used by this trap instance.
        /// </summary>
        /// <param name="services">Service container for trap-related systems.</param>
        public void Initialize(TrapServices services)
        {
            Services = services;
        }

        /// <summary>
        /// Notifies the trap that a collision occurred.
        /// Only collisions from objects with the configured player tag are processed.
        /// </summary>
        /// <param name="collision">Collision data provided by Unity.</param>
        public void NotifyCollision(Collision collision)
        {
            if (collision.gameObject.CompareTag(playerTag))
            {
                Activate(collision.gameObject, collision, null);
            }
        }

        /// <summary>
        /// Notifies the trap that a trigger enter occurred.
        /// Only trigger entries from objects with the configured player tag are processed.
        /// </summary>
        /// <param name="other">Collider that entered the trigger.</param>
        public void NotifyTrigger(Collider other)
        {
            if (other.gameObject.CompareTag(playerTag))
            {
                Activate(other.gameObject, null, other);
            }
        }

        /// <summary>
        /// Marks the trap as consumed and destroys its game object.
        /// </summary>
        /// <param name="delay">Optional delay in seconds before destruction.</param>
        public void ConsumeAndDestroy(float delay = 0f)
        {
            consumed = true;

            if (delay <= 0f)
                Destroy(gameObject);
            else
                Destroy(gameObject, delay);
        }

        private void Activate(GameObject instigator, Collision collision, Collider trigger)
        {
            if (!definition || !Services) return;
            if (consumed) return;

            if (definition.activationMode == TrapActivationMode.Repeatable)
            {
                if (Time.time < nextAllowedTime) return;
                nextAllowedTime = Time.time + definition.repeatCooldown;
            }
            else
            {
                consumed = true;
            }

            var context = new TrapExecutionContext(this, instigator, collision, trigger);

            if (definition.effects == null) return;

            foreach (var effect in definition.effects)
            {
                if (!effect) continue;
                effect.Execute(context);
            }
        }
    }
}
