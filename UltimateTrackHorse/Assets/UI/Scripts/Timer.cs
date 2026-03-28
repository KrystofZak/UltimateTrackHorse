using UnityEngine;
using TMPro; // Included for old references if any
using UnityEngine.UIElements;
using System;
using UI;

public class Timer : MonoBehaviour
{
    public event Action OnTimeUp;
    
    // Legacy support for TMP
    [Header("Timer Settings")]
    [SerializeField] private TMP_Text timerText;
    
    private Label uiToolkitTimerLabel;

    public float timeElapsed { get; private set; } = 0f;
    private float timeRemaining = 30f;
    private float incrementAmount = 0f;
    private bool timerIsRunning = false;
    private bool timeUp = false;

    private void Start()
    {
        // Try resolving new UI Toolkit document
        var document = FindObjectOfType<UIDocument>();
        if (document != null && document.rootVisualElement != null)
        {
            uiToolkitTimerLabel = document.rootVisualElement.Q<Label>("Timer");
        }
    }

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
        timeRemaining += incrementAmount;
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

    public void AddSecondsToIncrement(float seconds)
    {
        incrementAmount += seconds;
    }

    public void SubtractSecondsFromIncrement(float seconds)
    {
        incrementAmount -= seconds;
    }
    
    public void ResetIncrement()
    {
        incrementAmount = 0f;
    }

    private void DisplayTime(float timeToDisplay)
    {
        // Calculate seconds
        float seconds = Mathf.FloorToInt(timeToDisplay);
        
        // Calculate hundredths of a second (milliseconds formatted for 2 digits)
        float milliseconds = Mathf.FloorToInt((timeToDisplay - seconds) * 100);
        string timeString = string.Format("{0:00}:{1:00}", seconds, milliseconds);

        if (timerText != null)
        {
            timerText.text = timeString;
        }
        
        if (uiToolkitTimerLabel != null)
        {
            uiToolkitTimerLabel.text = timeString;
        }
    }
}
