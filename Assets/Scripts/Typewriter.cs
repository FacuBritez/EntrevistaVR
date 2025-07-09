using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
[ExecuteInEditMode]
public class Typewriter : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] float meshRevealProgress;
    TMP_Text textMesh;

    private void Awake()
    {
        textMesh = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        textMesh.maxVisibleCharacters = Mathf.FloorToInt(meshRevealProgress * textMesh.textInfo.characterCount);
    }
}
