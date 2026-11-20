using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra vertical de cansancio (estilo "píldora") que se dibuja dentro del
/// canvas "barraCansancio" de cada compañero del LVL5.
///
/// Construye su propio UI en runtime (marco claro + fondo oscuro + relleno
/// naranja) imitando la "Barra de cansancio.svg". El relleno se rellena de
/// abajo hacia arriba según el valor de cansancio (0..1).
///
/// El componente se agrega automáticamente desde Compañero; no hace falta
/// configurarlo a mano, aunque sus valores públicos pueden ajustarse desde
/// el Inspector durante Play.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class BarraCansancio : MonoBehaviour
{
    [Header("Dimensiones")]
    [Tooltip("Ancho de la barra en unidades del canvas padre.")]
    [SerializeField] private float anchoBarra = 160f;

    [Tooltip("Alto de la barra en unidades del canvas padre.")]
    [SerializeField] private float altoBarra = 550f;

    [Tooltip("Posición (x, y) respecto al centro del canvas padre.")]
    [SerializeField] private Vector2 offset = new Vector2(390f, 546f);

    [Header("Colores (iguales a la .svg)")]
    [SerializeField] private Color colorMarco = new Color(0.851f, 0.851f, 0.851f, 1f);   // #D9D9D9
    [SerializeField] private Color colorFondo = new Color(0.612f, 0.612f, 0.612f, 0.756f); // #9C9C9C
    [SerializeField] private Color colorRelleno = new Color(1f, 0.545f, 0f, 0.756f);       // #FF8B00

    [Header("Inserción del relleno (fracción del tamaño)")]
    [SerializeField] private float insercionHorizontal = 0.08f;
    [SerializeField] private float insercionVertical = 0.04f;

    private Canvas canvas;
    private Image imagenRelleno;

    private void Awake()
    {
        Construir();
    }

    /// <summary>
    /// Configura dimensiones/posición antes o después de construir el UI.
    /// </summary>
    public void Configurar(float ancho, float alto, Vector2 posicion)
    {
        anchoBarra = ancho;
        altoBarra = alto;
        offset = posicion;
    }

    /// <summary>
    /// Actualiza el relleno de la barra. Recibe un valor 0..1
    /// (0 = vacía/cansado total, 1 = llena/descansado).
    /// </summary>
    public void SetProgreso(float fraccion01)
    {
        if (imagenRelleno == null) return;
        imagenRelleno.fillAmount = Mathf.Clamp01(fraccion01);
    }

    private void Construir()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Dibujar la barra por ENCIMA de las paredes/murales transparentes.
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        RectTransform rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(anchoBarra, altoBarra);
        rt.anchoredPosition = offset;
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0f);

        Sprite pildora = GenerarSpritePildora();

        // Marco exterior claro.
        CrearImagen("Marco", pildora, colorMarco, rt, Vector2.zero);

        // Fondo oscuro (la "pista" de la barra).
        Vector2 insercion = new Vector2(anchoBarra * insercionHorizontal, altoBarra * insercionVertical);
        CrearImagen("Fondo", pildora, colorFondo, rt, insercion);

        // Relleno naranja que sube de abajo hacia arriba.
        imagenRelleno = CrearImagen("Relleno", pildora, colorRelleno, rt, insercion);
        imagenRelleno.type = Image.Type.Filled;
        imagenRelleno.fillMethod = Image.FillMethod.Vertical;
        imagenRelleno.fillOrigin = (int)Image.OriginVertical.Bottom;
        imagenRelleno.fillClockwise = true;
        imagenRelleno.fillAmount = 1f;
    }

    private Image CrearImagen(string nombre, Sprite sprite, Color color, RectTransform padre, Vector2 insercion)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);

        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(insercion.x, insercion.y);
        rt.offsetMax = new Vector2(-insercion.x, -insercion.y);
        rt.localScale = Vector3.one;

        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.type = Image.Type.Simple;
        return img;
    }

    /// <summary>
    /// Genera una textura blanca con forma de píldora (rectángulo con los
    /// extremos redondeados) y la devuelve como Sprite reutilizable.
    /// </summary>
    private Sprite GenerarSpritePildora()
    {
        int w = Mathf.Max(16, Mathf.RoundToInt(anchoBarra));
        int h = Mathf.Max(16, Mathf.RoundToInt(altoBarra));

        const int maxRes = 512;
        if (w > maxRes || h > maxRes)
        {
            float escala = maxRes / (float)Mathf.Max(w, h);
            w = Mathf.Max(16, Mathf.RoundToInt(w * escala));
            h = Mathf.Max(16, Mathf.RoundToInt(h * escala));
        }

        float radio = w * 0.5f; // píldora completa

        Texture2D textura = new Texture2D(w, h, TextureFormat.RGBA32, false);
        textura.name = "BarraCansancio_Pildora";
        textura.wrapMode = TextureWrapMode.Clamp;

        Color32 blanco = new Color32(255, 255, 255, 255);
        Color32 transparente = new Color32(0, 0, 0, 0);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                textura.SetPixel(x, y, DentroDePildora(x, y, w, h, radio) ? blanco : transparente);
            }
        }

        textura.Apply();

        Sprite sprite = Sprite.Create(textura, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "BarraCansancio_Pildora";
        return sprite;
    }

    private bool DentroDePildora(int x, int y, int w, int h, float radio)
    {
        float px = x + 0.5f;
        float py = y + 0.5f;
        float cx = w * 0.5f;
        float cy = h * 0.5f;

        float dx = Mathf.Abs(px - cx) - (w * 0.5f - radio);
        float dy = Mathf.Abs(py - cy) - (h * 0.5f - radio);
        dx = Mathf.Max(0f, dx);
        dy = Mathf.Max(0f, dy);

        return (dx * dx + dy * dy) <= (radio * radio);
    }
}
