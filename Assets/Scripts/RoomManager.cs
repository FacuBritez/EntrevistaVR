using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public float remainingTime { get; private set; }

    [SerializeField] float timeLimit;
    public List<PickUp> desiredItems;

    private void Awake() 
    {
        Instance = this;

        StartCoroutine(Timer(timeLimit));
    }

    IEnumerator Timer(float timeToWait)
    {
        remainingTime = timeToWait;

        while (remainingTime > 0)
        {
            remainingTime -= Time.deltaTime;
            yield return null;
        }

        CountItems();
    }

    public void CountItems()
    {
        var gottenItems = Player.Instance.grabbedItems;

        if(gottenItems.Count == desiredItems.Count)
        {
            SceneManager.LoadScene("RoomWin");
        } else 
        {
            SceneManager.LoadScene("RoomLoss");
        }
    }
}
