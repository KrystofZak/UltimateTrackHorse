using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    public GameObject settingsMenu;

    public void ToggleMenu()
    {
        bool isActive = settingsMenu.activeSelf;
        settingsMenu.SetActive(!isActive);
        Time.timeScale = isActive ? 1 : 0;
    }

    public void UnTogleMenu()
    {
        settingsMenu.SetActive(false);
        Time.timeScale = 1;
    }
}
