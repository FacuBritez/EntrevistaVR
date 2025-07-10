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

    public int vidas = 4;

    [SerializeField]
    GameObject[] vidasSprites;

    int positives;
    int negatives;
    int errors;
    [SerializeField] int Combo;
    [SerializeField] int Score;


    [SerializeField]
    Animator CanvasAnimator;

    [SerializeField]
    GameObject GanarTexto;
    [SerializeField]
    GameObject PerderTexto;

    [SerializeField]
    AudioClip LoseHPSound;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        Spawner.enabled = false;
        this.GetComponent<Timer>().enabled = false;

        ScoreText.text = "0";
        ComboText.text = "0";
    }

    public void UpdateValues(bool isError, bool isObstacle)
    {
        if (isError)    //ERROR
        {
            errors++;
            LoseHP();
            Combo = 0;
            ComboText.text = Combo.ToString();
            if (Spawner.currentStage > 0)
            {
                Spawner.StageDown();
            }
            //ErrorText.text = errors.ToString();
        }
        else
        {
            Combo++;
            ComboText.text = Combo.ToString();
            Spawner.Hit(Combo);
            if (isObstacle) //NEGATIVO ACERTADO
            {
                Score += 5 * (Spawner.currentStage + 1);
                ScoreText.text = Score.ToString();
                negatives++;
            }
            else            //POSITIVO ACERTADO
            {
                Score += 5 * (Spawner.currentStage + 1);
                ScoreText.text = Score.ToString();
                positives++;
            }
        }
    }
    public void LoseHP()
    {

        AudioSource.PlayClipAtPoint(LoseHPSound, this.transform.position);
        vidas--;
        if (vidas > 0)
        {
            for (int i = 0; i < vidasSprites.Length; i++)
            {
                vidasSprites[i].SetActive(false);
            }
            vidasSprites[vidas - 1].SetActive(true);
        }

        if (vidas == 0)
        {
            Die();
        }
    }

    void Die()
    {
        this.GetComponent<Timer>().timerText.gameObject.SetActive(false);
        this.GetComponent<Timer>().enabled = false;
        for (int i = 0; i < vidasSprites.Length; i++)
        {
            vidasSprites[i].SetActive(false);
        }
        Spawner.enabled = false;
        RemoveObjects();
        PerderTexto.SetActive(true);
        CanvasAnimator.SetTrigger("End");
    }

    void RemoveObjects()
    {
        ObjectInteractable[] objetosRestantes = FindObjectsOfType<ObjectInteractable>();
        for (int i = 0; i < objetosRestantes.Length; i++)
        {
            Destroy(objetosRestantes[i].gameObject);
        }
    }

    public void Win()
    {
        for (int i = 0; i < vidasSprites.Length; i++)
        {
            vidasSprites[i].SetActive(false);
        }
        Spawner.enabled = false;
        RemoveObjects();
        GanarTexto.SetActive(true);
        CanvasAnimator.SetTrigger("End");
    }
}
