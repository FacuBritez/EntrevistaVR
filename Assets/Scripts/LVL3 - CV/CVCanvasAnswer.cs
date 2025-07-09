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

    void OnValidate()
    {
        var rectTransform = transform as RectTransform;

        var collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
        // Algunos collider son distintos, los comento para evitar ifs :P
        //collider.size = new Vector3(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y, 50f);
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
    }

    public void TakePalabra(SelectEnterEventArgs args)
    {
        PalabraActual.GetComponent<XRGrabInteractable>().selectEntered.RemoveListener(TakePalabra);

        PalabraActual.GetComponentInParent<LookAtCamera>().enabled = true;
        PalabraActual = null;
    }


}


