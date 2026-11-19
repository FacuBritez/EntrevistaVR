using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla el display visual de cada trabajador (Compañero).
///
/// Layout esperado en el Canvas hijo del trabajador:
///  ┌─────────────────────────┐
///  │   [Image: IconoEstado]  │   ← Sprite del estado actual
///  │  [Image: IconoTrabajad] │   ← Sprite del tipo de trabajador
///  │   [TMP_Text: Estado]    │   ← Texto del estado actual
///  └─────────────────────────┘
///
/// Instrucciones de setup en el Inspector:
/// 1. Asignar las referencias a imgIconoEstado, imgIconoTrabajador y txtEstado.
/// 2. Expandir "Iconos de Estado" y asignar un sprite por cada valor del enum Estadocompañero
///    en el mismo orden: Esperando, Trabajando, Enojado, Distraido, Dormido.
/// 3. Expandir "Iconos de Trabajador" y asignar un sprite por cada valor del enum RolTarea
///    en el mismo orden: Programador, Artista, Contable, Musico, Marketing.
/// </summary>
public class WorkerDisplay : MonoBehaviour
{
    // ─── Referencias UI ──────────────────────────────────────────────────────
    [Header("Referencias UI")]
    [Tooltip("Image que muestra el ícono del ESTADO actual del trabajador (p.ej. Esperando, Trabajando…)")]
    [SerializeField] private Image imgIconoEstado;

    [Tooltip("Image que muestra el ícono del TIPO de trabajador (p.ej. Programador, Artista…)")]
    [SerializeField] private Image imgIconoTrabajador;

    [Tooltip("Texto que muestra el nombre del estado actual")]
    [SerializeField] private TMP_Text txtEstado;

    // ─── Sprites de estado ────────────────────────────────────────────────────
    [Header("Iconos de Estado  (mismo orden que el enum Estadocompañero)")]
    [Tooltip("Índice 0 = Esperando")]
    [SerializeField] private Sprite iconoEsperando;
    [Tooltip("Índice 1 = Trabajando")]
    [SerializeField] private Sprite iconoTrabajando;
    [Tooltip("Índice 2 = Enojado")]
    [SerializeField] private Sprite iconoEnojado;
    [Tooltip("Índice 3 = Distraido")]
    [SerializeField] private Sprite iconoDistraido;
    [Tooltip("Índice 4 = Dormido")]
    [SerializeField] private Sprite iconoDormido;

    // ─── Sprites de trabajador ────────────────────────────────────────────────
    [Header("Iconos de Trabajador  (mismo orden que el enum RolTarea)")]
    [Tooltip("Índice 0 = Programador")]
    [SerializeField] private Sprite iconoProgramador;
    [Tooltip("Índice 1 = Artista")]
    [SerializeField] private Sprite iconoArtista;
    [Tooltip("Índice 2 = Contable")]
    [SerializeField] private Sprite iconoContable;
    [Tooltip("Índice 3 = Musico")]
    [SerializeField] private Sprite iconoMusico;
    [Tooltip("Índice 4 = Marketing")]
    [SerializeField] private Sprite iconoMarketing;

    // ─── Colores opcionales por estado ───────────────────────────────────────
    [Header("Color de fondo del ícono de estado (opcional)")]
    [SerializeField] private Color colorEsperando  = Color.gray;
    [SerializeField] private Color colorTrabajando = Color.green;
    [SerializeField] private Color colorEnojado    = Color.red;
    [SerializeField] private Color colorDistraido  = Color.yellow;
    [SerializeField] private Color colorDormido    = new Color(0.45f, 0.55f, 0.75f, 1f);

        // ═══════════════════════════════════════════════════════════════════════
    //  VISUALIZACIÓN VR: Billboard + "translateZ" hacia la cámara (CORREGIDO)
    //  La etiqueta sigue al trabajador y se empuja hacia el jugador para que
    //  NUNCA quede en el mismo plano que la pared del fondo (adiós traspaso).
    // ═══════════════════════════════════════════════════════════════════════
    [Header("Configuración de Visualización VR")]
    [Tooltip("ON: empuja la etiqueta hacia la cámara para que no se mezcle con el fondo.")]
    [SerializeField] private bool separarDelFondo = true;

    [Tooltip("Cuántos metros hacia el jugador se desplaza la etiqueta. Aumentalo si la pared sigue peleando.")]
    [SerializeField] private float separacionFondo = 0.5f;

    private Transform cameraTransform;
    private Vector3 localOffset; // la posición que el editor ya dejó arriba de la cabeza

    void Start()
    {
        cameraTransform = Camera.main ? Camera.main.transform : null;

        // Fuerza que esta UI se dibuje por ENCIMA de las paredes/murales del
        // escenario (que usan materiales transparentes con el mismo render queue).
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }

        // Guardamos la posición LOCAL respecto al trabajador (lo que vos pusiste en el Editor).
        // Es el equivalente a "leer el layout estático antes de mutarlo en runtime".
        localOffset = transform.localPosition;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 1) Billboard: apuntamos el +Z HACIA la cámara (la cara legible de un Canvas es +Z).
        //    OJO: NO usar "transform.rotation = cameraTransform.rotation": eso apunta el +Z
        //    en la MISMA dirección de la cámara (alejándolo del jugador) y deja el texto espejado.
        Vector3 direccionHaciaCamara = cameraTransform.position - transform.position;
        direccionHaciaCamara.y = 0f; // billboard vertical: sin inclinación arriba/abajo

        if (direccionHaciaCamara.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direccionHaciaCamara);

        // 2) Si pediste separar del fondo, recalculamos la posición cada frame.
        if (separarDelFondo)
        {
            // "natural" = dónde estaría la etiqueta arriba de la cabeza del trabajador.
            // TransformPoint convierte la posición LOCAL (que respetó el editor) a coordenadas
            // de MUNDO, teniendo en cuenta la escala/rotación del padre. Por eso NO usamos
            // parent.position + offset a pelo: eso se rompe si el modelo está escalado.
            Vector3 natural = transform.parent != null
                ? transform.parent.TransformPoint(localOffset)
                : transform.position;

            // Dirección unitaria desde la etiqueta hacia la cámara (el "hacia el usuario").
            Vector3 haciaCamara = (cameraTransform.position - natural).normalized;

            // translateZ(+separacionFondo): la colocamos SIEMPRE más cerca que la pared.
            transform.position = natural + haciaCamara * separacionFondo;
        }
        // Si separarDelFondo = false → no tocamos position → el Editor manda (como antes).
    }
    // ─────────────────────────────────────────────────────────────────────────
    //  Inicialización del ícono de trabajador (se llama una vez desde Compañero)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Establece el ícono de tipo de trabajador. Llamar una vez en Start desde Compañero.
    /// </summary>
    public void InicializarRol(RolTarea rol)
    {
        if (imgIconoTrabajador == null) return;
        imgIconoTrabajador.sprite = ObtenerSpriteRol(rol);
        // Ocultar si no hay sprite asignado
        imgIconoTrabajador.enabled = imgIconoTrabajador.sprite != null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Actualización del display (llamar cada vez que cambia el estado)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Actualiza el ícono de estado, el color de fondo y el texto.
    /// Llamar desde Compañero al cambiar de estado o para mostrar progreso.
    /// </summary>
    public void ActualizarEstado(Estadocompañero estado, string textoEstado = null)
    {
        // --- Ícono de estado ---
        if (imgIconoEstado != null)
        {
            Sprite sprite = ObtenerSpriteEstado(estado);
            imgIconoEstado.sprite  = sprite;
            imgIconoEstado.enabled = sprite != null;
            imgIconoEstado.color   = ObtenerColorEstado(estado);
        }

        // --- Texto ---
        if (txtEstado != null)
        {
            txtEstado.text = textoEstado ?? estado.ToString();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers privados
    // ─────────────────────────────────────────────────────────────────────────

    private Sprite ObtenerSpriteEstado(Estadocompañero estado)
    {
        switch (estado)
        {
            case Estadocompañero.Esperando:  return iconoEsperando;
            case Estadocompañero.Trabajando: return iconoTrabajando;
            case Estadocompañero.Enojado:    return iconoEnojado;
            case Estadocompañero.Distraido:  return iconoDistraido;
            case Estadocompañero.Dormido:    return iconoDormido;
            default:                         return null;
        }
    }

    private Color ObtenerColorEstado(Estadocompañero estado)
    {
        switch (estado)
        {
            case Estadocompañero.Esperando:  return colorEsperando;
            case Estadocompañero.Trabajando: return colorTrabajando;
            case Estadocompañero.Enojado:    return colorEnojado;
            case Estadocompañero.Distraido:  return colorDistraido;
            case Estadocompañero.Dormido:    return colorDormido;
            default:                         return Color.white;
        }
    }

    private Sprite ObtenerSpriteRol(RolTarea rol)
    {
        switch (rol)
        {
            case RolTarea.Programador: return iconoProgramador;
            case RolTarea.Artista:     return iconoArtista;
            case RolTarea.Contable:    return iconoContable;
            case RolTarea.Musico:      return iconoMusico;
            case RolTarea.Marketing:   return iconoMarketing;
            default:                   return null;
        }
    }
}