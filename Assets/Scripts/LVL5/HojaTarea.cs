using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class HojaTarea : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoTarea;

    [Header("Icono de rol (mismo orden que el enum RolTarea)")]
    [SerializeField] private Sprite[] iconosRol;

    public string nombreTarea = "Tarea física";
    public RolTarea rolRequerido = RolTarea.Programador;
    public float duracionBase = 5f;
    public bool EsTutorial = false; // para distinguir si es la tarea tutorial

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoSalida;

    private XRGrabInteractable grab;
    private JugadorAsignadorTareas jugador;

    private Vector3 escalaOriginal;
    private Coroutine rutinaAparicion;
    private SpriteRenderer iconoRenderer;

    void Awake()
    {
        // IMPORTANTE: se captura acá (Awake corre apenas se instancia),
        // no en Start, porque GeneradorTareas llama a la animación en el mismo
        // frame y Start todavía no corrió (dejaba la escala en 0 = invisible).
        escalaOriginal = transform.localScale;

        CrearIconoRol();
    }

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (sonidoSalida != null && audioSource != null)
            audioSource.PlayOneShot(sonidoSalida);
    }

    /// <summary>
    /// Crea un pequeño SpriteRenderer en el medio de la hoja para mostrar
    /// el ícono del rol al que hay que entregarle la tarea.
    /// </summary>
    private void CrearIconoRol()
    {
        var go = new GameObject("IconoRol", typeof(SpriteRenderer));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, -0.55f);
        go.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
        iconoRenderer = go.GetComponent<SpriteRenderer>();
        iconoRenderer.sortingOrder = 1;
    }

    private void ActualizarIcono()
    {
        if (iconoRenderer == null || iconosRol == null || iconosRol.Length == 0)
            return;

        int idx = (int)rolRequerido;
        if (idx >= 0 && idx < iconosRol.Length)
            iconoRenderer.sprite = iconosRol[idx];
    }

    /// <summary>
    /// Animación de "squash": la hoja sale del fax, se hace grande, luego muy
    /// pequeña (casi desaparece) y termina asentándose en "destino" (el plano).
    /// </summary>
    public void IniciarAnimacionAparicion(Vector3 destino)
    {
        if (rutinaAparicion != null) StopCoroutine(rutinaAparicion);
        rutinaAparicion = StartCoroutine(AnimacionAparicion(destino));
    }

    private IEnumerator AnimacionAparicion(Vector3 destino)
    {
        Vector3 origen = transform.position;
        Quaternion rotacionInicial = transform.rotation;
        // La hoja termina "acostada" (paralela a la mesa): girada 90° en X.
        Quaternion rotacionFinal = Quaternion.Euler(90f, 0f, 0f);
        float duracion = 1.3f;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            float u = Mathf.Clamp01(t);

            // Movimiento: del fax al plano, con un pequeño arco al salir.
            Vector3 pos = Vector3.Lerp(origen, destino, u);
            pos.y += Mathf.Sin(u * Mathf.PI) * 0.25f;
            transform.position = pos;

            // Rotación: de la inclinación del fax a quedar acostada.
            transform.rotation = Quaternion.Slerp(rotacionInicial, rotacionFinal, u);

            // Squash: grande -> muy pequeña -> normal.
            transform.localScale = escalaOriginal * FactorSquash(u);

            yield return null;
        }

        transform.position = destino;
        transform.rotation = rotacionFinal;
        transform.localScale = escalaOriginal;
        rutinaAparicion = null;
    }

    /// <summary>
    /// Curva de squash: crece a 1.6, baja a 0.1 (casi desaparece) y vuelve a 1.
    /// </summary>
    private float FactorSquash(float u)
    {
        if (u < 0.4f)
            return Mathf.Lerp(1f, 1.6f, u / 0.4f);
        if (u < 0.75f)
            return Mathf.Lerp(1.6f, 0.1f, (u - 0.4f) / 0.35f);
        return Mathf.Lerp(0.1f, 1f, (u - 0.75f) / 0.25f);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        // Cortar la animación si todavía se estaba moviendo (evita forcejeos).
        if (rutinaAparicion != null)
        {
            StopCoroutine(rutinaAparicion);
            rutinaAparicion = null;
        }
        transform.localScale = escalaOriginal;

        jugador = args.interactorObject.transform.GetComponentInParent<JugadorAsignadorTareas>();
        if (jugador != null)
            jugador.AgarrarTarea(grab, args.interactorObject.transform);

        // Notificar al OleadasManager si es la tarea tutorial
        if (EsTutorial && OleadasManager.Instancia != null)
            OleadasManager.Instancia.AgarrarTareaTutorial();
    }

    public void ActualizarTexto()
    {
        if (textoTarea != null)
            textoTarea.text = nombreTarea;

        ActualizarIcono();
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (jugador != null)
            jugador.SoltarTarea();

        jugador = null;
    }

    void Update()
    {
        // Seguridad: si cae fuera de la oficina o bajo el piso, se reintegra.
        if (transform.position.y < -2f)
        {
            GeneradorTareas gen = FindObjectOfType<GeneradorTareas>();
            if (gen != null) gen.ReintentarTarea();
            Destroy(gameObject);
        }
    }
}
