using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CVCanvas : MonoBehaviour
{
    [SerializeField] CVCanvasAnswer sobreMi, formaciones, experiencias, cursos;

    // ---

    public void SetText(CVType CV)
    {
        sobreMi.SetCorrectAnswers(CV.SobreMi.Palabras);
        formaciones.SetCorrectAnswers(CV.Formaciones);
        experiencias.SetCorrectAnswers(CV.ExpLaboral);
        cursos.SetCorrectAnswers(CV.Cursos);
    }
}
