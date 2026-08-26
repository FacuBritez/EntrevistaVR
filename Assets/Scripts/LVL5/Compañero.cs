using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Posibles estados del compañero
public enum Estadocompañero
{
    Esperando,
    Trabajando,
    Enojado,
    Distraido
}

[System.Serializable]
public class Tarea
{
    public string nombreTarea;
    public RolTarea rolRequerido;
    public float duracionBase; // en segundos
}

public enum RolTarea
{
    Programador,
    Artista,
    Contable,
    Musico,
    Marketing
}

public class Compañero : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoRol;
    [SerializeField] private TMP_Text textoEstado;
    [SerializeField] private TMP_Text textoInfo;

    [Header("Configuración del compañero")]
    [SerializeField] private RolTarea rolcompañero; // Rol fijo de este compañero

    [Header("Velocidades")]
    [SerializeField] private float velocidadNormal = 1.0f;
    [SerializeField] private float velocidadEnojado = 0.5f;

    [Header("Distracción")]
    [SerializeField] private float tiempoMinDistraccion = 5f;
    [SerializeField] private float tiempoMaxDistraccion = 15f;
    [SerializeField] private float duracionDistraccion = 3f;

    [Header("Animator")]
    [SerializeField] private Animator animator; // Asignar desde Inspector o se buscará automáticamente

    // --- Variables internas ---
    private string nombre => gameObject.name;
    private Estadocompañero estadoActual = Estadocompañero.Esperando;
    private Queue<Tarea> listaTareas = new Queue<Tarea>();
    private Tarea tareaActual;
    private float progresoTarea;

    private float temporizadorDistraccion;
    private bool pausadoPorDistraccion = false;

    private Coroutine rutinaDistraccion;

    // Propiedades públicas
    public Estadocompañero EstadoActual => estadoActual;
    public Tarea TareaActual => tareaActual;
    public int TareasPendientes => listaTareas.Count;

    private void Start()
    {
        // Buscar Animator en los hijos si no se asignó manualmente
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        CambiarEstado(Estadocompañero.Esperando);
        ResetearTemporizadorDistraccion();

        if (textoRol != null) textoRol.text = rolcompañero.ToString();
    }

    private void Update()
    {
        if (estadoActual == Estadocompañero.Trabajando || estadoActual == Estadocompañero.Enojado)
        {
            ProcesarTarea();
        }

        if (estadoActual == Estadocompañero.Trabajando && !pausadoPorDistraccion)
        {
            temporizadorDistraccion -= Time.deltaTime;
            if (temporizadorDistraccion <= 0f)
            {
                IniciarDistraccion();
            }
        }

        ActualizarTextoEstado();
    }

    public void SetApuntado(bool apuntado)
    {
        if (textoInfo == null) return;
        textoInfo.text = apuntado ? "Apuntando" : "";
    }

    private void ActualizarTextoEstado()
    {
        if (textoEstado == null) return;

        if (estadoActual == Estadocompañero.Trabajando || estadoActual == Estadocompañero.Enojado)
        {
            textoEstado.text = $"{estadoActual} ({Mathf.Max(0, progresoTarea):F1}s)";
        }
        else
        {
            textoEstado.text = estadoActual.ToString();
        }
    }

    // MÉTODOS PÚBLICOS ---------------------------------

    public void AsignarTarea(Tarea nuevaTarea)
    {
        if (nuevaTarea == null)
        {
            Debug.LogWarning("Se intentó asignar una tarea nula.");
            return;
        }

        listaTareas.Enqueue(nuevaTarea);
        Debug.Log($"{nombre} ha recibido la tarea '{nuevaTarea.nombreTarea}'. Cantidad en espera: {listaTareas.Count}");

        if (estadoActual == Estadocompañero.Esperando)
        {
            IniciarSiguienteTarea();
        }
    }

    // MÉTODOS INTERNOS ---------------------------------

    private void ProcesarTarea()
    {
        if (tareaActual == null) return;
        if (pausadoPorDistraccion) return;

        float velocidad = (estadoActual == Estadocompañero.Trabajando) ? velocidadNormal : velocidadEnojado;
        progresoTarea -= Time.deltaTime * velocidad;

        if (progresoTarea <= 0f)
        {
            CompletarTareaActual();
        }
    }

    private void IniciarSiguienteTarea()
    {
        if (estadoActual == Estadocompañero.Distraido) return;

        if (listaTareas.Count > 0)
        {
            tareaActual = listaTareas.Dequeue();
            progresoTarea = tareaActual.duracionBase;

            bool esCorrecta = (tareaActual.rolRequerido == rolcompañero);
            Estadocompañero nuevoEstado = esCorrecta ? Estadocompañero.Trabajando : Estadocompañero.Enojado;

            CambiarEstado(nuevoEstado);
            Debug.Log($"{nombre} comenzó '{tareaActual.nombreTarea}' (Rol requerido: {tareaActual.rolRequerido}, Rol propio: {rolcompañero}) - {(esCorrecta ? "¡Correcta!" : "Incorrecta")}");

            if (nuevoEstado == Estadocompañero.Trabajando)
            {
                ResetearTemporizadorDistraccion();
            }
            else
            {
                pausadoPorDistraccion = false;
                if (rutinaDistraccion != null)
                {
                    StopCoroutine(rutinaDistraccion);
                    rutinaDistraccion = null;
                }
            }
        }
        else
        {
            tareaActual = null;
            progresoTarea = 0f;
            CambiarEstado(Estadocompañero.Esperando);
            Debug.Log($"{nombre} no tiene más tareas. Esperando...");
        }
    }

    private void CompletarTareaActual()
    {
        if (tareaActual == null) return;

        Debug.Log($"{nombre} completó la tarea '{tareaActual.nombreTarea}'.");

        if (OleadasManager.Instancia != null)
        {
            OleadasManager.Instancia.RegistrarTareaCompletada();
        }

        tareaActual = null;
        progresoTarea = 0f;

        if (rutinaDistraccion != null)
        {
            StopCoroutine(rutinaDistraccion);
            rutinaDistraccion = null;
            pausadoPorDistraccion = false;
        }

        IniciarSiguienteTarea();
    }

    private void CambiarEstado(Estadocompañero nuevoEstado)
    {
        if (estadoActual == nuevoEstado) return;
        estadoActual = nuevoEstado;
        Debug.Log($"{nombre} cambió a estado: {estadoActual}");

        // Actualizar la animación al cambiar de estado
        ActualizarAnimacion();
    }

    // MÉTODOS DE ANIMACIÓN ---------------------------------

    /// Reproduce la animación correspondiente al estado actual en bucle.
    private void ActualizarAnimacion()
    {
        if (animator == null) return;

        // El nombre del estado debe coincidir exactamente con el enum (Ej: "Esperando", "Trabajando", etc.)
        string nombreEstado = estadoActual.ToString();
        animator.Play(nombreEstado, 0, 0f);
        // Nota: El clip debe tener "Loop Time" activado en el Inspector del Animator.
    }

    // MÉTODOS DE DISTRACCIÓN ---------------------------------

    private void IniciarDistraccion()
    {
        if (estadoActual != Estadocompañero.Trabajando || pausadoPorDistraccion) return;

        CambiarEstado(Estadocompañero.Distraido);
        pausadoPorDistraccion = true;
        Debug.Log($"{nombre} se ha distraído por {duracionDistraccion} segundos.");

        if (rutinaDistraccion != null) StopCoroutine(rutinaDistraccion);
        rutinaDistraccion = StartCoroutine(FinalizarDistraccion());
    }

    private IEnumerator FinalizarDistraccion()
    {
        yield return new WaitForSeconds(duracionDistraccion);

        pausadoPorDistraccion = false;
        rutinaDistraccion = null;

        if (tareaActual != null)
        {
            CambiarEstado(Estadocompañero.Trabajando);
            ResetearTemporizadorDistraccion();
            Debug.Log($"{nombre} ha vuelto a trabajar en '{tareaActual.nombreTarea}'.");
        }
        else
        {
            IniciarSiguienteTarea();
        }
    }

    private void ResetearTemporizadorDistraccion()
    {
        temporizadorDistraccion = Random.Range(tiempoMinDistraccion, tiempoMaxDistraccion);
    }

    public bool EsRolCorrecto(RolTarea rol)
    {
        return rolcompañero == rol;
    }
}