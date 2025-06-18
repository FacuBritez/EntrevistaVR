using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPlayer : MonoBehaviour
{
    public static RoomPlayer Instance  { get; private set; }

    public event Action<List<RoomPickUp>> onItemGrabbed = delegate { };

    [SerializeField] Transform pickUpZone;
    [SerializeField] Vector3 pickupZoneOffset;

    public List<RoomPickUp> grabbedItems;

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

    public bool TryToPickUp(RoomPickUp pickup)
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
