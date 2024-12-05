using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player Instance  { get; private set; }

    public event Action<List<PickUp>> onItemGrabbed = delegate { };

    [SerializeField] Transform pickUpZone;
    [SerializeField] Vector3 pickupZoneOffset;

    public List<PickUp> grabbedItems;

    private void Awake() 
    {
        Instance = this;
    }

    private void Update() 
    {
        pickUpZone.position = Camera.main.transform.position + pickupZoneOffset;

        var cameraForward = Camera.main.transform.forward;;
        cameraForward.y = 0;
        pickUpZone.forward = cameraForward;
    }

    public bool TryToPickUp(PickUp pickup)
    {
        var desiredItems = RoomManager.Instance.desiredItems;
        
        if (desiredItems.Contains(pickup))
        {
            grabbedItems.Add(pickup);
            onItemGrabbed(grabbedItems);
            return true;
        }

        return false;
    }
}
