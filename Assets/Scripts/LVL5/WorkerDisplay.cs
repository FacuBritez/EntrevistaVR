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
///    en el mismo orden: Esperando, Trabajando, Enojado, Distraido.
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
