using System.Collections;
using GameLogic.Traps.Definitions;
using UnityEngine;

namespace GameLogic.Traps.Services
{
    /// <summary>
    /// Plays particle systems in camera space so they behave like a 2D screen effect.
    /// </summary>
    public class ScreenParticleService : MonoBehaviour
    {
        [SerializeField] private Transform particleRoot;
        [SerializeField] private string autoRootName = "ScreenParticleRoot";

        private ParticleSystem activeSystem;
        private Coroutine clearRoutine;

        /// <summary>
        /// Plays a new screen particle effect using the supplied profile.
        /// Any currently active effect is cleared first.
        /// </summary>
        public void Play(ScreenParticleProfile profile)
        {
            if (!profile || !profile.particlePrefab)
                return;

            Transform root = ResolveRoot();
            if (!root)
                return;

            if (activeSystem)
            {
                ClearCurrent(profile.interruptFadeOut);
            }

            ParticleSystem instance = Instantiate(profile.particlePrefab, root);
            Transform t = instance.transform;

            t.localPosition = profile.localPosition;
            t.localRotation = Quaternion.Euler(profile.localEulerAngles);
            t.localScale = profile.localScale;

            ApplyTint(instance, profile.tint);

            instance.gameObject.SetActive(true);
            instance.Play(true);

            activeSystem = instance;
            Destroy(instance.gameObject, profile.cleanupDelay);
        }

        /// <summary>
        /// Stops the currently visible screen particle effect.
        /// </summary>
        public void ClearCurrent(float stopDelay = 0.15f)
        {
            if (!activeSystem)
                return;

            if (clearRoutine != null)
                StopCoroutine(clearRoutine);

            ParticleSystem systemToClear = activeSystem;
            activeSystem = null;

            clearRoutine = StartCoroutine(ClearRoutine(systemToClear, stopDelay));
        }

        private IEnumerator ClearRoutine(ParticleSystem systemToClear, float stopDelay)
        {
            if (!systemToClear)
            {
                clearRoutine = null;
                yield break;
            }

            var systems = systemToClear.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                if (ps)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (stopDelay > 0f)
                yield return new WaitForSeconds(stopDelay);

            if (systemToClear)
                Destroy(systemToClear.gameObject, 2f);

            clearRoutine = null;
        }

        private Transform ResolveRoot()
        {
            if (particleRoot)
                return particleRoot;

            Camera cam = Camera.main;
            if (!cam)
                return null;

            Transform existing = cam.transform.Find(autoRootName);
            if (existing)
            {
                particleRoot = existing;
                return particleRoot;
            }

            GameObject root = new GameObject(autoRootName);
            particleRoot = root.transform;
            particleRoot.SetParent(cam.transform, false);
            particleRoot.localPosition = Vector3.zero;
            particleRoot.localRotation = Quaternion.identity;
            particleRoot.localScale = Vector3.one;

            return particleRoot;
        }

        private static void ApplyTint(ParticleSystem root, Color tint)
        {
            var systems = root.GetComponentsInChildren<ParticleSystem>(true);

            foreach (var ps in systems)
            {
                var main = ps.main;
                main.startColor = tint;
            }
        }
    }
}