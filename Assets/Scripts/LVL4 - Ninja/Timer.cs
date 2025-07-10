using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Text
using TMPro;
using System;

public class Timer : MonoBehaviour
{
    public float totalTime = 120f;
    public TextMeshProUGUI timerText;
    [SerializeField] float currentTime;

    public bool isRunning = true;

    private void OnEnable()
    {
        StartTimer();
    }

    void Start()
    {
        StartTimer();
    }

    void Update()
    {
        if (isRunning)
        {
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                UpdateTimerText(currentTime);
            }
            else
            {
                isRunning = false;
                currentTime = totalTime;
                UpdateTimerText(currentTime);
                Debug.Log("Termino el tiempo");
                timerText.enabled = false;
                this.enabled = false;
                this.gameObject.GetComponent<PlayerNinja>().Win();

            }
        }
        
    }

    void StartTimer()
    {
        isRunning = true;
        timerText.enabled = true;
        currentTime = totalTime;
        UpdateTimerText(currentTime);
    }

    void UpdateTimerText(float time)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}