using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndScreen : MonoBehaviour
{
    [SerializeField] GameObject win;
    [SerializeField] GameObject lose;


    void Start()
    {
        LVL3Manager.Instance.OnGameFinished += ShowEndScreen;
    }

    public void ShowEndScreen(bool hasWon)
    {
        if (hasWon)
        {
            win.SetActive(true);
        }
        else
        {
            lose.SetActive(true);
        }
    }
}
