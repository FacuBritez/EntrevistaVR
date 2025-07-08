using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class TimerCanvas : MonoBehaviour
{
    [SerializeField] TMP_Text textMesh;

    private void Update()
    {
        if (LVL3Manager.Instance == null) return;

        TimeSpan timeSpan = TimeSpan.FromSeconds(LVL3Manager.Instance.remainingTime);

        textMesh.text = string.Format("{0:0}:{1:00}.{2:D3}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
    }
}
