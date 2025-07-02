using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerNinja : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI PositiveText;
    [SerializeField]
    TextMeshProUGUI NegativeText;
    [SerializeField]
    TextMeshProUGUI ErrorText;

    float positives;
    float negatives;
    float errors;


    public void UpdateValues(bool isError, bool isObstacle)
    {
        if (isError)
        {
            errors++;
            ErrorText.text = errors.ToString();
        }
        else
        {
            if (isObstacle)
            {
                negatives++;
                NegativeText.text = negatives.ToString();
            }
            else
            {
                positives++;
                PositiveText.text = positives.ToString();
            }
        }
    }
}
