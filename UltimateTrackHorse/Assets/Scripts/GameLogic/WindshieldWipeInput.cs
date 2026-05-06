using GameLogic.Traps.Services;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// Provides player input for manually wiping the active trap screen overlay.
    /// </summary>
    public class WindshieldWipeInput : MonoBehaviour
    {
        [SerializeField] private KeyCode wipeKey = KeyCode.Space;
        [SerializeField] private ScreenOverlayService overlayService;
        [SerializeField] private float wipeDuration = 0.2f;

        private void Update()
        {
            if (Input.GetKeyDown(wipeKey))
            {
                overlayService.WipeCurrent(wipeDuration);
            }
        }
    }
}
