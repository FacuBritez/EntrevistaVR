using UnityEngine;

public class GeneradorTareas : MonoBehaviour
{
    [Header("Prefab y Spawn")]
    [SerializeField] private GameObject prefabHojaTarea; // prefab con el script HojaTarea
    [SerializeField] private Transform puntoSpawn;        // dónde aparece la hoja

    [Header("Tiempo entre tareas")]
    [SerializeField] private float tiempoMinSpawn = 3f;
    [SerializeField] private float tiempoMaxSpawn = 8f;

    [Header("Configuración de tareas")]
    [SerializeField] private string[] nombresPosibles = { "Reportar bug", "Diseñar UI", "Hacer factura", "Componer música", "Publicar post" };
    [SerializeField] private float duracionMin = 4f;
    [SerializeField] private float duracionMax = 10f;

    private float temporizador;

    void Start()
    {
        ResetearTemporizador();
    }

    void Update()
    {
        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            GenerarTarea();
            ResetearTemporizador();
        }
    }

    private void GenerarTarea()
    {
        if (prefabHojaTarea == null || puntoSpawn == null) return;

        GameObject nuevaHoja = Instantiate(prefabHojaTarea, puntoSpawn.position, puntoSpawn.rotation);
        HojaTarea hoja = nuevaHoja.GetComponent<HojaTarea>();

        if (hoja != null)
        {
            hoja.nombreTarea = nombresPosibles[Random.Range(0, nombresPosibles.Length)];
            hoja.rolRequerido = (RolTarea)Random.Range(0, System.Enum.GetValues(typeof(RolTarea)).Length);
            hoja.duracionBase = Random.Range(duracionMin, duracionMax);
        }

        Debug.Log($"🆕 Nueva tarea generada: {hoja.nombreTarea} ({hoja.rolRequerido})");
    }

    private void ResetearTemporizador()
    {
        temporizador = Random.Range(tiempoMinSpawn, tiempoMaxSpawn);
    }
}