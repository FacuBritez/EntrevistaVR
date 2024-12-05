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

        if(score < minNeutralEnding)
        {
            image.sprite = badEnding;
        } else if(score < minGoodEnding)
        {
            image.sprite = neutralEnding;
        } else 
        {
            image.sprite = goodEnding;
        }
    }
}
