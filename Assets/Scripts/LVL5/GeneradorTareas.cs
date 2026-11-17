using UnityEngine;
using TMPro;

[System.Serializable]
public class TareaDisponible
{
    public string nombre;
    public RolTarea rol;
}

public class GeneradorTareas : MonoBehaviour
{
    [Header("Prefab y Spawn")]
    [SerializeField] private GameObject prefabHojaTarea;
    [SerializeField] private Transform puntoSpawn;
    [SerializeField] private TMP_Text textoTareasRestantes;

    [Header("Plano de acumulación")]
    [Tooltip("Plano 'AcaAparecenTareas' donde se van apilando las tareas que nadie agarró.")]
    [SerializeField] private Transform planoAparicion;

    [Header("Temporización")]
    [Tooltip("Segundos entre cada tarea que sale automáticamente del fax.")]
    [SerializeField] private float intervaloSpawn = 5f;

    [Header("Configuración de tareas")]
    [SerializeField]
    private TareaDisponible[] tareasDisponibles =
    {
        // =========================
        // PROGRAMADOR
        // =========================

        new TareaDisponible { nombre = "Reportar bug", rol = RolTarea.Programador },
        new TareaDisponible { nombre = "Corregir error", rol = RolTarea.Programador },
        new TareaDisponible { nombre = "Implementar función", rol = RolTarea.Programador },
        new TareaDisponible { nombre = "Optimizar código", rol = RolTarea.Programador },
        new TareaDisponible { nombre = "Revisar código", rol = RolTarea.Programador },
        new TareaDisponible { nombre = "Crear script", rol = RolTarea.Programador },
        new TareaDisponible { nombre = "Integrar sistema", rol = RolTarea.Programador },
        new TareaDisponible { nombre = "Solucionar crash", rol = RolTarea.Programador },


        // =========================
        // ARTISTA
        // =========================

        new TareaDisponible { nombre = "Diseñar UI", rol = RolTarea.Artista },
        new TareaDisponible { nombre = "Crear iconos", rol = RolTarea.Artista },
        new TareaDisponible { nombre = "Diseñar menú", rol = RolTarea.Artista },
        new TareaDisponible { nombre = "Ajustar colores", rol = RolTarea.Artista },
        new TareaDisponible { nombre = "Crear ilustración", rol = RolTarea.Artista },
        new TareaDisponible { nombre = "Diseñar botones", rol = RolTarea.Artista },
        new TareaDisponible { nombre = "Mejorar interfaz", rol = RolTarea.Artista },
        new TareaDisponible { nombre = "Crear fondo", rol = RolTarea.Artista },


        // =========================
        // CONTABLE
        // =========================

        new TareaDisponible { nombre = "Hacer factura", rol = RolTarea.Contable },
        new TareaDisponible { nombre = "Revisar gastos", rol = RolTarea.Contable },
        new TareaDisponible { nombre = "Preparar presupuesto", rol = RolTarea.Contable },
        new TareaDisponible { nombre = "Registrar pago", rol = RolTarea.Contable },
        new TareaDisponible { nombre = "Revisar cuentas", rol = RolTarea.Contable },
        new TareaDisponible { nombre = "Calcular impuestos", rol = RolTarea.Contable },
        new TareaDisponible { nombre = "Organizar facturas", rol = RolTarea.Contable },
        new TareaDisponible { nombre = "Preparar informe financiero", rol = RolTarea.Contable },


        // =========================
        // MÚSICO
        // =========================

        new TareaDisponible { nombre = "Componer música", rol = RolTarea.Musico },
        new TareaDisponible { nombre = "Crear melodía", rol = RolTarea.Musico },
        new TareaDisponible { nombre = "Grabar sonido", rol = RolTarea.Musico },
        new TareaDisponible { nombre = "Editar audio", rol = RolTarea.Musico },
        new TareaDisponible { nombre = "Crear efectos de sonido", rol = RolTarea.Musico },
        new TareaDisponible { nombre = "Mezclar pistas", rol = RolTarea.Musico },
        new TareaDisponible { nombre = "Crear música de combate", rol = RolTarea.Musico },
        new TareaDisponible { nombre = "Crear música de menú", rol = RolTarea.Musico },


        // =========================
        // MARKETING
        // =========================

        new TareaDisponible { nombre = "Publicar post", rol = RolTarea.Marketing },
        new TareaDisponible { nombre = "Crear campaña", rol = RolTarea.Marketing },
        new TareaDisponible { nombre = "Diseñar anuncio", rol = RolTarea.Marketing },
        new TareaDisponible { nombre = "Analizar redes sociales", rol = RolTarea.Marketing },
        new TareaDisponible { nombre = "Preparar promoción", rol = RolTarea.Marketing },
        new TareaDisponible { nombre = "Crear contenido", rol = RolTarea.Marketing },
        new TareaDisponible { nombre = "Revisar estadísticas", rol = RolTarea.Marketing },
        new TareaDisponible { nombre = "Planificar campaña", rol = RolTarea.Marketing }
    };

    [SerializeField] private float duracionMin = 4f;
    [SerializeField] private float duracionMax = 10f;

    private int tareasRestantesPorGenerar;
    private bool oleadaActiva;

    private float temporizadorSpawn;
    private int contadorPlano; // cuántas tareas hay apiladas en el plano (para escalonarlas)

    public void IniciarOleada(int cantidadTareas)
    {
        tareasRestantesPorGenerar = cantidadTareas;
        oleadaActiva = true;

        temporizadorSpawn = 0f; // la primera sale en seguida
        contadorPlano = 0;

        // Mostrar el plano donde se apilan las tareas (está oculto en la escena).
        if (planoAparicion != null)
            planoAparicion.gameObject.SetActive(true);

        ActualizarTextoRestantes();
    }

    private void Update()
    {
        if (!oleadaActiva)
            return;

        if (tareasRestantesPorGenerar <= 0)
            return;

        // Las tareas salen solas cada "intervaloSpawn" segundos,
        // SIN esperar a que el jugador agarre la anterior.
        temporizadorSpawn -= Time.deltaTime;
        if (temporizadorSpawn <= 0f)
        {
            temporizadorSpawn = intervaloSpawn;
            GenerarTarea();
        }
    }

    private void GenerarTarea()
    {
        if (prefabHojaTarea == null || puntoSpawn == null)
            return;

        if (tareasDisponibles == null || tareasDisponibles.Length == 0)
            return;

        GameObject hojaGO = Instantiate(
            prefabHojaTarea,
            puntoSpawn.position,
            puntoSpawn.rotation
        );

        HojaTarea hoja = hojaGO.GetComponent<HojaTarea>();

        if (hoja == null)
        {
            Debug.LogWarning("El prefab no tiene componente HojaTarea.");
            Destroy(hojaGO);
            return;
        }

        // Elegir una tarea completa.
        // El nombre y el rol están vinculados.
        TareaDisponible tarea =
            tareasDisponibles[
                Random.Range(0, tareasDisponibles.Length)
            ];

        hoja.nombreTarea = tarea.nombre;
        hoja.rolRequerido = tarea.rol;

        // Duración aleatoria
        hoja.duracionBase =
            Random.Range(duracionMin, duracionMax);

        hoja.ActualizarTexto();

        // Animación "squash" desde el fax hasta el plano, donde se acumulan.
        hoja.IniciarAnimacionAparicion(CalcularDestinoAcumulacion());

        tareasRestantesPorGenerar--;

        ActualizarTextoRestantes();

        Debug.Log(
            $"🆕 Nueva tarea: {hoja.nombreTarea} | " +
            $"Rol requerido: {hoja.rolRequerido} | " +
            $"Duración: {hoja.duracionBase:F1}s"
        );
    }

    /// <summary>
    /// Calcula la posición de aterrizaje de una nueva tarea sobre el plano,
    /// apilándola (un poquito más arriba) y con una pequeña dispersión lateral.
    /// </summary>
    private Vector3 CalcularDestinoAcumulacion()
    {
        if (planoAparicion == null)
            return puntoSpawn.position;

        Vector3 destino = planoAparicion.position;
        destino.x += Random.Range(-0.12f, 0.12f);
        destino.z += Random.Range(-0.12f, 0.12f);
        destino.y += contadorPlano * 0.002f; // escalón de pila (hojas finas)

        contadorPlano++;
        return destino;
    }

    private void ActualizarTextoRestantes()
    {
        if (textoTareasRestantes != null)
        {
            textoTareasRestantes.text =
                $"Tareas Restantes: {tareasRestantesPorGenerar}";
        }
    }

    /// <summary>
    /// Llamado cuando una tarea se cae o se descarta sin asignar, permitiendo que la máquina vuelva a imprimirla sin perder el cupo.
    /// </summary>
    public void ReintentarTarea()
    {
        tareasRestantesPorGenerar++;
        ActualizarTextoRestantes();
        Debug.Log("🔄 Tarea reintegrada a la cola de la entrega.");
    }
}
