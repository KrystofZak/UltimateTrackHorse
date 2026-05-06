using UnityEngine;

namespace GameLogic.Traps.Definitions
{
    /// <summary>
    /// Enum for different types of overlays
    /// </summary>
    public enum ScreenOverlayLayoutMode
    {
        RandomSplats,
        FullScreen
    }
    
    /// <summary>
    /// Configuration asset describing how a trap overlay should look and animate on screen.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Traps/Screen Overlay Profile")]
    public class ScreenOverlayProfile : ScriptableObject
    {
        /// <summary>
        /// Layout of the overlay image(s)
        /// </summary>
        public ScreenOverlayLayoutMode layoutMode = ScreenOverlayLayoutMode.FullScreen;
        /// <summary>
        /// Sprite variants used when spawning overlay splats.
        /// </summary>
        public Sprite[] splatSprites;

        /// <summary>
        /// Base tint applied to each spawned overlay image.
        /// </summary>
        public Color tint = Color.white;

        /// <summary>
        /// Target overlay alpha reached after the fade-in.
        /// </summary>
        [Range(0f, 1f)] public float alpha = 0.4f;

        /// <summary>
        /// Minimum number of overlay splats to spawn.
        /// </summary>
        public int minSplats = 1;

        /// <summary>
        /// Maximum number of overlay splats to spawn.
        /// </summary>
        public int maxSplats = 3;

        /// <summary>
        /// Inclusive random scale range applied to each spawned splat.
        /// </summary>
        public Vector2 scaleRange = new Vector2(0.8f, 1.3f);

        /// <summary>
        /// Inclusive random Z rotation range applied to each spawned splat.
        /// </summary>
        public Vector2 rotationRange = new Vector2(-25f, 25f);

        /// <summary>
        /// Duration of the fade-in animation.
        /// </summary>
        public float fadeIn = 0.1f;

        /// <summary>
        /// Time the overlay remains fully visible before fading out.
        /// </summary>
        public float hold = 0.8f;

        /// <summary>
        /// Duration of the fade-out animation.
        /// </summary>
        public float fadeOut = 1f;

        /// <summary>
        /// Suggested wipe duration for manually clearing this overlay.
        /// </summary>
        public float wipeDuration = 0.25f;
    }
}
