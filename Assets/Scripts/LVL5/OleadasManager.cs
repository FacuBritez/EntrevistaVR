using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OleadasManager : MonoBehaviour
{
    [System.Serializable]
    public class ConfiguracionEntrega
    {
        public int cantidadTareas = 5;
        public float tiempoEntrega = 180f;
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
    [SerializeField] private float duracionMensajeEntregaCompletada = 3f;
    [SerializeField] private float duracionMensajeProximaEntrega = 2f;

    [Header("Generador")]
    [SerializeField] private GeneradorTareas generadorTareas;

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoEntregaCompletada;
    [SerializeField] private AudioClip sonidoEntregaFallida;
    [SerializeField] private AudioClip sonidoJuegoGanado;

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

    // Tutorial
    private enum TutorialState { EsperandoAgarrar, EsperandoAsignar, Completado }
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
        IniciarTutorial();
    }

    private void Update()
    {
        if (tutorialActivo)
        {
            // No hacemos nada aquí, el flujo se maneja con eventos
            return;
        }

        if (!entregaActiva)
            return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante < 0f)
            tiempoRestante = 0f;

        ActualizarCirculoProgresoTiempo();

        // Si ya se asignaron todas las tareas pero todavía
        // hay compañeros trabajando, mostramos el mensaje.
        if (tareasAsignadas >= ObtenerConfiguracionActual().cantidadTareas &&
            tareasCompletadas < ObtenerConfiguracionActual().cantidadTareas)
        {
            MostrarMensajeEsperando();
        }

        // Si se acaba el tiempo, se pierde.
        if (tiempoRestante <= 0f)
        {
            FinalizarEntregaPorTiempo();
            return;
        }

        // Si todas las tareas fueron terminadas, se completa.
        if (tareasCompletadas >= ObtenerConfiguracionActual().cantidadTareas)
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
            tutorialState = TutorialState.Completado;
            tutorialActivo = false;
            DejarDeResaltar();
            OcultarFlecha();
            StartCoroutine(SecuenciaTutorial());
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
        float t = 1f - Mathf.Pow(progreso, 0.5f); // raíz cuadrada para efecto de easing

        circuloProgreso.color = Color.Lerp(Color.white, Color.red, t);
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

    public void RegistrarAsignacion(bool correcta)
    {
        tareasAsignadas++;

        if (correcta)
            tareasCorrectas++;
        else
            tareasIncorrectas++;

        Debug.Log(
            $"Asignadas: {tareasAsignadas} | " +
            $"Correctas: {tareasCorrectas} | " +
            $"Incorrectas: {tareasIncorrectas}"
        );
    }

    public void RegistrarTareaCompletada()
    {
        tareasCompletadas++;

        Debug.Log(
            $"Tareas completadas: " +
            $"{tareasCompletadas}/{ObtenerConfiguracionActual().cantidadTareas}"
        );

        if (tareasCompletadas >= ObtenerConfiguracionActual().cantidadTareas)
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

        if (audioSource != null && sonidoEntregaCompletada != null)
            audioSource.PlayOneShot(sonidoEntregaCompletada);

        // Al completar, el círculo se llena al 100% (éxito)
        if (circuloProgreso != null)
        {
            circuloProgreso.fillAmount = 1f;
            circuloProgreso.color = Color.white; // opcional, pero visualmente se ve bien
        }

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
            cartelGanaste.SetActive(true);

        Debug.Log("¡JUEGO COMPLETADO!");
    }

    private void PerderJuego()
    {
        if (cartelPerdiste != null)
            cartelPerdiste.SetActive(true);

        Debug.Log("¡JUEGO PERDIDO!");
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