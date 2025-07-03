using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CVType", menuName = "LVL3/CV Type")]
public class CVType : ScriptableObject
{
    public DataSobreMi SobreMi;
    [TextArea] public string[] Formaciones;
    [TextArea] public string[] ExpLaboral;
    [TextArea] public string[] Cursos;

    [Space]
    [TextArea] public string[] OpcionesIncorrectas;

    [System.Serializable]

    public struct DataSobreMi
    {
        [TextArea] public string[] Oraciones;
        [TextArea] public string[] Palabras;

    }

    public enum CVFields
    {
        SobreMi,
        Formaciones,
        ExpLaboral,
        Cursos
    }
}    

