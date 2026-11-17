using UnityEngine;

/// <summary>
/// Hace que este GameObject mire de frente a la cámara principal,
/// rotando SOLO en el eje Y (billboard vertical).
///
/// ⚠️ IMPORTANTE (canvas / texto / sprites):
/// Un Canvas (UI) se lee por su cara +Z. Por eso, para que el texto
/// se vea bien y NO espejado, hay que apuntar el +Z del objeto HACIA
/// la cámara (LookRotation(cámara - objeto)).
/// No confundir con un SpriteRenderer o Quad, que se leen por -Z y
/// necesitan apuntar el +Z en dirección contraria a la cámara.
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

        // Vector desde el objeto HACIA la cámara, proyectado en el plano horizontal.
        // (Para un Canvas la cara legible es +Z, así que apuntamos el +Z hacia la cámara.)
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
