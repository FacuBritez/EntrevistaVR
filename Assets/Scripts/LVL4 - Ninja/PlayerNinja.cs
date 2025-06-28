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


    [SerializeField]
    SuctionGun mySuctionGun;

    [SerializeField]
    Sword mySword;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {

    }


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }


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
