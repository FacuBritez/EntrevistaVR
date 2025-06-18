using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ResetPlayerPosition : MonoBehaviour
{
    [SerializeField] InputActionAsset actions;

    
    Transform cameraTransform => Camera.main.transform;

    Vector3 initialPosition;

    private void Awake() 
    {
        initialPosition = transform.position;
    }

    private void OnEnable() 
    {
		actions.FindActionMap("XRI LeftHand Interaction").FindAction("Reposition").performed += CallResetPosition;

		Invoke("ResetPosition", 0.5f);
    }

    private void OnDisable()
    {
        actions.FindActionMap("XRI LeftHand Interaction").FindAction("Reposition").performed -= CallResetPosition;
    }

    void CallResetPosition(InputAction.CallbackContext context) => ResetPosition();

    public void ResetPosition()
    {
        var posOffset = initialPosition - cameraTransform.position;
        posOffset.y = 0;

        var fwd = Vector3.forward;
        fwd.y = 0;
        var camFwd = cameraTransform.forward;
        camFwd.y = 0;
        var angle = Vector3.SignedAngle(camFwd, fwd, Vector3.up);

        transform.RotateAround(cameraTransform.position, Vector3.up, angle);
        transform.position += posOffset;
    }
}
