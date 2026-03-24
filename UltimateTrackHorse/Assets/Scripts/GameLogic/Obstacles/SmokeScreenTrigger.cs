using UnityEngine;

namespace GameLogic.Obstacles
{
    public class SmokeScreenTrigger : MonoBehaviour, IScreenTintInjectable
    {
        [SerializeField] private string carTag = "Player";
        [SerializeField] private Color smokeColor = Color.gray;
        [SerializeField] private float alpha = 0.35f;
        [SerializeField] private float fadeIn = 0.15f;
        [SerializeField] private float hold = 0.8f;
        [SerializeField] private float fadeOut = 1.0f;

        private ScreenTintController tintController;

        public void Initialize(ScreenTintController controller)
        {
            tintController = controller;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(carTag))
                return;

            if (tintController != null)
                tintController.PlayTint(smokeColor, alpha, fadeIn, hold, fadeOut);
        }

        public void InjectScreenTint(ScreenTintController controller)
        {
            tintController = controller;
        }
    }
}