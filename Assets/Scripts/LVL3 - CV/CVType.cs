using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CVType", menuName = "LVL3/CV Type")]
public class CVType : ScriptableObject
{
    [TextArea] public string[] SobreMi;
    [TextArea] public string[] Formaciones;
    [TextArea] public string[] ExpLaboral;
    [TextArea] public string[] Cursos;

    [Space]
    [TextArea] public string[] OpcionesIncorrectas;

    public enum CVFields
    {
        SobreMi,
        Formaciones,
        ExpLaboral,
        Cursos
    }
}    

