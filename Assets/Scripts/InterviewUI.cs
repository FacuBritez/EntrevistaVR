using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InterviewUI : MonoBehaviour
{
    [SerializeField] TMP_Text questionText;

    [Space]
    [SerializeField] Button[] answerButtons;
    [SerializeField] Button next;

    [Space]
    [SerializeField] Image timer;

    [Space]
    [SerializeField] UnityEvent<InterviewQuestion.AnswerType> onAnswerPicked;
    
    public void ShowText(string text, bool showNext)
    {
        questionText.text = text;

        if(showNext)
        {
            next.transform.parent.gameObject.SetActive(true);
        } else 
        {
            next.transform.parent.gameObject.SetActive(false);
        }
    }

    public void ShowQuestion(InterviewQuestion.Answer[] answers, float answerTime)
    {
        answerButtons[0].transform.parent.gameObject.SetActive(true);
        timer.transform.parent.gameObject.SetActive(true);
        next.transform.parent.gameObject.SetActive(false);

        List<InterviewQuestion.Answer> answersAsList = new(answers);

        for (int i = 0; i < answerButtons.Length; i++)
        {
            var currentButton = answerButtons[i];
            var currentButtonText = currentButton.GetComponentInChildren<TMP_Text>();

            int randomAnswerIndex = Random.Range(0, answersAsList.Count);
            InterviewQuestion.Answer randomAnswer = answersAsList[randomAnswerIndex];

            currentButtonText.text = randomAnswer.AnswerText;
            currentButton.onClick.RemoveAllListeners();
            currentButton.onClick.AddListener(() => onAnswerPicked.Invoke(randomAnswer.AnswerType));

            answersAsList.RemoveAt(randomAnswerIndex);
        }

        StartCoroutine(Timer(answerTime));
    }

    public void HideQuestion()
    {
        answerButtons[0].transform.parent.gameObject.SetActive(false);
        timer.transform.parent.gameObject.SetActive(false);
    }

    IEnumerator Timer(float length)
    {
        float time = 0f;

        while (time < length)
        {
            time += Time.deltaTime;

            float progress = time/length;

            timer.fillAmount = 1f - progress;

            yield return null;
        }
    }
}
