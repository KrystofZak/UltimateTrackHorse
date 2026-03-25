using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic.Obstacles
{
    public class ScreenTintController : MonoBehaviour
    {
        [SerializeField] private Image overlayImage;

        private Coroutine activeRoutine;

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