using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameLogic.Traps.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Traps
{
    /// <summary>
    /// Manages temporary full-screen overlay splats used by trap effects.
    /// </summary>
    public class ScreenOverlayService : MonoBehaviour
    {
        [SerializeField] private RectTransform overlayRoot;
        [SerializeField] private Image imagePrefab;

        private readonly List<Image> activeImages = new();
        private Coroutine activeRoutine;

        /// <summary>
        /// Plays a new overlay sequence using the supplied profile.
        /// Any currently playing overlay is stopped and cleared first.
        /// </summary>
        /// <param name="profile">Visual and timing configuration for the overlay.</param>
        public void Play(ScreenOverlayProfile profile)
        {
            if (!profile || !overlayRoot || !imagePrefab) return;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            ClearImmediate();
            activeRoutine = StartCoroutine(PlayRoutine(profile));
        }

        /// <summary>
        /// Wipes the currently visible overlay using the default duration.
        /// </summary>
        public void WipeCurrent()
        {
            if (activeImages.Count == 0) return;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            StartCoroutine(WipeRoutine(0.25f));
        }

        /// <summary>
        /// Wipes the currently visible overlay over a custom duration.
        /// </summary>
        /// <param name="duration">Time in seconds used to fade the overlay away.</param>
        public void WipeCurrent(float duration)
        {
            if (activeImages.Count == 0) return;

            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            StartCoroutine(WipeRoutine(duration));
        }

        private IEnumerator PlayRoutine(ScreenOverlayProfile profile)
        {
            SpawnOverlay(profile);

            yield return FadeAll(0f, profile.alpha, profile.fadeIn);
            yield return new WaitForSeconds(profile.hold);
            yield return FadeAll(profile.alpha, 0f, profile.fadeOut);

            if (profile.layoutMode != ScreenOverlayLayoutMode.RandomSplats)
            {
                ClearImmediate();
            }

            activeRoutine = null;
        }

        private IEnumerator WipeRoutine(float duration)
        {
            float startAlpha = GetCurrentAlpha();
            yield return FadeAll(startAlpha, 0f, duration);
            ClearImmediate();
            activeRoutine = null;
        }

        private void SpawnOverlay(ScreenOverlayProfile profile)
        {
            switch (profile.layoutMode)
            {
                case ScreenOverlayLayoutMode.FullScreen:
                    SpawnFullScreenTint(profile);
                    break;

                case ScreenOverlayLayoutMode.RandomSplats:
                default:
                    SpawnSplats(profile);
                    break;
            }
        }

        private void SpawnSplats(ScreenOverlayProfile profile)
        {
            int count = Random.Range(profile.minSplats, profile.maxSplats + 1);
            Vector2 area = overlayRoot.rect.size;

            for (int i = 0; i < count; i++)
            {
                var image = Instantiate(imagePrefab, overlayRoot);

                if (profile.splatSprites is { Length: > 0 })
                {
                    image.sprite = profile.splatSprites[Random.Range(0, profile.splatSprites.Length)];
                }

                var rect = image.rectTransform;
                rect.anchoredPosition = new Vector2(
                    Random.Range(-area.x * 0.35f, area.x * 0.35f),
                    Random.Range(-area.y * 0.35f, area.y * 0.35f));

                float rotation = Random.Range(profile.rotationRange.x, profile.rotationRange.y);
                rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

                float scale = Random.Range(profile.scaleRange.x, profile.scaleRange.y);
                rect.localScale = Vector3.one * scale;

                var color = profile.tint;
                color.a = 0f;
                image.color = color;

                activeImages.Add(image);
            }
        }

        private void SpawnFullScreenTint(ScreenOverlayProfile profile)
        {
            var image = Instantiate(imagePrefab, overlayRoot);
            var rect = image.rectTransform;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            var color = profile.tint;
            color.a = 0f;
            image.color = color;

            activeImages.Add(image);
        }

        private IEnumerator FadeAll(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                SetAllAlpha(to);
                yield break;
            }

            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float alpha = Mathf.Lerp(from, to, time / duration);
                SetAllAlpha(alpha);
                yield return null;
            }

            SetAllAlpha(to);
        }

        private void SetAllAlpha(float alpha)
        {
            foreach (var image in activeImages)
            {
                if (!image) continue;
                Color c = image.color;
                c.a = alpha;
                image.color = c;
            }
        }

        private float GetCurrentAlpha()
        {
            return (from image in activeImages where image select image.color.a).FirstOrDefault();
        }

        private void ClearImmediate()
        {
            foreach (var t in activeImages.Where(t => t))
            {
                Destroy(t.gameObject);
            }

            activeImages.Clear();
        }
    }
}