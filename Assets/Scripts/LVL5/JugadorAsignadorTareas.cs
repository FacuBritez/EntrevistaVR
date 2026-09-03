using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class JugadorAsignadorTareas : MonoBehaviour
{
    [Header("Láser")]
    [SerializeField] private LineRenderer lineaLaser; // Asignar en Inspector
    [SerializeField] private float rango = 3f;       // Distancia del láser

    [SerializeField] private float radioDeteccion = 0.1f;

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoAgarrar;
    [SerializeField] private AudioClip sonidoApuntando;
    [SerializeField] private AudioClip sonidoAsignacionCorrecta;
    [SerializeField] private AudioClip sonidoAsignacionIncorrecta;
    [SerializeField] private AudioClip sonidoSoltarSinApuntar;

    private XRGrabInteractable tareaEnMano;
    private Compañero compañeroApuntado;
    private Transform origenRayo; // se asigna dinámicamente según la mano que agarró

    void Start()
    {
        if (lineaLaser != null)
        {
            lineaLaser.enabled = false;
            lineaLaser.positionCount = 2;
        }
    }

    void Update()
    {
        if (tareaEnMano != null)
        {
            DetectarCompañero();
            ActualizarLaser();
        }
        else
        {
            if (compañeroApuntado != null)
            {
                Debug.Log("👋 Dejó de apuntar (sin tarea)");
                compañeroApuntado = null;
            }
            if (lineaLaser != null && lineaLaser.enabled)
                lineaLaser.enabled = false;
        }
    }

    // ---- Eventos llamados desde la hoja ----
    public void AgarrarTarea(XRGrabInteractable tarea, Transform origen)
    {
        tareaEnMano = tarea;
        origenRayo = origen;
        compañeroApuntado = null;
        if (lineaLaser != null) lineaLaser.enabled = true;
        audioSource.PlayOneShot(sonidoAgarrar);
        Debug.Log("📄 Tarea agarrada");
    }

    public void SoltarTarea()
    {
        if (tareaEnMano != null && compañeroApuntado != null)
        {
            AsignarTarea();
        }
        else
        {
            if (tareaEnMano != null && compañeroApuntado == null)
            {
                audioSource.PlayOneShot(sonidoSoltarSinApuntar);
                Debug.Log("❌ Soltó la tarea sin apuntar a ningún compañero");
            }
            else if (tareaEnMano == null)
                Debug.Log("⚠️ SoltarTarea llamado sin tarea en mano");
        }

        tareaEnMano = null;
        origenRayo = null;
        compañeroApuntado = null;
        if (lineaLaser != null) lineaLaser.enabled = false;
    }

    // ---- Detección con raycast ----
    void DetectarCompañero()
    {
        if (origenRayo == null) return;

        Vector3 origen = origenRayo.position;
        Vector3 direccion = origenRayo.forward;

        Debug.DrawRay(origen, direccion * rango, Color.yellow);

        RaycastHit hit;

        if (Physics.SphereCast(origen, radioDeteccion, direccion, out hit, rango))
        {
            Compañero nuevo = hit.collider.GetComponentInParent<Compañero>();

            if (nuevo != null)
            {
                if (nuevo != compañeroApuntado)
                {
                    if (compañeroApuntado != null)
                        compañeroApuntado.SetApuntado(false);

                    compañeroApuntado = nuevo;
                    compañeroApuntado.SetApuntado(true);
                    audioSource.PlayOneShot(sonidoApuntando);

                    Debug.Log($"🎯 Apuntando a: {nuevo.name} (con tarea)");
                }

                return;
            }
        }

        if (compañeroApuntado != null)
        {
            compañeroApuntado.SetApuntado(false);
            Debug.Log($"👋 Dejó de apuntar a {compañeroApuntado.name} (con tarea)");
            compañeroApuntado = null;
        }
    }
    // ---- Láser ----
    void ActualizarLaser()
    {
        if (lineaLaser == null || origenRayo == null) return;

        Vector3 origen = origenRayo.position;
        Vector3 direccion = origenRayo.forward;

        lineaLaser.SetPosition(0, origen);

        if (compañeroApuntado != null)
        {
            // Láser verde que llega hasta el compañero
            lineaLaser.SetPosition(1, compañeroApuntado.transform.position);
            lineaLaser.startColor = Color.green;
            lineaLaser.endColor = Color.green;
        }
        else
        {
            // Láser rojo hasta el rango máximo
            lineaLaser.SetPosition(1, origen + direccion * rango);
            lineaLaser.startColor = Color.red;
            lineaLaser.endColor = Color.red;
        }
    }

    // ---- Asignación ----
    void AsignarTarea()
    {
        HojaTarea hoja = tareaEnMano.GetComponent<HojaTarea>();

        if (hoja == null || compañeroApuntado == null)
            return;

        Tarea nueva = new Tarea
        {
            nombreTarea = hoja.nombreTarea,
            rolRequerido = hoja.rolRequerido,
            duracionBase = hoja.duracionBase
        };

        bool correcta = compañeroApuntado.EsRolCorrecto(hoja.rolRequerido);

        compañeroApuntado.AsignarTarea(nueva);

        if (OleadasManager.Instancia != null)
        {
            OleadasManager.Instancia.RegistrarAsignacion(correcta);
        }

        audioSource.PlayOneShot(correcta ? sonidoAsignacionCorrecta : sonidoAsignacionIncorrecta);

        Debug.Log(
            $"📄 Tarea '{nueva.nombreTarea}' asignada a " +
            $"{compañeroApuntado.name} - " +
            $"{(correcta ? "CORRECTA" : "INCORRECTA")}"
        );

        compañeroApuntado.SetApuntado(false);

        Destroy(tareaEnMano.gameObject);

        tareaEnMano = null;
        origenRayo = null;
        compañeroApuntado = null;
    }
}