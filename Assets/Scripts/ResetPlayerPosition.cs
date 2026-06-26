using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class ResetPlayerPosition : MonoBehaviour
{
    [SerializeField] InputActionAsset actions;
    [SerializeField] float holdThreshold = 1.5f;
    [SerializeField] string escenaDestino;

    Transform cameraTransform => Camera.main.transform;
    Vector3 initialPosition;
    float pressStartTime;

    private void Awake()
    {
        initialPosition = transform.position;
    }

    private void OnEnable()
    {
        var action = actions.FindActionMap("XRI LeftHand Interaction").FindAction("Reposition");
        action.started += OnPressStarted;
        action.canceled += OnPressReleased;
        Invoke("ResetPosition", 0.5f);
    }

    private void OnDisable()
    {
        var action = actions.FindActionMap("XRI LeftHand Interaction").FindAction("Reposition");
        action.started -= OnPressStarted;
        action.canceled -= OnPressReleased;
    }

    void OnPressStarted(InputAction.CallbackContext ctx)
    {
        pressStartTime = Time.time;
    }

    void OnPressReleased(InputAction.CallbackContext ctx)
    {
        if (Time.time - pressStartTime >= holdThreshold)
            SceneManager.LoadScene(escenaDestino);
        else
            ResetPosition();
    }

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
