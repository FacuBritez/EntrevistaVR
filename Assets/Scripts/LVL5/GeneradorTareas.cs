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

    private GameObject hojaActual;

    private int tareasRestantesPorGenerar;
    private bool oleadaActiva;

    public void IniciarOleada(int cantidadTareas)
    {
        tareasRestantesPorGenerar = cantidadTareas;
        oleadaActiva = true;

        hojaActual = null;

        ActualizarTextoRestantes();
    }

    private void Update()
    {
        if (!oleadaActiva)
            return;

        if (tareasRestantesPorGenerar <= 0)
            return;

        if (hojaActual != null)
            return;

        GenerarTarea();
    }

    private void GenerarTarea()
    {
        if (prefabHojaTarea == null || puntoSpawn == null)
            return;

        if (tareasDisponibles == null || tareasDisponibles.Length == 0)
            return;

        hojaActual = Instantiate(
            prefabHojaTarea,
            puntoSpawn.position,
            puntoSpawn.rotation
        );

        HojaTarea hoja = hojaActual.GetComponent<HojaTarea>();

        if (hoja == null)
        {
            Debug.LogWarning("El prefab no tiene componente HojaTarea.");
            Destroy(hojaActual);
            hojaActual = null;
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

        tareasRestantesPorGenerar--;

        ActualizarTextoRestantes();

        Debug.Log(
            $"🆕 Nueva tarea: {hoja.nombreTarea} | " +
            $"Rol requerido: {hoja.rolRequerido} | " +
            $"Duración: {hoja.duracionBase:F1}s"
        );
    }

    private void ActualizarTextoRestantes()
    {
        if (textoTareasRestantes != null)
        {
            textoTareasRestantes.text =
                $"Tareas Restantes: {tareasRestantesPorGenerar}";
        }
    }
}
