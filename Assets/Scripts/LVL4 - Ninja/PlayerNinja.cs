using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerNinja : MonoBehaviour
{

    [SerializeField] ObjectSpawner Spawner;

    [SerializeField]
    TextMeshProUGUI ComboText;
    [SerializeField]
    TextMeshProUGUI ScoreText;

    int positives;
    int negatives;
    int errors;
    [SerializeField] int Combo;

    public void UpdateValues(bool isError, bool isObstacle)
    {
        if (isError)    //ERROR
        {
            errors++;
            Combo = 0;
            ComboText.text = Combo.ToString();
            Spawner.StageDown();
            //ErrorText.text = errors.ToString();
        }
        else
        {
            Combo++;
            ComboText.text = Combo.ToString();
            Spawner.Hit(Combo);
            if (isObstacle) //NEGATIVO ACERTADO
            {
                negatives++;
            }
            else            //POSITIVO ACERTADO
            {
                positives++;
            }
        }
    }
}
