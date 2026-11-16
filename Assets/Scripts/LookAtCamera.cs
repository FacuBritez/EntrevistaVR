using UnityEngine;

/// <summary>
/// Hace que este GameObject mire hacia la cámara principal,
/// rotando SOLO en el eje Y (billboard vertical).
/// Ideal para displays de trabajadores en VR.
/// </summary>
public class LookAtCamera : MonoBehaviour
{
    [Tooltip("Si está activado, el display sigue la cámara suavemente en lugar de hacerlo de golpe.")]
    [SerializeField] private bool suavizado = false;
    [SerializeField] private float velocidadSuavizado = 10f;

    private Transform camTransform;

    private void Start()
    {
        if (Camera.main != null)
            camTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (camTransform == null)
        {
            if (Camera.main != null) camTransform = Camera.main.transform;
            else return;
        }

        // Vector hacia la cámara proyectado en el plano horizontal (solo Y)
        Vector3 direccion = camTransform.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.0001f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direccion);

        if (suavizado)
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * velocidadSuavizado);
        else
            transform.rotation = targetRotation;
    }
}
