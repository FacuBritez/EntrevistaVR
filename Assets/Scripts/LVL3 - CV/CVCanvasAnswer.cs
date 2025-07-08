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

        if (!IsPalabraCorrectlyPlaced(palabra, out string errorReason))
        {
            palabra.ShowCorrection(errorReason);
        }
    }

    public void TakePalabra(SelectEnterEventArgs args)
    {
        PalabraActual.PlayReverseExpandAnimation(0.5f);
        PalabraActual.HideCorrection();

        PalabraActual.GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(TakePalabra);

        PalabraActual.GetComponentInParent<LookAtCamera>().enabled = true;
        PalabraActual = null;
    }

    bool IsPalabraCorrectlyPlaced(CVPalabra palabra, out string errorReason)
    {
        errorReason = "Respuesta incorrecta!";

        var currentAnswerPool = LVL3Manager.instance.CurrentCV[field];
        var currentAnswer = palabra.GetText();

        if (!currentAnswerPool.Contains(currentAnswer))
        {
            errorReason = "Categoría incorrecta!";
            return false;
        }

        if (currentAnswerPool.ToList().IndexOf(currentAnswer) != transform.GetSiblingIndex())
        {
            errorReason = "Orden incorrecto!";
            return false;
        }

        return true;
    }


}


