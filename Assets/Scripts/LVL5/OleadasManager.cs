using UnityEngine;
using TMPro;

public class OleadasManager : MonoBehaviour
{
    [System.Serializable]
    public class ConfiguracionOleada
    {
        public int cantidadTareas = 5;
        public float tiempoOleada = 180f;
    }

    [Header("Configuración de Oleadas")]
    [SerializeField] private ConfiguracionOleada oleada1;
    [SerializeField] private ConfiguracionOleada oleada2;
    [SerializeField] private ConfiguracionOleada oleada3;

    [Header("UI")]
    [SerializeField] private TMP_Text textoOleada;

    [SerializeField] private TMP_Text textoTiempo;

    [Header("Generador")]
    [SerializeField] private GeneradorTareas generadorTareas;

    private int numeroOleada = 0;
    private float tiempoRestante;

    private int tareasAsignadas;
    private int tareasCorrectas;
    private int tareasIncorrectas;
    private int tareasCompletadas;

    private bool oleadaActiva = false;

    public static OleadasManager Instancia { get; private set; }

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        IniciarSiguienteOleada();
    }

    private void Update()
    {
        if (!oleadaActiva)
            return;

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante < 0f)
            tiempoRestante = 0f;

        ActualizarTextoTiempo();

        if (tiempoRestante <= 0f)
        {
            FinalizarOleadaPorTiempo();
        }
        else if (tareasCompletadas >= ObtenerConfiguracionActual().cantidadTareas)
        {
            CompletarOleada();
        }
    }

    private void IniciarSiguienteOleada()
    {
        numeroOleada++;

        if (numeroOleada > 3)
        {
            GanarJuego();
            return;
        }

        ConfiguracionOleada config = ObtenerConfiguracionActual();

        tareasAsignadas = 0;
        tareasCorrectas = 0;
        tareasIncorrectas = 0;
        tareasCompletadas = 0;

        tiempoRestante = config.tiempoOleada;
        oleadaActiva = true;

        ActualizarTextoTiempo();

        if (generadorTareas != null)
        {
            generadorTareas.IniciarOleada(config.cantidadTareas);
        }

        if (textoOleada != null)
        {
            textoOleada.text = $"Oleada {numeroOleada}";
        }

        Debug.Log($"🌊 OLEADA {numeroOleada} - {config.cantidadTareas} tareas - {config.tiempoOleada} segundos");
    }

    private ConfiguracionOleada ObtenerConfiguracionActual()
    {
        switch (numeroOleada)
        {
            case 1: return oleada1;
            case 2: return oleada2;
            case 3: return oleada3;
            default: return oleada1;
        }
    }

    private void ActualizarTextoTiempo()
    {
        if (textoTiempo == null)
            return;

        int minutos = Mathf.FloorToInt(tiempoRestante / 60f);
        int segundos = Mathf.FloorToInt(tiempoRestante % 60f);

        textoTiempo.text = $"{minutos:00}:{segundos:00}";
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
            $"📊 Asignadas: {tareasAsignadas} | " +
            $"Correctas: {tareasCorrectas} | " +
            $"Incorrectas: {tareasIncorrectas}"
        );
    }

    public void RegistrarTareaCompletada()
    {
        tareasCompletadas++;

        Debug.Log($"✅ Tareas completadas: {tareasCompletadas}");

        if (tareasCompletadas >= ObtenerConfiguracionActual().cantidadTareas)
        {
            CompletarOleada();
        }
    }

    // ---------------------------------------------------------
    // FINALIZACIÓN
    // ---------------------------------------------------------

    private void CompletarOleada()
    {
        if (!oleadaActiva)
            return;

        oleadaActiva = false;

        Debug.Log(
            $"🌊 OLEADA {numeroOleada} COMPLETADA | " +
            $"Correctas: {tareasCorrectas} | " +
            $"Incorrectas: {tareasIncorrectas}"
        );

        IniciarSiguienteOleada();
    }

    private void FinalizarOleadaPorTiempo()
    {
        if (!oleadaActiva)
            return;

        oleadaActiva = false;

        int noAsignadas =
            ObtenerConfiguracionActual().cantidadTareas - tareasAsignadas;

        Debug.Log(
            $"❌ OLEADA {numeroOleada} FALLIDA | " +
            $"Correctas: {tareasCorrectas} | " +
            $"Incorrectas: {tareasIncorrectas} | " +
            $"No asignadas: {noAsignadas}"
        );

        PerderJuego();
    }

    private void GanarJuego()
    {
        Debug.Log("🏆 ¡JUEGO COMPLETADO!");
        ActualizarTextoTiempo();
    }

    private void PerderJuego()
    {
        Debug.Log("💀 ¡JUEGO PERDIDO!");
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
            return ObtenerConfiguracionActual().cantidadTareas - tareasAsignadas;
        }
    }
}