using UnityEngine;

public class GameMenuActions : MonoBehaviour
{
    public void QuitGame()
    {
        Application.Quit();
    }

    public void ResetGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
