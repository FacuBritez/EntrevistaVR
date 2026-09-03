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

    private int numeroEntrega = 0;
    private float tiempoRestante;
    private float tiempoTotalEntrega;

    private int tareasAsignadas;
    private int tareasCorrectas;
    private int tareasIncorrectas;
    private int tareasCompletadas;

    private bool entregaActiva = false;
    private bool esperandoProximaEntrega = false;

    public static OleadasManager Instancia { get; private set; }

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        IniciarSiguienteEntrega();
    }

    private void Update()
    {
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

        MostrarMensaje("¡Trabajo completado!");

        Debug.Log("¡JUEGO COMPLETADO!");
    }

    private void PerderJuego()
    {
        MostrarMensaje("No se pudo completar el trabajo a tiempo.");

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