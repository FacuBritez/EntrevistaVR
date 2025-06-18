using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Entrevistadora : MonoBehaviour
{

    Animator anim;

    private void Awake() 
    {
        anim = GetComponent<Animator>();
    }

    public void QuestionAnswered(InterviewQuestion.AnswerType answer)
    {
        anim.SetInteger("AnswerType", (int)answer);
        anim.SetTrigger("OnAnswer");


    }

    public void SpeechAnim()
    {
        anim.SetTrigger("OnQuestion");
    }
}
