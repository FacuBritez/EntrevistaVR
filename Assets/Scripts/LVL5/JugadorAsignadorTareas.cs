using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class JugadorAsignadorTareas : MonoBehaviour
{
    [Header("Láser")]
    [SerializeField] private LineRenderer lineaLaser; // Asignar en Inspector
    [SerializeField] private float rango = 3f;       // Distancia del láser

    private XRGrabInteractable tareaEnMano;
    private Compañero compañeroApuntado;

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
    public void AgarrarTarea(XRGrabInteractable tarea)
    {
        tareaEnMano = tarea;
        compañeroApuntado = null;
        if (lineaLaser != null) lineaLaser.enabled = true;
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
                Debug.Log("❌ Soltó la tarea sin apuntar a ningún compañero");
            else if (tareaEnMano == null)
                Debug.Log("⚠️ SoltarTarea llamado sin tarea en mano");
        }

        tareaEnMano = null;
        compañeroApuntado = null;
        if (lineaLaser != null) lineaLaser.enabled = false;
    }

    // ---- Detección con raycast ----
    void DetectarCompañero()
    {
        Vector3 origen = transform.position;
        Vector3 direccion = transform.forward;

        RaycastHit hit;
        if (Physics.Raycast(origen, direccion, out hit, rango))
        {
            Compañero nuevo = hit.collider.GetComponent<Compañero>();
            if (nuevo != null)
            {
                if (nuevo != compañeroApuntado)
                {
                    compañeroApuntado = nuevo;
                    Debug.Log($"🎯 Apuntando a: {nuevo.name} (con tarea)");
                }
                return;
            }
        }

        // Si ya no apunta a ningún compañero
        if (compañeroApuntado != null)
        {
            Debug.Log($"👋 Dejó de apuntar a {compañeroApuntado.name} (con tarea)");
            compañeroApuntado = null;
        }
    }

    // ---- Láser ----
    void ActualizarLaser()
    {
        if (lineaLaser == null) return;

        Vector3 origen = transform.position;
        Vector3 direccion = transform.forward;

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
        if (hoja == null || compañeroApuntado == null) return;

        Tarea nueva = new Tarea
        {
            nombreTarea = hoja.nombreTarea,
            rolRequerido = hoja.rolRequerido,
            duracionBase = hoja.duracionBase
        };

        compañeroApuntado.AsignarTarea(nueva);
        Debug.Log($"✅ Tarea '{nueva.nombreTarea}' asignada a {compañeroApuntado.name}");

        Destroy(tareaEnMano.gameObject);
        tareaEnMano = null;
        compañeroApuntado = null;
    }
}