using UnityEngine;

namespace GameLogic.Traps
{
    /// <summary>
    /// Trap effect that spawns a prefab relative to the activated trap.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Effects/Spawn Prefab")]
    public class SpawnPrefabEffectDefinition : TrapEffectDefinition
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 localOffset;
        [SerializeField] private bool useTrapRotation = true;

        /// <summary>
        /// Instantiates the configured prefab at the trap position plus the configured local offset.
        /// </summary>
        /// <param name="context">Execution data for the trap activation.</param>
        public override void Execute(TrapExecutionContext context)
        {
            if (!prefab) return;

            var trapTransform = context.Trap.transform;
            
            var position = trapTransform.position + trapTransform.rotation * localOffset;
            var rotation = useTrapRotation
                ? context.Trap.transform.rotation
                : Quaternion.identity;
            Instantiate(prefab, position, rotation);
        }
    }
}
