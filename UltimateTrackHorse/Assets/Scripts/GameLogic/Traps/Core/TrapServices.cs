using System;
using GameLogic.Audio;
using GameLogic.Traps.Services;
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
        [SerializeField] private ScreenParticleService screenParticles;
        
        /// <summary>
        /// Service responsible for playing and clearing screen overlays.
        /// </summary>
        public ScreenOverlayService ScreenOverlay => screenOverlay;
        
        /// <summary>
        /// Service responsible for playing screen-space particles.
        /// </summary>
        public ScreenParticleService ScreenParticles => screenParticles;

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
            if (!screenParticles) screenParticles = FindAnyObjectByType<ScreenParticleService>();
            if (!obstacleManager) obstacleManager = FindAnyObjectByType<ObstacleManager>();
            if (!audioManager) audioManager = FindAnyObjectByType<AudioManager>();
        }
    }
}
