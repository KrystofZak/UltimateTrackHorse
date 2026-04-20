using System;
using GameLogic.Audio;
using UnityEngine;

namespace GameLogic.Traps.Core
{
    /// <summary>
    /// Central container for shared systems used by trap runtimes and effects.
    /// </summary>
    public class TrapServices : MonoBehaviour
    {
        [SerializeField] private ScreenOverlayService screenOverlay;
        [SerializeField] private ObstacleManager obstacleManager;
        [SerializeField] private AudioManager audioManager;
        
        /// <summary>
        /// Service responsible for playing and clearing screen overlays.
        /// </summary>
        public ScreenOverlayService ScreenOverlay => screenOverlay;

        /// <summary>
        /// Service responsible for managing obstacles.
        /// </summary>
        public ObstacleManager ObstacleManager => obstacleManager;
        
        /// <summary>
        /// Service responsible for managing trap sfx.
        /// </summary>
        public AudioManager Audio => audioManager;

        private void Awake()
        {
            if (!screenOverlay) screenOverlay = FindAnyObjectByType<ScreenOverlayService>();
            if (!obstacleManager) obstacleManager = FindAnyObjectByType<ObstacleManager>();
            if (!audioManager) audioManager = FindAnyObjectByType<AudioManager>();
        }
    }
}
