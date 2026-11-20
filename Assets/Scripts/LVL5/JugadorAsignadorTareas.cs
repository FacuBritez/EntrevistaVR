using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class JugadorAsignadorTareas : MonoBehaviour
{
    [Header("Láser")]
    [SerializeField] private LineRenderer lineaLaser; // Asignar en Inspector
    [SerializeField] private float rango = 3f;       // Distancia del láser
    [Tooltip("Color del láser (celeste por defecto).")]
    [SerializeField] private Color colorLaser = new Color(0.45f, 0.8f, 1f, 1f);
    [Tooltip("Grosor del láser en unidades de mundo.")]
    [SerializeField] private float anchoLaser = 0.02f;

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
    private XRInteractorLineVisual lineVisual; // componente que también controla el LineRenderer

    void Start()
    {
        if (lineaLaser != null)
        {
            lineaLaser.enabled = false;
            lineaLaser.positionCount = 2;

            // El Ray Interactor trae un XRInteractorLineVisual que pelea por el
            // LineRenderer; lo guardamos para apagarlo mientras mostramos el láser.
            lineVisual = lineaLaser.GetComponent<XRInteractorLineVisual>();

            ConfigurarLaser();
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null && !audioSource.enabled)
            audioSource.enabled = true;
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
                compañeroApuntado.SetApuntado(false);
                compañeroApuntado = null;
            }
            ApagarLaser();
        }
    }

    // ---- Eventos llamados desde la hoja ----
    public void AgarrarTarea(XRGrabInteractable tarea, Transform origen)
    {
        tareaEnMano = tarea;
        origenRayo = origen;
        compañeroApuntado = null;
        if (lineaLaser != null)
        {
            ConfigurarLaser();
            // Apagamos el visual del Ray Interactor para que no sobrescriba el láser.
            if (lineVisual != null) lineVisual.enabled = false;
            lineaLaser.enabled = true;
        }
        ReproducirSonido(sonidoAgarrar);
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
                ReproducirSonido(sonidoSoltarSinApuntar);
                Debug.Log("❌ Soltó la tarea sin apuntar a ningún compañero. La hoja queda flotando para volver a agarrarla.");
            }
            else if (tareaEnMano == null)
            {
                Debug.Log("⚠️ SoltarTarea llamado sin tarea en mano");
            }
        }

        tareaEnMano = null;
        origenRayo = null;
        compañeroApuntado = null;
        ApagarLaser();
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

            // A un compañero dormido no se lo puede apuntar: no pasa nada.
            if (nuevo != null && !nuevo.EstaDormido)
            {
                if (nuevo != compañeroApuntado)
                {
                    if (compañeroApuntado != null)
                        compañeroApuntado.SetApuntado(false);

                    compañeroApuntado = nuevo;
                    compañeroApuntado.SetApuntado(true);
                    ReproducirSonido(sonidoApuntando);

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
    void ConfigurarLaser()
    {
        if (lineaLaser == null) return;

        // Material que respeta el color de vértice y la transparencia
        // (el material por defecto opaco no muestra el color).
        if (lineaLaser.material == null ||
            lineaLaser.material.shader == null ||
            lineaLaser.material.shader.name != "Sprites/Default")
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lineaLaser.material = new Material(shader);
        }

        lineaLaser.useWorldSpace = true;
        lineaLaser.positionCount = 2;
        lineaLaser.widthMultiplier = anchoLaser;

        lineaLaser.startColor = colorLaser;
        lineaLaser.endColor = colorLaser;

        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(colorLaser, 0f),
                new GradientColorKey(colorLaser, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(colorLaser.a, 0f),
                new GradientAlphaKey(colorLaser.a, 1f)
            }
        );
        lineaLaser.colorGradient = gradiente;
    }

    void ApagarLaser()
    {
        if (lineaLaser != null)
            lineaLaser.enabled = false;

        // Devolvemos el control al visual del Ray Interactor.
        if (lineVisual != null)
            lineVisual.enabled = true;
    }

    void ActualizarLaser()
    {
        if (lineaLaser == null || origenRayo == null) return;

        Vector3 origen = origenRayo.position;
        Vector3 direccion = origenRayo.forward;

        lineaLaser.SetPosition(0, origen);

        if (compañeroApuntado != null)
        {
            // Llega hasta el centro del cuerpo del compañero apuntado.
            lineaLaser.SetPosition(1, compañeroApuntado.PuntoApuntado);
            lineaLaser.startColor = colorLaser;
            lineaLaser.endColor = colorLaser;
        }
        else
        {
            // Llega hasta el rango máximo.
            lineaLaser.SetPosition(1, origen + direccion * rango);
            lineaLaser.startColor = colorLaser;
            lineaLaser.endColor = colorLaser;
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
            OleadasManager.Instancia.RegistrarAsignacion(correcta, compañeroApuntado.PuntoApuntado);
        }

        if (hoja.EsTutorial && OleadasManager.Instancia != null)
        {
            OleadasManager.Instancia.SoltarTareaTutorial(correcta);
        }

        ReproducirSonido(correcta ? sonidoAsignacionCorrecta : sonidoAsignacionIncorrecta);

        Debug.Log(
            $"📄 Tarea '{nueva.nombreTarea}' asignada a " +
            $"{compañeroApuntado.name} - " +
            $"{(correcta ? "CORRECTA" : "INCORRECTA")}"
        );

        compañeroApuntado.SetApuntado(false);

        if (correcta)
        {
            // Entrega correcta: la hoja desaparece (se la lleva el trabajador).
            Destroy(tareaEnMano.gameObject);
        }
        else
        {
            // Entrega incorrecta: la hoja se queda y se "desintegra".
            hoja.Desintegrar();
        }

        tareaEnMano = null;
        origenRayo = null;
        compañeroApuntado = null;
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
