using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Question", menuName = "InterviewDialogue/Question")]
public class InterviewQuestion : ScriptableObject
{
    [TextArea]
    public string Question;
    public Answer[] PossibleAnswers;
    public Response[] Responses;


    public enum AnswerType
    {
        Good,
        Neutral,
        Bad,
        None
    }

    [System.Serializable]
    public struct Answer
    {
        [TextArea]  public string AnswerText;
        public AnswerType AnswerType;
    }

    [System.Serializable]
    public struct Response
    {
        public AnswerType ResponseType;
        [TextArea] public string ResponseText;
    }
}
