using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomResultsLoader : MonoBehaviour
{
    [SerializeField] GameObject badEnding, goodEnding;
    
    [Space]
    [SerializeField] TMP_Text missingObjectsListText;

    private void Start() 
    {
        // Si no nos faltaron items, ganamos!
        // No tenemos que hacer nada mas excepto mostrar la pantalla de victoria.
        if(RoomManager.MissingObjects.Count == 0)
        {
            goodEnding.SetActive(true);
            return;
        }

        // Si llegamos acá, es porque si nos faltan items.
        missingObjectsListText.text = string.Join("\n", RoomManager.MissingObjects);

        badEnding.SetActive(true);
    }
}
