using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CVCanvasAnswer : MonoBehaviour
{

    [SerializeField] TMP_Text[] meshes;
    [Space]
    [SerializeField] string defaultString;
    // ---

    string[] correctAnswers;

    // --

    void Awake()
    {
        foreach (var mesh in meshes)
        {
            mesh.text = defaultString;
        }
    }

    public void SetCorrectAnswers(string[] answers)
    {
        correctAnswers = (string[])answers.Clone();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out CVPalabra palabra)) return;
        if (!correctAnswers.Contains(palabra.Text.text)) return;

        var availableSlots = meshes.Where(x => x.text == defaultString).FirstOrDefault();

        if (availableSlots == null) return;
        availableSlots.text = palabra.Text.text;

        other.gameObject.SetActive(false);
    }

    // ---

    public void SetTexts(string[] strings)
    {
        for (int i = 0; i < meshes.Length; i++)
        {
            TMP_Text currentText = meshes[i];

            currentText.text = strings[i];
        }
    }

    public void SetText(string input)
    {
        meshes[0].text = input;
    }
}

