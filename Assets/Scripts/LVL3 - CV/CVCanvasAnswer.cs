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
    [SerializeField] Image image;

    // ---

    public CVPalabra PalabraActual { get; private set; }

    // ---

    void OnValidate()
    {
        var collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
    }

    Coroutine fadeCoroutine;

    void OnTriggerStay(Collider other)
    {
        if (!other.TryGetComponent(out CVPalabra palabra)) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(Fade(1f));

        if (palabra.GetComponent<XRGrabInteractable>().isSelected) return;
        if (PalabraActual != null) return;

        PlacePalabra(palabra);
    }

    // ---

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<CVPalabra>())
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(Fade(0f));
        }
    }

    IEnumerator Fade(float target)
    {
        float time = 0;
        float duration = 0.15f;
        float start = image.color.a;
        while (time < duration)
        {
            time += Time.deltaTime;
            var color = image.color;
            color.a = Mathf.Lerp(start, target, time / duration);
            image.color = color;
            yield return null;
        }
    }

    public void PlacePalabra(CVPalabra palabra) {

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

        var currentAnswerPool = LVL3Manager.Instance.CurrentCV[field];
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


