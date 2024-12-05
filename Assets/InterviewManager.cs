using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class InterviewManager : MonoBehaviour
{
    #region Serialized Fields
    [SerializeField, TextArea] string initialDialogue, endingDialogue;
    [SerializeField] InterviewQuestion[] questions;

    [Space, SerializeField] float timeToAnswer;

    [Space(40f)]
    [SerializeField] UnityEvent<string, bool> showText;
    [SerializeField] UnityEvent<InterviewQuestion.Answer[], float> onQuestionStart;
    [SerializeField] UnityEvent onQuestionEnd;

    #endregion

    int currentQuestionIndex = -1;
    InterviewQuestion currentQuestion => questions[currentQuestionIndex];

    static public int Score;

    private void Start() 
    {
        showText.Invoke(initialDialogue, true);
    }

    public void AskNextQuestion()
    {
        currentQuestionIndex++;

        if(currentQuestionIndex == questions.Length)
        {
            showText.Invoke(endingDialogue, true);
            return;
        }

        if(currentQuestionIndex > questions.Length)
        {
            SceneManager.LoadScene("InterviewResults");
            return;
        }

        showText.Invoke(currentQuestion.Question, false);

        StartCoroutine(StartQuestion());
    }

    public void EndQuestion(InterviewQuestion.AnswerType questionPicked)
    {
        StopAllCoroutines();

        if(questionPicked == InterviewQuestion.AnswerType.Good)
        {
            Score++;
        } else if(questionPicked == InterviewQuestion.AnswerType.Bad || questionPicked == InterviewQuestion.AnswerType.None)
        {
            Score--;
        }

        string response = currentQuestion.Responses.Where(x => x.ResponseType == questionPicked)?.FirstOrDefault().ResponseText;
        showText.Invoke(response, true);

        onQuestionEnd.Invoke();
    }

    IEnumerator StartQuestion()
    {
        yield return new WaitForSeconds(1f);

        onQuestionStart.Invoke(currentQuestion.PossibleAnswers, timeToAnswer);

        yield return new WaitForSeconds(timeToAnswer);

        EndQuestion(InterviewQuestion.AnswerType.None);
    }
}
