using UnityEngine;
using TMPro; // Required for TextMeshPro UI integration

public class Timer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private TMP_Text timerText;
    private float timeRemaining = 30f;
    private bool timerIsRunning = false;

    void Start()
    {
        // Starts the timer automatically
        timerIsRunning = true;
    }

    void Update()
    {
        if (timerIsRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                timeRemaining = 0;
                timerIsRunning = false;
                DisplayTime(timeRemaining);
                
                // --- Add any logic for when the timer ends here ---
                Debug.Log("Time has run out!");
            }
        }
    }

    private void DisplayTime(float timeToDisplay)
    {
        // Calculate seconds
        float seconds = Mathf.FloorToInt(timeToDisplay);
        
        // Calculate hundredths of a second (milliseconds formatted for 2 digits)
        float milliseconds = Mathf.FloorToInt((timeToDisplay - seconds) * 100);

        // Updates the text to the format 30:00 (Seconds:Milliseconds)
        timerText.text = string.Format("{0:00}:{1:00}", seconds, milliseconds);
    }
}
