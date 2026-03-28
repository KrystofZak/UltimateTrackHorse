using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Obstacles
{
    public class ScreenTintController : MonoBehaviour
    {
        [SerializeField] private Image overlayImage;

        private Coroutine activeRoutine;

        private void Awake()
        {
            // Auto-heal: If the old UI image was deleted, automatically spawn a new one on top of the screen!
            if (overlayImage == null)
            {
                GameObject canvasObj = new GameObject("Auto_SmokeTintCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // Render above everything else

                GameObject imageObj = new GameObject("Auto_SmokeTintImage");
                imageObj.transform.SetParent(canvasObj.transform, false);

                overlayImage = imageObj.AddComponent<Image>();
                overlayImage.raycastTarget = false; // Don't block the player from clicking pause!
                overlayImage.color = new Color(0, 0, 0, 0); // Start totally invisible

                // Stretch the image to perfectly cover the entire screen
                RectTransform rt = overlayImage.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                Debug.Log("ScreenTintController: Rebuilt the missing UI Overlay Image automatically.");
            }
        }

        public void PlayTint(Color tintColor, float targetAlpha, float fadeIn, float hold, float fadeOut)
        {
            if (activeRoutine != null)
                StopCoroutine(activeRoutine);

            activeRoutine = StartCoroutine(TintRoutine(tintColor, targetAlpha, fadeIn, hold, fadeOut));
        }

        private IEnumerator TintRoutine(Color tintColor, float targetAlpha, float fadeIn, float hold, float fadeOut)
        {
            // start from current color but switch tint
            Color c = tintColor;
            c.a = 0f;
            overlayImage.color = c;

            float t = 0f;

            // fade in
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                var a = Mathf.Lerp(0f, targetAlpha, t / fadeIn);
                overlayImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, a);
                yield return null;
            }

            overlayImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, targetAlpha);

            // hold
            yield return new WaitForSeconds(hold);

            // fade out
            t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                var a = Mathf.Lerp(targetAlpha, 0f, t / fadeOut);
                overlayImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, a);
                yield return null;
            }

            overlayImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, 0f);
            activeRoutine = null;
        }
    }
}