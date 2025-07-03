using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CVPalabra : MonoBehaviour
{
    [SerializeField] TMP_Text TextMesh;

    // ---

    public CVType.CVFields fieldType { get; private set; }

    // ---

    public void SetText(string text, CVType.CVFields sourceField)
    {
        TextMesh.text = text;
        fieldType = sourceField;
    }
}
