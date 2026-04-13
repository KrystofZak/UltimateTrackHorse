using UnityEngine;

namespace GameLogic.Traps
{
    /// <summary>
    /// Central container for shared systems used by trap runtimes and effects.
    /// </summary>
    public class TrapServices : MonoBehaviour
    {
        [SerializeField] private ScreenOverlayService screenOverlay;
        [SerializeField] private ObstacleManager obstacleManager;

        /// <summary>
        /// Service responsible for playing and clearing screen overlays.
        /// </summary>
        public ScreenOverlayService ScreenOverlay => screenOverlay;

        /// <summary>
        /// Service responsible for managing obstacles.
        /// </summary>
        public ObstacleManager ObstacleManager => obstacleManager;
    }
}
