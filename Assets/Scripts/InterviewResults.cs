using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InterviewResults : MonoBehaviour
{
    [SerializeField] int minNeutralEnding, minGoodEnding;
    
    [Space]
    [SerializeField] Sprite goodEnding, neutralEnding, badEnding;

    private void Awake() 
    {
        Image image = GetComponent<Image>();

        int score = InterviewManager.Score;

        Debug.Log(score);

        if(score < minNeutralEnding)
        {
			Debug.Log("bad");
			image.sprite = badEnding;
        } else if(score < minGoodEnding)
        {
			Debug.Log("Mid");
			image.sprite = neutralEnding;
        } else 
        {
			Debug.Log("good");
			image.sprite = goodEnding;
        }
    }
}
