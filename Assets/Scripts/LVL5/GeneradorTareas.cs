using UnityEngine;

public class GeneradorTareas : MonoBehaviour
{
    [Header("Prefab y Spawn")]
    [SerializeField] private GameObject prefabHojaTarea;
    [SerializeField] private Transform puntoSpawn;

    [Header("Tiempo entre tareas")]
    [SerializeField] private float tiempoMinSpawn = 3f;
    [SerializeField] private float tiempoMaxSpawn = 8f;

    [Header("Configuración de tareas")]
    [SerializeField] private string[] nombresPosibles =
    {
        "Reportar bug",
        "Diseñar UI",
        "Hacer factura",
        "Componer música",
        "Publicar post"
    };

    [SerializeField] private float duracionMin = 4f;
    [SerializeField] private float duracionMax = 10f;

    private float temporizador;
    private GameObject hojaActual;

    private int tareasRestantesPorGenerar;
    private bool oleadaActiva;

    public void IniciarOleada(int cantidadTareas)
    {
        tareasRestantesPorGenerar = cantidadTareas;
        oleadaActiva = true;

        temporizador = 0f;
        hojaActual = null;
    }

    private void Update()
    {
        if (!oleadaActiva)
            return;

        if (tareasRestantesPorGenerar <= 0)
            return;

        if (hojaActual != null)
            return;

        temporizador -= Time.deltaTime;

        if (temporizador <= 0f)
        {
            GenerarTarea();
            temporizador = Random.Range(tiempoMinSpawn, tiempoMaxSpawn);
        }
    }

    private void GenerarTarea()
    {
        if (prefabHojaTarea == null || puntoSpawn == null)
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
            return;
        }

        hoja.nombreTarea =
            nombresPosibles[Random.Range(0, nombresPosibles.Length)];

        hoja.rolRequerido =
            (RolTarea)Random.Range(
                0,
                System.Enum.GetValues(typeof(RolTarea)).Length
            );

        hoja.duracionBase =
            Random.Range(duracionMin, duracionMax);

        tareasRestantesPorGenerar--;

        Debug.Log(
            $"🆕 Nueva tarea: {hoja.nombreTarea} ({hoja.rolRequerido})"
        );
    }
}