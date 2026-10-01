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

        if (tareasAsignadas >= ObtenerConfiguracionActual().cantidadTareas &&
            tareasCompletadas < ObtenerConfiguracionActual().cantidadTareas)
        {
            MostrarMensajeEsperando();
        }

        if (tiempoRestante <= 0f)
        {
            FinalizarEntregaPorTiempo();
            return;
        }

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

        StartCoroutine(SecuenciaInicioTutorial());
    }

    private IEnumerator SecuenciaInicioTutorial()
    {
        MostrarMensajeTutorial("¡Bienvenido a tu primer día de pasantía!");
        yield return new WaitForSeconds(10f);

        MostrarMensajeTutorial("Asigna las tareas que lleguen a tus compañeros.");
        yield return new WaitForSeconds(5f);

        MostrarMensajeTutorial("Mira la pizarra de tu izquierda para ver sus roles.");
        yield return new WaitForSeconds(5f);

        CrearTareaTutorial();
        MostrarMensajeTutorial("Para agarrar la tarea acercale la mano y manten apretado el botón de agarre.");
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
            MostrarMensajeTutorial("La tarea es para el programador. Apunta a él y suelta la tarea.");
        }
    }

    public void SoltarTareaTutorial(bool acerto)
    {
        if (tutorialActivo && tutorialState == TutorialState.EsperandoAsignar && acerto)
        {
            tutorialState = TutorialState.Completado;
            tutorialActivo = false;
            StartCoroutine(SecuenciaTutorial());
        }
    }

    private IEnumerator SecuenciaTutorial()
    {
        MostrarMensajeTutorial("¡Perfecto! Ahora empieza el trabajo real.");
        yield return new WaitForSeconds(3f);
        MostrarMensajeTutorial("Recuerda: lee bien la tarea y asígnala a quien corresponda.");
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

        LimpiarMensaje();
        IniciarSiguienteEntrega();
    }

    private void MostrarMensajeTutorial(string mensaje)
    {
        if (textoMensaje != null)
            textoMensaje.text = mensaje;
        if (textoEntrega != null)
            textoEntrega.gameObject.SetActive(false);
        if (circuloProgreso != null)
            circuloProgreso.gameObject.SetActive(false);
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

        ActualizarCirculoProgresoTiempo();

        if (generadorTareas != null)
        {
            generadorTareas.IniciarOleada(config.cantidadTareas);
        }

        if (textoEntrega != null)
        {
            textoEntrega.text = $"Entrega {numeroEntrega}";
            textoEntrega.gameObject.SetActive(true);
        }

        if (circuloProgreso != null)
            circuloProgreso.gameObject.SetActive(true);

        LimpiarMensaje();

        Debug.Log($"ENTREGA {numeroEntrega} - {config.cantidadTareas} tareas - {config.tiempoEntrega} segundos");
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
    // CÍRCULO DE PROGRESO
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
        float t = 1f - Mathf.Pow(progreso, 0.5f);
        circuloProgreso.color = Color.Lerp(Color.white, Color.red, t);
    }

    // ---------------------------------------------------------
    // MENSAJES
    // ---------------------------------------------------------

    private void MostrarMensajeEsperando()
    {
        if (textoMensaje == null)
            return;
        textoMensaje.text = "Todas las tareas fueron asignadas.\nEsperando que finalicen los trabajos...";
        if (circuloProgreso != null)
            circuloProgreso.gameObject.SetActive(false);
        if (textoEntrega != null)
            textoEntrega.gameObject.SetActive(false);
    }

    private void MostrarMensaje(string mensaje)
    {
        if (textoMensaje != null)
            textoMensaje.text = mensaje;
        if (textoEntrega != null)
            textoEntrega.gameObject.SetActive(false);
        if (circuloProgreso != null)
            circuloProgreso.gameObject.SetActive(false);
    }

    private void LimpiarMensaje()
    {
        if (textoMensaje != null)
            textoMensaje.text = "";
        if (textoEntrega != null)
            textoEntrega.gameObject.SetActive(true);
        if (circuloProgreso != null)
            circuloProgreso.gameObject.SetActive(true);
    }

    // ---------------------------------------------------------
    // REGISTRO
    // ---------------------------------------------------------

    public void RegistrarAsignacion(bool correcta)
    {
        tareasAsignadas++;
        if (correcta) tareasCorrectas++;
        else tareasIncorrectas++;
        Debug.Log($"Asignadas: {tareasAsignadas} | Correctas: {tareasCorrectas} | Incorrectas: {tareasIncorrectas}");
    }

    public void RegistrarTareaCompletada()
    {
        tareasCompletadas++;
        Debug.Log($"Tareas completadas: {tareasCompletadas}/{ObtenerConfiguracionActual().cantidadTareas}");
        if (tareasCompletadas >= ObtenerConfiguracionActual().cantidadTareas)
            CompletarEntrega();
    }

    // ---------------------------------------------------------
    // FINALIZACIÓN
    // ---------------------------------------------------------

    private void CompletarEntrega()
    {
        if (!entregaActiva || esperandoProximaEntrega) return;
        entregaActiva = false;
        esperandoProximaEntrega = true;

        if (audioSource != null && sonidoEntregaCompletada != null)
            audioSource.PlayOneShot(sonidoEntregaCompletada);

        if (circuloProgreso != null)
        {
            circuloProgreso.fillAmount = 1f;
            circuloProgreso.color = Color.white;
        }

        Debug.Log($"ENTREGA {numeroEntrega} COMPLETADA | Correctas: {tareasCorrectas} | Incorrectas: {tareasIncorrectas}");
        StartCoroutine(TransicionProximaEntrega());
    }

    private IEnumerator TransicionProximaEntrega()
    {
        MostrarMensaje("¡Entrega completada!");
        yield return new WaitForSeconds(duracionMensajeEntregaCompletada);
        if (numeroEntrega >= 3)
        {
            GanarJuego();
            yield break;
        }
        MostrarMensaje("Preparando la próxima entrega...\n¡A trabajar!");
        yield return new WaitForSeconds(duracionMensajeProximaEntrega);
        IniciarSiguienteEntrega();
    }

    private void FinalizarEntregaPorTiempo()
    {
        if (!entregaActiva) return;
        entregaActiva = false;
        if (audioSource != null && sonidoEntregaFallida != null)
            audioSource.PlayOneShot(sonidoEntregaFallida);
        int noAsignadas = ObtenerConfiguracionActual().cantidadTareas - tareasAsignadas;
        Debug.Log($"ENTREGA {numeroEntrega} FALLIDA | Correctas: {tareasCorrectas} | Incorrectas: {tareasIncorrectas} | No asignadas: {noAsignadas}");
        PerderJuego();
    }

    private void GanarJuego()
    {
        esperandoProximaEntrega = false;
        if (audioSource != null && sonidoJuegoGanado != null)
            audioSource.PlayOneShot(sonidoJuegoGanado);
        cartelGanaste.SetActive(true);
        Debug.Log("¡JUEGO COMPLETADO!");
    }

    private void PerderJuego()
    {
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

    public int TareasNoAsignadas => ObtenerConfiguracionActual().cantidadTareas - tareasAsignadas;
}