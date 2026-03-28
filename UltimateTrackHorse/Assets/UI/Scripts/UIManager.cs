using UnityEngine;

namespace UI
{
    // Obsolete class replaced by UIController.
    // Keeping this file empty prevents "Missing Script" errors in the editor
    // if it's still attached to any leftover prefabs or game objects.
    public class UIManager : MonoBehaviour
    {
        private void Awake()
        {
            Debug.LogWarning("Old UIManager is obsolete and should be removed from the scene. Use UIController instead.");
        }
    }
}
