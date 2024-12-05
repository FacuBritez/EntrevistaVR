using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UITimer : MonoBehaviour
{
    [SerializeField] TMP_Text timeText;

    private void Update() 
    {
        int minutes = Mathf.FloorToInt(RoomManager.Instance.remainingTime / 60f);
        int seconds = Mathf.FloorToInt(RoomManager.Instance.remainingTime - minutes * 60f);

        timeText.text = string.Format("{0:0}:{1:00}", minutes, seconds);
    }
}
