using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(TMP_Text))]
[RequireComponent(typeof(BoxCollider))]
public class CVCanvasAnswer : MonoBehaviour
{
    [SerializeField] CVType.CVFields field;

    // ---

    public CVPalabra PalabraActual { get; private set; }

    // ---

    void OnValidate()
    {
        var collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
    }


    void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent(out CVPalabra palabra)) return;
        if (palabra.GetComponent<XRGrabInteractable>().isSelected) return;
        if (PalabraActual != null) return;

        PlacePalabra(palabra);
    }

    // ---

    public void PlacePalabra(CVPalabra palabra)
    {

        palabra.GetComponent<LookAtCamera>().enabled = false;
        palabra.transform.forward = -Vector3.forward;

        palabra.transform.position = transform.position;

        PalabraActual = palabra;

        palabra.GetComponent<XRGrabInteractable>().selectEntered.AddListener(TakePalabra);

        palabra.PlayExpandAnimation(GetComponent<RectTransform>().sizeDelta * 0.003f, 0.5f);
    }

    public void TakePalabra(SelectEnterEventArgs args)
    {
        PalabraActual.PlayReverseExpandAnimation(0.5f);
        
        PalabraActual.GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(TakePalabra);

        PalabraActual.GetComponentInParent<LookAtCamera>().enabled = true;
        PalabraActual = null;
    }


}


