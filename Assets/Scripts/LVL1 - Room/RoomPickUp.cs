using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class RoomPickUp : MonoBehaviour
{
    XRGrabInteractable interactable;

    Vector3 initialPosition;
    Quaternion initialRotation;

    private void Awake() 
    {
        interactable = GetComponent<XRGrabInteractable>();

        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    private void OnTriggerStay(Collider other) 
    {
        if (interactable.isSelected) return;
        if (!other.transform.root.TryGetComponent(out RoomPlayer player)) return;

        if (player.TryToPickUp(this))
        {
            gameObject.SetActive(false);
        }
        else
        {
            transform.position = initialPosition;
            transform.rotation = initialRotation;
        }
    }
}
