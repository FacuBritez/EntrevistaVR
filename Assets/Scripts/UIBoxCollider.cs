using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[ExecuteInEditMode]
public class UIBoxCollider : MonoBehaviour
{
    BoxCollider myCollider;
    RectTransform rectTransform;

    // ---

    private void Awake()
    {
        myCollider = GetComponent<BoxCollider>();
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        myCollider.size = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, 50f);
    }
}
