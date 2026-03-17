using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Views")]
    public GameObject mainMenuView;
    public GameObject gameView;
    public GameObject pauseView;
    public GameObject settingsView;

    // Track the view we came from to correctly return back from Settings
    private GameObject previousView; 

    private void Start()
    {
        // Start by displaying the Main Menu and hiding others
        ShowMainMenu();
    }

    /// <summary>
    /// Displays the Main Menu. Connect to QuitButton in Pause Menu or call on Start.
    /// </summary>
    public void ShowMainMenu()
    {
        HideAllViews();
        mainMenuView.SetActive(true);
        Time.timeScale = 1f;
    }

    /// <summary>
    /// Starts the game. Connect this to PlayButton in Main Menu.
    /// </summary>
    public void OnPlayClicked()
    {
        HideAllViews();
        gameView.SetActive(true);
        Time.timeScale = 1f; // Ensure time is running

        Timer timer = FindObjectOfType<Timer>();
        if (timer != null)
        {
            timer.ResetTimer();
            timer.StartTimer();
        }
    }

    /// <summary>
    /// Pauses the game. Connect this to the Pause button in Game View.
    /// </summary>
    public void OnPauseClicked()
    {
        HideAllViews();
        pauseView.SetActive(true);
        Time.timeScale = 0f; // Stop the game time
    }

    /// <summary>
    /// Resumes the game. Connect this to ResumeButton in Pause View.
    /// </summary>
    public void OnResumeClicked()
    {
        HideAllViews();
        gameView.SetActive(true);
        Time.timeScale = 1f; // Resume the game time
    }

    /// <summary>
    /// Opens Settings. Connect this to SettingsButton in Main Menu & Pause View.
    /// </summary>
    public void OnSettingsClicked()
    {
        // Remember where we came from (Main Menu or Pause View)
        if (mainMenuView.activeSelf) previousView = mainMenuView;
        else if (pauseView.activeSelf) previousView = pauseView;
        
        HideAllViews();
        settingsView.SetActive(true);
    }

    /// <summary>
    /// Closes Settings and returns to the previous view. Connect to ResumeButton in Settings View.
    /// </summary>
    public void OnSettingsBackClicked()
    {
        HideAllViews();
        if (previousView != null)
        {
            previousView.SetActive(true);
        }
        else
        {
            mainMenuView.SetActive(true);
        }
    }

    /// <summary>
    /// Quits the game or goes to Main Menu.
    /// Connect to QuitButton in Main Menu and Pause View.
    /// </summary>
    public void OnQuitClicked()
    {
        if (pauseView.activeSelf)
        {
            // If quitting from Pause Menu, return to Main Menu
            ShowMainMenu();
        }
        else if (mainMenuView.activeSelf)
        {
            // If quitting from Main Menu, close the app
            Debug.Log("Quitting application...");
            Application.Quit();
        }
    }

    /// <summary>
    /// Helper method to easily disable all views.
    /// </summary>
    private void HideAllViews()
    {
        if (mainMenuView != null) mainMenuView.SetActive(false);
        if (gameView != null) gameView.SetActive(false);
        if (pauseView != null) pauseView.SetActive(false);
        if (settingsView != null) settingsView.SetActive(false);
    }
}
