using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(HingeJoint))]
public class HingedDoor : MonoBehaviour
{
    [SerializeField] XRGrabInteractable grabbableHandle;
    [SerializeField] Rigidbody fixedJointHandle;

    private void Awake() 
    {
        grabbableHandle.selectExited.AddListener( (p) => ResetHandle() );
    }

    private void FixedUpdate() 
    {
        fixedJointHandle.MovePosition(grabbableHandle.transform.position);
    }

    public void ResetHandle()
    {
        grabbableHandle.transform.localPosition = Vector3.zero;
        grabbableHandle.transform.localRotation = Quaternion.identity;
    }
}
