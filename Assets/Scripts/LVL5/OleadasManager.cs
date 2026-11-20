using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OleadasManager : MonoBehaviour
{
    [System.Serializable]
    public class ConfiguracionEntrega
    {
        public int cantidadTareas = 10;
        public float tiempoEntrega = 30f;
    }

    [Header("Configuración de Entregas")]
    [SerializeField] private ConfiguracionEntrega entrega1;
    [SerializeField] private ConfiguracionEntrega entrega2;
    [SerializeField] private ConfiguracionEntrega entrega3;

    [Header("UI")]
    [SerializeField] private TMP_Text textoEntrega;
    [SerializeField] private TMP_Text textoMensaje;
    [SerializeField] private Image circuloProgreso;

    [Header("Tiempos de mensajes")]
    [SerializeField] private float duracionMensajeEntregaCompletada = 2f;
    [SerializeField] private float duracionMensajeProximaEntrega = 2f;

    [Header("Generador")]
    [SerializeField] private GeneradorTareas generadorTareas;

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoEntregaCompletada;
    [SerializeField] private AudioClip sonidoEntregaFallida;
    [SerializeField] private AudioClip sonidoJuegoGanado;

    [Header("Alerta de tiempo")]
    [Tooltip("Luz 'LuzAlerta' que se enciende cuando queda poco tiempo.")]
    [SerializeField] private Light luzAlerta;
    [Tooltip("Sonido de alarma (se puede cambiar desde el Inspector). Suena en bucle.")]
    [SerializeField] private AudioClip sonidoAlarma;
    [Tooltip("Segundos restantes para activar la alerta (luz + alarma).")]
    [SerializeField] private float umbralAlerta = 20f;

    [Header("Dinero / Puntaje")]
    [Tooltip("Dinero con el que empieza la partida.")]
    [SerializeField] private int dineroInicial = 10000;
    [SerializeField] private int dineroCorrecta = 5000;
    [SerializeField] private int dineroIncorrecta = -3500;
    [SerializeField] private int dineroDormido = -8000;
    [Tooltip("TMP_Text de un Canvas (world-space) en el escenario donde se muestra el dinero.")]
    [SerializeField] private TMP_Text textoDinero;

    [Header("Recreo")]
    [Tooltip("Animación de recreo global, usada por todos los compañeros.")]
    [SerializeField] private AnimationClip animacionRecreoGlobal;

    [Header("Tutorial")]
    [SerializeField] private GameObject prefabTareaTutorial;   // El prefab de la tarea (mismo que usa GeneradorTareas)
    [SerializeField] private Transform puntoSpawnTutorial;     // Donde aparece la tarea (mismo que GeneradorTareas)
    [SerializeField] private GameObject programador;           // El compañero correcto
    [SerializeField] private GameObject[] todosLosCompanieros; // Todos los compañeros (incluido programador)

    [Header("Tutorial HUD (VR)")]
    [SerializeField] private float distanciaHUD = 1.5f;
    [SerializeField] private float alturaHUD = 0f;
    [SerializeField] private Vector2 tamanoHUD = new Vector2(1200f, 180f);

    [Header("Final")]
    [SerializeField] private GameObject cartelGanaste;
    [SerializeField] private GameObject cartelPerdiste;

    private int numeroEntrega = 0;
    private float tiempoRestante;
    private float tiempoTotalEntrega;

    private int tareasAsignadas;
    private int tareasCorrectas;
    private int tareasIncorrectas;
    private int tareasCompletadas;

    private bool entregaActiva = false;
    private bool esperandoProximaEntrega = false;
    private bool bloqueoColorTiempo = false; // evita que Update pise el color durante el efecto de error
    private Coroutine rutinaErrorTiempo;
    private bool alertaActiva = false; // la alarma de tiempo está sonando

    // Dinero / puntaje
    private int dinero = 0;
    // Tutorial
    private enum TutorialState { EsperandoAgarrar, EsperandoAsignar, EsperandoRecreo, Completado }
    private TutorialState tutorialState = TutorialState.EsperandoAgarrar;
    private bool tutorialActivo = true;
    private GameObject tareaTutorialInstancia;  // la instancia actual

    // Tutorial: UI (HUD) y efectos visuales
    private Canvas canvasTutorial;
    private TextMeshProUGUI textoTutorial;
    private GameObject panelTutorial;
    private GameObject flechaRoja;
    private SpriteRenderer flechaRenderer;
    private Transform objetivoResaltado;
    private Vector3 escalaOriginalResaltado;
    private Coroutine rutinaPulso;
    private Coroutine rutinaFlecha;

    // Tutorial: resplandor (glow) del objeto resaltado
    private GameObject resplandor;
    private SpriteRenderer resplandorRenderer;
    private Vector3 centroResplandor;

    public static OleadasManager Instancia { get; private set; }

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        // Asegurar un AudioSource para la alarma y los sonidos.
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Dinero inicial.
        dinero = dineroInicial;
        ActualizarTextoDinero();

        // Compartir la animación de recreo global con todos los compañeros.
        Compañero.AnimacionRecreoGlobal = animacionRecreoGlobal;

        IniciarTutorial();
    }

    private void Update()
    {
        if (tutorialActivo)
        {
            // Durante el tutorial, esperar a que el jugador mande al programador a recreo.
            if (tutorialState == TutorialState.EsperandoRecreo)
            {
                Compañero comp = (programador != null) ? programador.GetComponent<Compañero>() : null;
                if (comp != null && comp.EstaEnRecreo)
                {
                    tutorialState = TutorialState.Completado;
                    tutorialActivo = false;
                    DejarDeResaltar();
                    OcultarFlecha();
                    StartCoroutine(SecuenciaTutorial());
                }
            }
            return;
        }

        if (!entregaActiva)
            return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante < 0f)
            tiempoRestante = 0f;

        ActualizarCirculoProgresoTiempo();

        // Alerta: cuando queda poco tiempo, encender la luz y sonar la alarma.
        if (tiempoRestante <= umbralAlerta && tiempoRestante > 0f)
        {
            ActivarAlerta();
        }

        // Si ya se asignaron todas las tareas pero todavía
        // hay compañeros trabajando, mostramos el mensaje.
        if (tareasAsignadas >= ObtenerConfiguracionActual().cantidadTareas &&
            TareasResueltas < ObtenerConfiguracionActual().cantidadTareas)
        {
            MostrarMensajeEsperando();
        }

        // Si se acaba el tiempo, se pierde.
        if (tiempoRestante <= 0f)
        {
            FinalizarEntregaPorTiempo();
            return;
        }

        // Si todas las tareas fueron resueltas (terminadas o rechazadas), se completa.
        if (TareasResueltas >= ObtenerConfiguracionActual().cantidadTareas)
        {
            CompletarEntrega();
        }
    }

    // ---------------------------------------------------------
    // TUTORIAL
    // ---------------------------------------------------------

    private void IniciarTutorial()
    {
        tutorialActivo = true;
        tutorialState = TutorialState.EsperandoAgarrar;

        if (generadorTareas != null)
            generadorTareas.enabled = false;

        foreach (var c in todosLosCompanieros)
        {
            if (c != programador)
                c.SetActive(false);
        }
        if (programador != null)
            programador.SetActive(true);

        // Ocultar la UI diegética (feedback del escenario) mientras dura el tutorial.
        if (textoMensaje != null) textoMensaje.gameObject.SetActive(false);
        if (textoEntrega != null) textoEntrega.gameObject.SetActive(false);
        if (circuloProgreso != null) circuloProgreso.gameObject.SetActive(false);

        // Crear el HUD del tutorial (sin efecto de blanco y negro; se usa resplandor).
        CrearCanvasTutorial();

        StartCoroutine(SecuenciaInicioTutorial());
    }

    private IEnumerator SecuenciaInicioTutorial()
    {
        MostrarDialogo("¡Es tu primer día de pasantía!");
        yield return new WaitForSeconds(6f);

        MostrarDialogo("Asigna las tareas que lleguen a los roles.");
        yield return new WaitForSeconds(5f);

        MostrarDialogo("Mira la pizarra de tu izquierda para ver sus colores.");
        yield return new WaitForSeconds(5f);

        CrearTareaTutorial();
        ResaltarObjeto(tareaTutorialInstancia);
        MostrarDialogo("Para agarrar la tarea acercá la mano y mantené apretado el botón de agarre (lateral).");
    }

    private void CrearTareaTutorial()
    {
        if (prefabTareaTutorial == null || puntoSpawnTutorial == null)
        {
            Debug.LogError("Faltan referencias para la tarea tutorial (prefab o punto de spawn).");
            return;
        }

        // Destruir la anterior si existe
        if (tareaTutorialInstancia != null)
            Destroy(tareaTutorialInstancia);

        tareaTutorialInstancia = Instantiate(prefabTareaTutorial, puntoSpawnTutorial.position, puntoSpawnTutorial.rotation);
        HojaTarea ht = tareaTutorialInstancia.GetComponent<HojaTarea>();
        if (ht != null)
        {
            ht.nombreTarea = "Corregir error de código";
            ht.rolRequerido = RolTarea.Programador;
            ht.duracionBase = 5f;
            ht.ActualizarTexto();
            ht.EsTutorial = true; // para que no interfiera con la lógica normal
        }
    }

    public void AgarrarTareaTutorial()
    {
        if (tutorialActivo && tutorialState == TutorialState.EsperandoAgarrar)
        {
            tutorialState = TutorialState.EsperandoAsignar;
            DejarDeResaltar();
            ResaltarObjeto(programador);
            MostrarFlecha(programador != null ? programador.transform : null);
            MostrarDialogo("La tarea es para el programador. Apunta a él y suelta la tarea.");
        }
    }

    public void SoltarTareaTutorial(bool acerto)
    {
        if (tutorialActivo && tutorialState == TutorialState.EsperandoAsignar && acerto)
        {
            tutorialState = TutorialState.EsperandoRecreo;
            DejarDeResaltar();
            OcultarFlecha();
            ResaltarObjeto(programador);
            MostrarDialogo("Con la pistola podés enviarlo a recrearse para que recuperen su barra de energía. Si se les acaba, se duermen por mucho tiempo");
        }
    }

    private IEnumerator SecuenciaTutorial()
    {
        MostrarDialogo("¡Perfecto! Ahora empieza el trabajo real.");
        yield return new WaitForSeconds(3f);
        MostrarDialogo("Recuerda: lee bien la tarea y asígnala a quien corresponda.");
        yield return new WaitForSeconds(3f);
        StartCoroutine(FinalizarTutorial());
    }

    private IEnumerator FinalizarTutorial()
    {
        yield return new WaitForSeconds(2f);

        // Reactivar todos los compañeros
        foreach (var c in todosLosCompanieros)
        {
            c.SetActive(true);
        }

        // Destruir la tarea tutorial
        if (tareaTutorialInstancia != null)
            Destroy(tareaTutorialInstancia);

        // Reactivar el generador de tareas
        if (generadorTareas != null)
            generadorTareas.enabled = true;

        // Quitar el HUD, la flecha y restaurar los colores.
        LimpiarTutorial();

        LimpiarMensaje();
        IniciarSiguienteEntrega();
    }

    // ---------------------------------------------------------
    // UI DEL TUTORIAL (HUD) + EFECTOS VISUALES
    // ---------------------------------------------------------

    private void MostrarDialogo(string mensaje)
    {
        if (textoTutorial != null)
            textoTutorial.text = mensaje;
    }

    /// <summary>
    /// Crea el HUD del tutorial: un Canvas en Screen Space Overlay (interfaz de usuario),
    /// NO un cartel del escenario. Muestra un panel con el texto del diálogo.
    /// </summary>
    private void CrearCanvasTutorial()
    {
        if (canvasTutorial != null) return;

        Camera camara = Camera.main;
        if (camara == null)
        {
            Debug.LogError("No hay cámara con tag MainCamera.");
            return;
        }

        var goCanvas = new GameObject("CanvasTutorial", typeof(RectTransform));
        goCanvas.transform.SetParent(camara.transform, false);
        goCanvas.transform.localPosition = new Vector3(0f, alturaHUD, distanciaHUD);
        goCanvas.transform.localRotation = Quaternion.identity;
        goCanvas.transform.localScale = Vector3.one * 0.001f;

        canvasTutorial = goCanvas.AddComponent<Canvas>();
        canvasTutorial.renderMode = RenderMode.WorldSpace;
        canvasTutorial.worldCamera = camara;

        // Mostrar el HUD del tutorial por encima de todo (paredes/murales, UI de compañeros, etc.).
        canvasTutorial.overrideSorting = true;
        canvasTutorial.sortingOrder = 200;

        var canvasRect = goCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = tamanoHUD;

        // Panel: ahora ocupa TODO el canvas
        var goPanel = new GameObject("PanelTutorial", typeof(RectTransform), typeof(Image));
        goPanel.transform.SetParent(goCanvas.transform, false);
        goPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        var panelRect = goPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelTutorial = goPanel;

        // Texto (igual que antes)
        var goTexto = new GameObject("TextoTutorial", typeof(RectTransform), typeof(TextMeshProUGUI));
        goTexto.transform.SetParent(goPanel.transform, false);
        textoTutorial = goTexto.GetComponent<TextMeshProUGUI>();
        textoTutorial.font = TMP_Settings.defaultFontAsset;
        textoTutorial.fontSize = 40;
        textoTutorial.alignment = TextAlignmentOptions.Center;
        textoTutorial.color = Color.white;
        var textRect = goTexto.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(40f, 30f);
        textRect.offsetMax = new Vector2(-40f, -30f);
        textoTutorial.text = "";
    }

    // ---------------------------------------------------------
    // RESALTADO (pulso de escala leve + resplandor)
    // ---------------------------------------------------------

    private void ResaltarObjeto(GameObject obj)
    {
        DejarDeResaltar();
        if (obj == null) return;

        objetivoResaltado = obj.transform;
        escalaOriginalResaltado = objetivoResaltado.localScale;
        CrearResplandor(obj);
        rutinaPulso = StartCoroutine(Pulso(objetivoResaltado));
    }

    private IEnumerator Pulso(Transform target)
    {
        float t = 0f;
        while (target != null && target == objetivoResaltado)
        {
            t += Time.deltaTime * 4f;
            // 75% más leve que antes (era 0.18 -> ahora 0.045).
            float factor = 1f + Mathf.Sin(t) * 0.045f;
            target.localScale = escalaOriginalResaltado * factor;

            // Resplandor: sigue al objeto, hace billboard y pulsa el alpha.
            if (resplandor != null)
            {
                resplandor.transform.position = centroResplandor;

                if (Camera.main != null)
                {
                    Vector3 dir = resplandor.transform.position - Camera.main.transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                        resplandor.transform.rotation = Quaternion.LookRotation(dir);
                }

                if (resplandorRenderer != null)
                {
                    float alpha = 0.45f + Mathf.Sin(t) * 0.2f;
                    Color c = resplandorRenderer.color;
                    c.a = Mathf.Clamp01(alpha);
                    resplandorRenderer.color = c;
                }
            }

            yield return null;
        }
    }

    private void DejarDeResaltar()
    {
        if (objetivoResaltado != null)
            objetivoResaltado.localScale = escalaOriginalResaltado;
        objetivoResaltado = null;
        if (rutinaPulso != null)
        {
            StopCoroutine(rutinaPulso);
            rutinaPulso = null;
        }
        QuitarResplandor();
    }

    private void CrearResplandor(GameObject obj)
    {
        QuitarResplandor();
        if (obj == null) return;

        var go = new GameObject("Resplandor");
        resplandorRenderer = go.AddComponent<SpriteRenderer>();
        resplandorRenderer.sprite = CrearSpriteResplandor();
        resplandorRenderer.color = new Color(1f, 0.85f, 0.25f, 0.5f); // amarillo suave
        resplandorRenderer.sortingOrder = -50; // se dibuja detrás
        resplandor = go;

        // Tamaño proporcional al objeto.
        float tam = 1f;
        Bounds b = new Bounds(obj.transform.position, Vector3.zero);
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            foreach (var r in renderers)
                b.Encapsulate(r.bounds);
            tam = Mathf.Max(b.size.x, b.size.y, b.size.z) * 1.5f;
        }
        centroResplandor = b.center;

        resplandor.transform.localScale = Vector3.one * tam;
        resplandor.transform.position = centroResplandor;
    }

    private void QuitarResplandor()
    {
        if (resplandor != null) { Destroy(resplandor); resplandor = null; }
        resplandorRenderer = null;
    }

    /// <summary>
    /// Genera una textura radial suave (glow) que se desvanece hacia los bordes.
    /// </summary>
    private Sprite CrearSpriteResplandor()
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha; // falloff suave
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ---------------------------------------------------------
    // FLECHA ROJA (señala al trabajador)
    // ---------------------------------------------------------

    private void MostrarFlecha(Transform objetivo)
    {
        if (objetivo == null) return;
        if (flechaRoja == null)
            CrearFlecha();

        flechaRoja.SetActive(true);
        if (rutinaFlecha != null) StopCoroutine(rutinaFlecha);
        rutinaFlecha = StartCoroutine(SeguirFlecha(objetivo));
    }

    private void CrearFlecha()
    {
        var go = new GameObject("FlechaRoja");
        flechaRenderer = go.AddComponent<SpriteRenderer>();
        flechaRenderer.sprite = CrearSpriteFlecha();
        flechaRenderer.color = Color.red;
        flechaRenderer.sortingOrder = 200;
        go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
        flechaRoja = go;
    }

    /// <summary>
    /// Dibuja en runtime una textura de flecha roja apuntando hacia abajo.
    /// </summary>
    private Sprite CrearSpriteFlecha()
    {
        int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0, 0, 0, 0);
        Color red = Color.red;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

        int cx = size / 2;

        // Asta (rectángulo vertical, mitad superior)
        for (int y = size / 2; y < size; y++)
            for (int x = cx - 5; x <= cx + 5; x++)
                tex.SetPixel(x, y, red);

        // Punta (triángulo hacia abajo, mitad inferior)
        for (int y = 0; y <= size / 2; y++)
        {
            float t = y / (float)(size / 2); // 0 en el ápice, 1 en la base
            int half = Mathf.RoundToInt(t * (size / 2));
            for (int x = cx - half; x <= cx + half; x++)
                tex.SetPixel(x, y, red);
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private IEnumerator SeguirFlecha(Transform objetivo)
    {
        Transform cam = Camera.main ? Camera.main.transform : null;
        while (objetivo != null && flechaRoja != null && flechaRoja.activeSelf)
        {
            Vector3 pos = objetivo.position + Vector3.up * 2.0f;
            pos.y += Mathf.Sin(Time.time * 3f) * 0.15f; // flota arriba/abajo
            flechaRoja.transform.position = pos;

            // Billboard vertical (solo Y) para que la flecha mire de frente al jugador.
            if (cam != null)
            {
                Vector3 dir = pos - cam.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    flechaRoja.transform.rotation = Quaternion.LookRotation(dir);
            }
            yield return null;
        }
    }

    private void OcultarFlecha()
    {
        if (rutinaFlecha != null)
        {
            StopCoroutine(rutinaFlecha);
            rutinaFlecha = null;
        }
        if (flechaRoja != null)
            flechaRoja.SetActive(false);
    }

    // ---------------------------------------------------------
    // LIMPIEZA DEL TUTORIAL
    // ---------------------------------------------------------

    private void LimpiarTutorial()
    {
        DejarDeResaltar();
        OcultarFlecha();

        if (canvasTutorial != null) { Destroy(canvasTutorial.gameObject); canvasTutorial = null; }
        if (flechaRoja != null) { Destroy(flechaRoja); flechaRoja = null; }

        textoTutorial = null;
        panelTutorial = null;
        flechaRenderer = null;
        objetivoResaltado = null;
    }

    // ---------------------------------------------------------
    // INICIO DE ENTREGA
    // ---------------------------------------------------------

    private void IniciarSiguienteEntrega()
    {
        numeroEntrega++;

        if (numeroEntrega > 3)
        {
            GanarJuego();
            return;
        }

        ConfiguracionEntrega config = ObtenerConfiguracionActual();

        tareasAsignadas = 0;
        tareasCorrectas = 0;
        tareasIncorrectas = 0;
        tareasCompletadas = 0;

        tiempoRestante = config.tiempoEntrega;
        tiempoTotalEntrega = config.tiempoEntrega;
        entregaActiva = true;
        esperandoProximaEntrega = false;

        // Apagar la alerta de la entrega anterior.
        DesactivarAlerta();

        // Reiniciar círculo de progreso (lleno al 100% y color blanco)
        ActualizarCirculoProgresoTiempo();

        if (generadorTareas != null)
        {
            generadorTareas.IniciarOleada(config.cantidadTareas);
        }

        if (textoEntrega != null)
        {
            textoEntrega.text = $"Entrega {numeroEntrega}";
        }

        LimpiarMensaje();

        Debug.Log(
            $"ENTREGA {numeroEntrega} - " +
            $"{config.cantidadTareas} tareas - " +
            $"{config.tiempoEntrega} segundos"
        );
    }

    private ConfiguracionEntrega ObtenerConfiguracionActual()
    {
        switch (numeroEntrega)
        {
            case 1: return entrega1;
            case 2: return entrega2;
            case 3: return entrega3;
            default: return entrega1;
        }
    }

    /// <summary>
    /// Tareas ya resueltas: las completadas correctamente más las rechazadas
    /// (entregadas a un rol equivocado). La entrega se completa cuando todas
    /// las tareas de la oleada están resueltas, no solo las correctas.
    /// </summary>
    private int TareasResueltas => tareasCompletadas + tareasIncorrectas;

    // ---------------------------------------------------------
    // CÍRCULO DE PROGRESO (TIEMPO RESTANTE + COLOR)
    // ---------------------------------------------------------

    private void ActualizarCirculoProgresoTiempo()
    {
        if (circuloProgreso == null)
            return;

        if (tiempoTotalEntrega <= 0)
        {
            circuloProgreso.fillAmount = 0f;
            circuloProgreso.color = Color.red;
            return;
        }

        float progreso = tiempoRestante / tiempoTotalEntrega;
        circuloProgreso.fillAmount = progreso;

        // Durante el efecto de error no pisamos el color (parpadea en rojo).
        if (bloqueoColorTiempo)
            return;

        float t = 1f - Mathf.Pow(progreso, 0.5f); // raíz cuadrada para efecto de easing

        circuloProgreso.color = Color.Lerp(Color.white, Color.red, t);
    }

    /// <summary>
    /// Enciende la luz de alerta y arranca la alarma en bucle (si hay clip).
    /// </summary>
    private void ActivarAlerta()
    {
        if (alertaActiva) return;
        alertaActiva = true;

        if (luzAlerta != null)
            luzAlerta.gameObject.SetActive(true);

        if (audioSource != null && sonidoAlarma != null)
        {
            audioSource.loop = true;
            audioSource.clip = sonidoAlarma;
            audioSource.Play();
        }

        Debug.Log("🚨 ¡Alerta! Queda poco tiempo.");
    }

    /// <summary>
    /// Apaga la luz de alerta y detiene la alarma.
    /// </summary>
    private void DesactivarAlerta()
    {
        if (!alertaActiva)
        {
            // Asegurar que la luz quede apagada aunque la alarma no haya sonado.
            if (luzAlerta != null && luzAlerta.gameObject.activeSelf)
                luzAlerta.gameObject.SetActive(false);
            return;
        }

        alertaActiva = false;

        if (luzAlerta != null)
            luzAlerta.gameObject.SetActive(false);

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    /// <summary>
    /// Penaliza una entrega incorrecta: descuenta 10% del tiempo total y
    /// dispara el efecto de error en la pantalla de tiempo.
    /// </summary>
    private void AplicarPenalizacionIncorrecta()
    {
        tiempoRestante = Mathf.Max(0f, tiempoRestante - tiempoTotalEntrega * 0.10f);
        ActualizarCirculoProgresoTiempo();

        if (rutinaErrorTiempo != null) StopCoroutine(rutinaErrorTiempo);
        rutinaErrorTiempo = StartCoroutine(EfectoErrorTiempo());

        Debug.Log($"❌ Entrega incorrecta: -10% de tiempo (-{tiempoTotalEntrega * 0.10f:F0}s).");
    }

    private IEnumerator EfectoErrorTiempo()
    {
        bloqueoColorTiempo = true;

        if (circuloProgreso != null)
        {
            for (int i = 0; i < 3; i++)
            {
                circuloProgreso.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                circuloProgreso.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
        }

        bloqueoColorTiempo = false;
        rutinaErrorTiempo = null;
        ActualizarCirculoProgresoTiempo();
    }

    // ---------------------------------------------------------
    // MENSAJES
    // ---------------------------------------------------------

    private void MostrarMensajeEsperando()
    {
        if (textoMensaje == null)
            return;

        textoMensaje.text =
            "Todas las tareas fueron asignadas.\n" +
            "Esperando que finalicen los trabajos...";
    }

    private void MostrarMensaje(string mensaje)
    {
        if (textoMensaje != null)
            textoMensaje.text = mensaje;

        // Ocultar el texto de entrega y el círculo de progreso
        if (textoEntrega != null)
            textoEntrega.gameObject.SetActive(false);

        if (circuloProgreso != null)
            circuloProgreso.gameObject.SetActive(false);
    }

    private void LimpiarMensaje()
    {
        if (textoMensaje != null)
            textoMensaje.text = "";

        // Mostrar el texto de entrega y el círculo de progreso
        if (textoEntrega != null)
            textoEntrega.gameObject.SetActive(true);

        if (circuloProgreso != null)
            circuloProgreso.gameObject.SetActive(true);
    }

    // ---------------------------------------------------------
    // REGISTRO DE ESTADÍSTICAS
    // ---------------------------------------------------------

    public void RegistrarAsignacion(bool correcta, Vector3? posicionPopup = null)
    {
        tareasAsignadas++;

        if (correcta)
        {
            tareasCorrectas++;
        }
        else
        {
            tareasIncorrectas++;

            // Penalización por entrega incorrecta (solo durante una entrega activa).
            if (entregaActiva)
                AplicarPenalizacionIncorrecta();
        }

        // Dinero (solo durante una entrega activa).
        if (entregaActiva)
        {
            AgregarDinero(correcta ? dineroCorrecta : dineroIncorrecta, posicionPopup);
        }

        Debug.Log(
            $"Asignadas: {tareasAsignadas} | " +
            $"Correctas: {tareasCorrectas} | " +
            $"Incorrectas: {tareasIncorrectas}"
        );

        // Una tarea rechazada queda resuelta de inmediato: si con eso ya se
        // resolvieron todas, completamos la entrega (evita que quede pausada).
        // Si la penalización dejó el tiempo en 0, la entrega se pierde por tiempo.
        if (!correcta && entregaActiva && tiempoRestante > 0f &&
            TareasResueltas >= ObtenerConfiguracionActual().cantidadTareas)
        {
            CompletarEntrega();
        }
    }

    public void RegistrarTareaCompletada()
    {
        tareasCompletadas++;

        Debug.Log(
            $"Tareas completadas: " +
            $"{tareasCompletadas}/{ObtenerConfiguracionActual().cantidadTareas}"
        );

        if (TareasResueltas >= ObtenerConfiguracionActual().cantidadTareas)
        {
            CompletarEntrega();
        }
    }

    // ---------------------------------------------------------
    // FINALIZACIÓN
    // ---------------------------------------------------------

    private void CompletarEntrega()
    {
        if (!entregaActiva || esperandoProximaEntrega)
            return;

        entregaActiva = false;
        esperandoProximaEntrega = true;

        DesactivarAlerta();

        if (audioSource != null && sonidoEntregaCompletada != null)
            audioSource.PlayOneShot(sonidoEntregaCompletada);

        // Al completar, el círculo se llena al 100% (éxito)
        if (circuloProgreso != null)
        {
            circuloProgreso.fillAmount = 1f;
            circuloProgreso.color = Color.white; // opcional, pero visualmente se ve bien
        }

        // Confeti al terminar la oleada.
        LanzarConfeti();

        Debug.Log(
            $"ENTREGA {numeroEntrega} COMPLETADA | " +
            $"Correctas: {tareasCorrectas} | " +
            $"Incorrectas: {tareasIncorrectas}"
        );

        StartCoroutine(TransicionProximaEntrega());
    }

    private IEnumerator TransicionProximaEntrega()
    {
        MostrarMensaje("¡Entrega completada!");

        yield return new WaitForSeconds(
            duracionMensajeEntregaCompletada
        );

        if (numeroEntrega >= 3)
        {
            GanarJuego();
            yield break;
        }

        MostrarMensaje(
            "Preparando la próxima entrega...\n" +
            "¡A trabajar!"
        );

        yield return new WaitForSeconds(
            duracionMensajeProximaEntrega
        );

        IniciarSiguienteEntrega();
    }

    private void FinalizarEntregaPorTiempo()
    {
        if (!entregaActiva)
            return;

        entregaActiva = false;

        DesactivarAlerta();

        if (audioSource != null && sonidoEntregaFallida != null)
            audioSource.PlayOneShot(sonidoEntregaFallida);

        int noAsignadas =
            ObtenerConfiguracionActual().cantidadTareas -
            tareasAsignadas;

        Debug.Log(
            $"ENTREGA {numeroEntrega} FALLIDA | " +
            $"Correctas: {tareasCorrectas} | " +
            $"Incorrectas: {tareasIncorrectas} | " +
            $"No asignadas: {noAsignadas}"
        );

        PerderJuego();
    }

    // ---------------------------------------------------------
    // FIN DEL JUEGO
    // ---------------------------------------------------------

    private void GanarJuego()
    {
        esperandoProximaEntrega = false;

        if (audioSource != null && sonidoJuegoGanado != null)
            audioSource.PlayOneShot(sonidoJuegoGanado);

        if (cartelGanaste != null)
        {
            PonerPorEncimaDeTodo(cartelGanaste);
            cartelGanaste.SetActive(true);
        }

        // Todos los compañeros celebran (animación de recreo).
        if (todosLosCompanieros != null)
        {
            foreach (var go in todosLosCompanieros)
            {
                if (go == null) continue;
                var comp = go.GetComponent<Compañero>();
                if (comp != null)
                    comp.Celebrar();
            }
        }

        LanzarConfeti();

        Debug.Log("¡JUEGO COMPLETADO!");
    }

    private void PerderJuego()
    {
        if (cartelPerdiste != null)
        {
            PonerPorEncimaDeTodo(cartelPerdiste);
            cartelPerdiste.SetActive(true);
        }

        // Desaparecer a los compañeros (no se puede interactuar con ellos).
        DesactivarCompanieros();

        Debug.Log("¡JUEGO PERDIDO!");
    }

    private void DesactivarCompanieros()
    {
        if (todosLosCompanieros == null)
            return;

        foreach (var c in todosLosCompanieros)
        {
            if (c != null)
                c.SetActive(false);
        }
    }

    /// <summary>
    /// Llueven confetis de colores sobre el jugador (cámara).
    /// </summary>
    private void LanzarConfeti()
    {
        Vector3 centro = Camera.main != null ? Camera.main.transform.position : transform.position;

        for (int i = 0; i < 60; i++)
        {
            GameObject pieza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pieza.transform.position = centro + Vector3.up * 3f +
                new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
            pieza.transform.localScale = new Vector3(
                Random.Range(0.03f, 0.08f),
                Random.Range(0.03f, 0.08f),
                Random.Range(0.03f, 0.08f));

            var rend = pieza.GetComponent<Renderer>();
            rend.material.color = Color.HSVToRGB(Random.value, 1f, 1f);

            var rb = pieza.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.drag = 1f;
            rb.angularVelocity = Random.insideUnitSphere * 5f;

            Destroy(pieza, 4f);
        }
    }

    /// <summary>
    /// Sube el sortingOrder de todos los renderers/canvases del objeto para que
    /// se dibuje por encima de las paredes/murales y del resto de la UI.
    /// </summary>
    private void PonerPorEncimaDeTodo(GameObject obj)
    {
        if (obj == null) return;

        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            r.sortingOrder = 200;

        var canvases = obj.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            c.overrideSorting = true;
            c.sortingOrder = 200;
        }
    }

    // ---------------------------------------------------------
    // DINERO / PUNTAJE
    // ---------------------------------------------------------

    public int DineroActual => dinero;

    /// <summary>
    /// Suma (o resta) dinero, actualiza el contador y, si se pasa una posición,
    /// muestra un popup flotante con el valor (gamefeeling de casino).
    /// </summary>
    public void AgregarDinero(int cantidad, Vector3? posicionPopup = null)
    {
        dinero += cantidad;
        ActualizarTextoDinero();

        if (posicionPopup.HasValue)
            CrearPopupDinero(cantidad, posicionPopup.Value);

        // Si te quedás sin dinero (negativo), perdés.
        if (dinero < 0 && entregaActiva)
            PerderPorDinero();
    }

    /// <summary>
    /// Pierde la partida por quedarse sin dinero.
    /// </summary>
    private void PerderPorDinero()
    {
        if (!entregaActiva)
            return;

        entregaActiva = false;
        DesactivarAlerta();

        if (audioSource != null && sonidoEntregaFallida != null)
            audioSource.PlayOneShot(sonidoEntregaFallida);

        Debug.Log("❌ Te quedaste sin dinero. ¡JUEGO PERDIDO!");

        PerderJuego();
    }

    /// <summary>
    /// Notifica que un compañero se durmió (penalización de dinero).
    /// </summary>
    public void RegistrarCompaneroDormido(Vector3 posicion)
    {
        if (!entregaActiva)
            return;

        AgregarDinero(dineroDormido, posicion);
    }

    private void ActualizarTextoDinero()
    {
        if (textoDinero != null)
            textoDinero.text = $"$ {dinero:N0}";
    }

    /// <summary>
    /// Muestra un popup flotante con el valor sumado/restado (gamefeeling de casino).
    /// </summary>
    private void CrearPopupDinero(int cantidad, Vector3 posicion)
    {
        bool positivo = cantidad >= 0;

        var go = new GameObject("PopupDinero", typeof(RectTransform));
        go.transform.position = posicion;
        go.transform.localScale = Vector3.one * 0.004f;

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 300;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 120f);

        var goTexto = new GameObject("Texto", typeof(RectTransform));
        goTexto.transform.SetParent(go.transform, false);

        var texto = goTexto.AddComponent<TextMeshProUGUI>();
        texto.font = TMP_Settings.defaultFontAsset;
        texto.fontSize = 48;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = positivo ? new Color(0.3f, 1f, 0.5f, 1f) : new Color(1f, 0.2f, 0.2f, 1f);
        texto.outlineWidth = 0.2f;
        texto.outlineColor = Color.black;
        texto.text = positivo ? $"+{cantidad:N0}" : $"{cantidad:N0}";

        var rtTexto = goTexto.GetComponent<RectTransform>();
        rtTexto.anchorMin = Vector2.zero;
        rtTexto.anchorMax = Vector2.one;
        rtTexto.offsetMin = Vector2.zero;
        rtTexto.offsetMax = Vector2.zero;

        StartCoroutine(AnimacionPopup(go, texto, posicion));
    }

    private IEnumerator AnimacionPopup(GameObject go, TextMeshProUGUI texto, Vector3 posicionInicial)
    {
        float duracion = 1.2f;
        float t = 0f;
        Color colorBase = texto.color;
        float escalaBase = 0.01f;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            float u = Mathf.Clamp01(t);

            // Zoom in: de 30% a 120% de la escala base.
            float escala = escalaBase * Mathf.Lerp(0.3f, 1.2f, u);
            go.transform.localScale = Vector3.one * escala;

            // Sube.
            go.transform.position = posicionInicial + Vector3.up * (u * 1.5f);

            // Pierde opacidad al final.
            float alpha = (u > 0.55f) ? Mathf.Lerp(1f, 0f, (u - 0.55f) / 0.45f) : 1f;
            texto.color = new Color(colorBase.r, colorBase.g, colorBase.b, alpha);

            // Billboard hacia la cámara.
            if (Camera.main != null)
            {
                Vector3 dir = go.transform.position - Camera.main.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                    go.transform.rotation = Quaternion.LookRotation(dir);
            }

            yield return null;
        }

        Destroy(go);
    }

    // ---------------------------------------------------------
    // CONSULTAS
    // ---------------------------------------------------------

    public int TareasAsignadas => tareasAsignadas;
    public int TareasCorrectas => tareasCorrectas;
    public int TareasIncorrectas => tareasIncorrectas;
    public int TareasCompletadas => tareasCompletadas;

    public int TareasNoAsignadas
    {
        get
        {
            return ObtenerConfiguracionActual().cantidadTareas -
                   tareasAsignadas;
        }
    }
}