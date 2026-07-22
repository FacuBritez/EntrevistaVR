using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Posibles estados del compañero
public enum Estadocompañero
{
    Esperando,
    Trabajando,
    Enojado,
    Distraido
}

// Puede ser un ScriptableObject o un MonoBehaviour.
// La asignaremos desde el exterior.
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
    [Header("Configuración del compañero")]
    [SerializeField] private RolTarea rolcompañero; // Rol fijo de este compañero

    [Header("Velocidades")]
    [SerializeField] private float velocidadNormal = 1.0f;   // multiplicador para tarea correcta
    [SerializeField] private float velocidadEnojado = 0.5f; // multiplicador para tarea incorrecta

    [Header("Distracción")]
    [SerializeField] private float tiempoMinDistraccion = 5f;   // cada cuanto puede distraerse
    [SerializeField] private float tiempoMaxDistraccion = 15f;  // máximo
    [SerializeField] private float duracionDistraccion = 3f;    // cuánto dura distraído

    // --- Variables internas ---

    private string nombre => gameObject.name;
    private Estadocompañero estadoActual = Estadocompañero.Esperando;
    private Queue<Tarea> listaTareas = new Queue<Tarea>(); // lista de tareas pendientes
    private Tarea tareaActual;        // referencia a la tarea que se está ejecutando
    private float progresoTarea;      // tiempo restante para completar la tarea actual (en segundos)

    // Temporizadores internos
    private float temporizadorDistraccion;  // cuenta regresiva para la próxima distracción
    private bool pausadoPorDistraccion = false;

    // Corrutina para manejar la distracción (para no bloquear el Update)
    private Coroutine rutinaDistraccion;

    // Eventos (opcional, para notificar a otros sistemas)
    //public System.Action<Estadocompañero> OnEstadoCambiado;
    //public System.Action<Tarea> OnTareaCompletada;
    //public System.Action OnlistaVacia;

    

    // Propiedades públicas para consultar el estado o la tarea actual
    public Estadocompañero EstadoActual => estadoActual;
    public Tarea TareaActual => tareaActual;
    public int TareasPendientes => listaTareas.Count;



    private void Start()
    {
        CambiarEstado(Estadocompañero.Esperando);
        // Programamos la primera distracción
        ResetearTemporizadorDistraccion();
    }

    private void Update()
    {
        // Solo actualizamos si estamos en un estado que requiere procesamiento
        if (estadoActual == Estadocompañero.Trabajando || estadoActual == Estadocompañero.Enojado)
        {
            ProcesarTarea();
        }

        // Control de distracción (solo si está trabajando y no está distraído)
        if (estadoActual == Estadocompañero.Trabajando && !pausadoPorDistraccion)
        {
            temporizadorDistraccion -= Time.deltaTime;
            if (temporizadorDistraccion <= 0f)
            {
                // Se dispara la distracción
                IniciarDistraccion();
            }
        }
    }

    // MÉTODOS PÚBLICOS PARA EL JUGADOR ---------------------------------

    /// Añadir una nueva tarea al compañero.
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
        // Si está distraído, al volver a Trabajando, inicia automáticamente.
    }

    // MÉTODOS INTERNOS DE LÓGICA

    /// Procesa el progreso de la tarea actual (reduce tiempo restante).
    private void ProcesarTarea()
    {
        if (tareaActual == null) return;

        // Si está distraído, no procesamos (pausado)
        if (pausadoPorDistraccion) return;

        // Calculamos la velocidad efectiva según el estado
        float velocidad = (estadoActual == Estadocompañero.Trabajando) ? velocidadNormal : velocidadEnojado;
        progresoTarea -= Time.deltaTime * velocidad;

        // Si el progreso llega a cero, completamos la tarea
        if (progresoTarea <= 0f)
        {
            CompletarTareaActual();
        }
    }

    /// Toma la primera tarea de la lista y la comienza.
    private void IniciarSiguienteTarea()
    {
        // Si estamos distraídos, no podemos iniciar una nueva tarea (esperamos a que termine la distracción)
        if (estadoActual == Estadocompañero.Distraido) return;

        // Si hay tareas en la lista, sacamos la primera
        if (listaTareas.Count > 0)
        {
            tareaActual = listaTareas.Dequeue();
            progresoTarea = tareaActual.duracionBase;

            // Evaluamos si el rol coincide
            bool esCorrecta = (tareaActual.rolRequerido == rolcompañero);
            Estadocompañero nuevoEstado = esCorrecta ? Estadocompañero.Trabajando : Estadocompañero.Enojado;

            CambiarEstado(nuevoEstado);
            Debug.Log($"{nombre} comenzó '{tareaActual.nombreTarea}' (Rol requerido: {tareaActual.rolRequerido}, Rol propio: {rolcompañero}) - {(esCorrecta ? "¡Correcta!" : "Incorrecta")}");

            // Reiniciamos el contador de distracción solo si es trabajando
            if (nuevoEstado == Estadocompañero.Trabajando)
            {
                ResetearTemporizadorDistraccion();
            }
            else
            {
                // Si está enojado, nos aseguramos de que no haya distracción pendiente
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
            // No hay más tareas, volvemos a esperar
            tareaActual = null;
            progresoTarea = 0f;
            CambiarEstado(Estadocompañero.Esperando);
            //OnlistaVacia?.Invoke();
            Debug.Log($"{nombre} no tiene más tareas. Esperando...");
        }
    }

    /// Finaliza la tarea actual, notifica y pasa a la siguiente.
    private void CompletarTareaActual()
    {
        if (tareaActual == null) return;

        Debug.Log($"{nombre} completó la tarea '{tareaActual.nombreTarea}'.");
        //OnTareaCompletada?.Invoke(tareaActual);

        // Limpiamos la tarea actual
        tareaActual = null;
        progresoTarea = 0f;

        // Si estábamos distraídos, cancelamos la corrutina
        if (rutinaDistraccion != null)
        {
            StopCoroutine(rutinaDistraccion);
            rutinaDistraccion = null;
            pausadoPorDistraccion = false;
        }

        // Pasamos a la siguiente tarea
        IniciarSiguienteTarea();
    }

    /// Cambia el estado actual y notifica el cambio.
    private void CambiarEstado(Estadocompañero nuevoEstado)
    {
        if (estadoActual == nuevoEstado) return;
        estadoActual = nuevoEstado;
        //OnEstadoCambiado?.Invoke(estadoActual);
        Debug.Log($"{nombre} cambió a estado: {estadoActual}");
    }

    // MÉTODOS DE DISTRACCIÓN

    /// Inicia el proceso de distracción (se llama cuando el temporizador llega a cero).
    private void IniciarDistraccion()
    {
        // Solo puede distraerse si está trabajando y no está ya distraído
        if (estadoActual != Estadocompañero.Trabajando || pausadoPorDistraccion) return;

        // Cambiamos a estado Distraído (pausa el progreso)
        CambiarEstado(Estadocompañero.Distraido);
        pausadoPorDistraccion = true;
        Debug.Log($"{nombre} se ha distraído por {duracionDistraccion} segundos.");

        // Iniciamos la corrutina que cuenta la duración de la distracción
        if (rutinaDistraccion != null) StopCoroutine(rutinaDistraccion);
        rutinaDistraccion = StartCoroutine(FinalizarDistraccion());
    }

    /// Corrutina que espera el tiempo de distracción y luego vuelve a trabajando.
    private IEnumerator FinalizarDistraccion()
    {
        yield return new WaitForSeconds(duracionDistraccion);

        // Terminó la distracción
        pausadoPorDistraccion = false;
        rutinaDistraccion = null;

        // Volvemos a trabajar (si sigue habiendo tarea)
        if (tareaActual != null)
        {
            CambiarEstado(Estadocompañero.Trabajando);
            // Reiniciamos el temporizador de distracción para el próximo ciclo
            ResetearTemporizadorDistraccion();
            Debug.Log($"{nombre} ha vuelto a trabajar en '{tareaActual.nombreTarea}'.");
        }
        else
        {
            // Si no hay tarea, pasamos a espera
            IniciarSiguienteTarea();
        }
    }

    private void ResetearTemporizadorDistraccion()
    {
        temporizadorDistraccion = Random.Range(tiempoMinDistraccion, tiempoMaxDistraccion);
    }
}