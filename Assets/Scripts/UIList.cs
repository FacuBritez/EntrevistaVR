using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIList : MonoBehaviour
{
    [SerializeField] TMP_Text textMesh;

    private void Start() 
    {
        Player.Instance.onItemGrabbed += (items) => UpdateList(items, RoomManager.Instance.desiredItems);

        UpdateList(new List<PickUp>(), RoomManager.Instance.desiredItems);
    }

    void UpdateList(List<PickUp> grabbedItems, List<PickUp> desiredItems)
    {
        string result = "";

        var remainingItems = desiredItems.Where(x => !grabbedItems.Contains(x));

        foreach (var item in remainingItems)
        {
            result += item.name + "\n";
        }

        foreach (var item in grabbedItems)
        {
            result += "<s> <color=green>" + item.name + "</color> </s>" + "\n";
        }

        textMesh.text = result;
    }
}
