using UnityEngine;
using TMPro; // Required for TextMeshPro UI integration

public class Timer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private TMP_Text timerText;
    public float timeElapsed = 0f;
    private bool timerIsRunning = false;

    void Start()
    {
        // Timer no longer starts automatically
        DisplayTime(timeElapsed);
    }

    void Update()
    {
        if (timerIsRunning)
        {
            timeElapsed += Time.deltaTime;
            DisplayTime(timeElapsed);
        }
    }

    public void StartTimer()
    {
        timerIsRunning = true;
    }

    public void StopTimer()
    {
        timerIsRunning = false;
    }

    public void ResetTimer()
    {
        timeElapsed = 0f;
        DisplayTime(timeElapsed);
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
