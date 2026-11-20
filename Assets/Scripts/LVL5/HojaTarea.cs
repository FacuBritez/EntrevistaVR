using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class HojaTarea : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoTarea;

    [Header("Icono de rol (mismo orden que el enum RolTarea)")]
    [SerializeField] private Sprite[] iconosRol;

    [Header("Fuente")]
    [SerializeField] private TMP_FontAsset fuenteTarea;

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
    private Coroutine rutinaDesintegracion;
    private Coroutine rutinaVolver;
    private Vector3 posicionBase;
    private Quaternion rotacionBase = Quaternion.Euler(90f, 0f, 0f);
    private SpriteRenderer iconoRenderer;

    // Colliders de todas las hojas, para que no colisionen entre sí.
    private static List<Collider> colisionadoresHojas = new List<Collider>();

    void Awake()
    {
        // IMPORTANTE: se captura acá (Awake corre apenas se instancia),
        // no en Start, porque GeneradorTareas llama a la animación en el mismo
        // frame y Start todavía no corrió (dejaba la escala en 0 = invisible).
        escalaOriginal = transform.localScale;

        CrearIconoRol();
        IgnorarColisionesEntreHojas();
    }

    /// <summary>
    /// Evita que las hojas colisionen entre sí (solo entre hojas); así al aparecer
    /// apiladas y mover una, no se empujan entre ellas ni traspasan el escenario.
    /// </summary>
    private void IgnorarColisionesEntreHojas()
    {
        Collider miCollider = GetComponent<Collider>();
        if (miCollider == null) return;

        foreach (var otro in colisionadoresHojas)
        {
            if (otro != null)
                Physics.IgnoreCollision(miCollider, otro, true);
        }
        colisionadoresHojas.Add(miCollider);
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
    /// Arma el display de la hoja según el diseño del SVG:
    /// título arriba, línea divisoria gris, y abajo una caja roja con el ícono del rol.
    /// </summary>
    private void CrearIconoRol()
    {
        var spriteBlanco = CrearSpriteSolido(Color.white);

        // Borde negro (detrás de la hoja, un poco más grande).
        var goBorde = new GameObject("Borde", typeof(SpriteRenderer));
        goBorde.transform.SetParent(transform, false);
        goBorde.transform.localPosition = new Vector3(0f, 0f, -0.6f);
        goBorde.transform.localScale = new Vector3(1.06f, 1.06f, 1f);
        var bordeRenderer = goBorde.GetComponent<SpriteRenderer>();
        bordeRenderer.sprite = spriteBlanco;
        bordeRenderer.color = Color.black;
        bordeRenderer.sortingOrder = -1;

        // Línea divisoria (gris).
        var goDivisor = new GameObject("Divisor", typeof(SpriteRenderer));
        goDivisor.transform.SetParent(transform, false);
        goDivisor.transform.localPosition = new Vector3(0f, 0.09f, -0.555f);
        goDivisor.transform.localScale = new Vector3(0.85f, 0.02f, 1f);
        var divisorRenderer = goDivisor.GetComponent<SpriteRenderer>();
        divisorRenderer.sprite = spriteBlanco;
        divisorRenderer.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        divisorRenderer.sortingOrder = 1;

        // Caja roja (fondo del ícono).
        var goCaja = new GameObject("CajaIcono", typeof(SpriteRenderer));
        goCaja.transform.SetParent(transform, false);
        goCaja.transform.localPosition = new Vector3(0f, -0.19f, -0.555f);
        goCaja.transform.localScale = new Vector3(0.6f, 0.41f, 1f);
        var cajaRenderer = goCaja.GetComponent<SpriteRenderer>();
        cajaRenderer.sprite = spriteBlanco;
        cajaRenderer.color = Color.red;
        cajaRenderer.sortingOrder = 0;

        // Ícono del rol (dentro de la caja roja).
        var go = new GameObject("IconoRol", typeof(SpriteRenderer));
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, -0.19f, -0.55f);
        go.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        iconoRenderer = go.GetComponent<SpriteRenderer>();
        iconoRenderer.sortingOrder = 2;

        // Texto (título) arriba.
        if (textoTarea != null)
        {
            var pos = textoTarea.transform.localPosition;
            pos.y = 0.3f;
            textoTarea.transform.localPosition = pos;
        }
    }

    /// <summary>
    /// Genera un sprite sólido de 1x1 píxel del color indicado (para el borde,
    /// el divisor y la caja).
    /// </summary>
    private Sprite CrearSpriteSolido(Color color)
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
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
        // Guardar la posición base a la que debe volver si se suelta sin asignar.
        posicionBase = destino;

        if (rutinaAparicion != null) StopCoroutine(rutinaAparicion);
        rutinaAparicion = StartCoroutine(AnimacionAparicion(destino));
    }

    /// <summary>
    /// Vuelve a la posición donde quedó después de spawnear (la base de acumulación).
    /// Se llama al soltar la hoja sin asignarla.
    /// </summary>
    public void VolverAPosicion()
    {
        if (rutinaVolver != null) StopCoroutine(rutinaVolver);
        rutinaVolver = StartCoroutine(AnimacionVolver());
    }

    private IEnumerator AnimacionVolver()
    {
        // Deshabilitar el agarre mientras vuelve.
        if (grab != null) grab.enabled = false;

        // Congelar la física para que no siga girando/volando por el "throw" del grab.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        float duracion = 0.5f;
        float t = 0f;
        Vector3 posInicio = transform.position;
        Quaternion rotInicio = transform.rotation;
        Quaternion rotFinal = Quaternion.Euler(90f, 0f, 0f);

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            float u = Mathf.Clamp01(t);
            transform.position = Vector3.Lerp(posInicio, posicionBase, u);
            transform.rotation = Quaternion.Slerp(rotInicio, rotFinal, u);
            yield return null;
        }

        transform.position = posicionBase;
        transform.rotation = rotFinal;
        transform.localScale = escalaOriginal;

        // Devolver la física a la normalidad.
        if (rb != null)
            rb.isKinematic = false;

        if (grab != null) grab.enabled = true;
        rutinaVolver = null;
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

    /// <summary>
    /// En vez de desaparecer de golpe, la hoja se "desintegra" en el lugar:
    /// se encoge y se desvanece, y recién al final se destruye.
    /// </summary>
    public void Desintegrar()
    {
        if (grab != null)
            grab.enabled = false;

        if (rutinaDesintegracion != null) StopCoroutine(rutinaDesintegracion);
        rutinaDesintegracion = StartCoroutine(AnimacionDesintegracion());
    }

    private IEnumerator AnimacionDesintegracion()
    {
        float duracion = 0.6f;
        float t = 0f;
        Vector3 escalaInicial = transform.localScale;

        var sprites = GetComponentsInChildren<SpriteRenderer>();
        var textos = GetComponentsInChildren<TMP_Text>();

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            float u = Mathf.Clamp01(t);

            // Se encoge.
            transform.localScale = escalaInicial * (1f - u * 0.85f);

            // Se desvanece.
            float alpha = 1f - u;
            foreach (var s in sprites)
            {
                Color c = s.color;
                c.a = alpha;
                s.color = c;
            }
            foreach (var txt in textos)
            {
                Color c = txt.color;
                c.a = alpha;
                txt.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
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
