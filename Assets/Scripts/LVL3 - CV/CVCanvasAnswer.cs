using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(BoxCollider))]


public class CVCanvasAnswer : MonoBehaviour
{
    [SerializeField] CVType.CVFields field;

    // ---

    public CVPalabra PalabraActual { get; private set; }

    void OnValidate()
    {
        var rectTransform = transform as RectTransform;

        var collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
        // Algunos collider son distintos, los comento para evitar ifs :P
        //collider.size = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, 50f);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent(out CVPalabra palabra)) return;
        if (palabra.GetComponent<XRGrabInteractable>().isSelected) return;
        if (PalabraActual != null) return;

        PlacePalabra(palabra);
    }


    public void PlacePalabra(CVPalabra palabra) {

        palabra.GetComponent<LookAtCamera>().enabled = false;
        palabra.transform.forward = -Vector3.forward;

        palabra.transform.position = transform.position;

        PalabraActual = palabra;

        palabra.GetComponent<XRGrabInteractable>().selectEntered.AddListener(TakePalabra);
    }
    
    public void TakePalabra(SelectEnterEventArgs args)
    {
        PalabraActual.GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(TakePalabra);

        PalabraActual.GetComponentInParent<LookAtCamera>().enabled = true;
        PalabraActual = null;
    }


}


