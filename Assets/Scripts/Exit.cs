using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
public class Exit : MonoBehaviour
{
    XRSimpleInteractable interactable;

    float timeHolding = 0;

    [SerializeField] float holdTimeAmount;
    [SerializeField] Image holdTimeVisualizer;

    private void Awake() 
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void Update() 
    {
        holdTimeVisualizer.fillAmount = timeHolding / holdTimeAmount;
        
        if (!interactable.isSelected)
        {
            timeHolding = 0;
            return;
        }

        timeHolding += Time.deltaTime;

        if(timeHolding > holdTimeAmount)
        {
            RoomManager.Instance.CountItems();
            timeHolding = 0;
        }
    }
}
