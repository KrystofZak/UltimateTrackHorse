using UnityEngine;
using TMPro; // Required for TextMeshPro UI integration
using System;

public class Timer : MonoBehaviour
{
    public event Action OnTimeUp;
    [Header("Timer Settings")]
    [SerializeField] private TMP_Text timerText;
    public float timeElapsed { get; private set; } = 0f;
    private float timeRemaining = 30f;
    private bool timerIsRunning = false;
    private bool timeUp = false;

    void Update()
    {
        if (timerIsRunning)
        {
            timeElapsed += Time.deltaTime;
            timeRemaining -= Time.deltaTime;

            if (timeRemaining < 0)
            {
                timeRemaining = 0;
                if (!timeUp)
                {
                    timeUp = true;
                    OnTimeUp?.Invoke();
                }
            }
            
            DisplayTime(timeRemaining);
        }
    }

    public void SetStartTime(float startTime)
    {
        timeRemaining = startTime;
        timeElapsed = 0f;
        timeUp = false;
        DisplayTime(timeRemaining);
        StartTimer();
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
        StopTimer();
        timeElapsed = 0f;
        timeRemaining = 0f;
        timeUp = false;
        DisplayTime(timeRemaining);
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
