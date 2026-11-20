using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Posibles estados del compañero
public enum Estadocompañero
{
    Esperando,
    Trabajando,
    Enojado,
    Distraido,
    Dormido,
    Recreo
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
    [Tooltip("Componente WorkerDisplay ubicado en el Canvas hijo de este trabajador.")]
    [SerializeField] private WorkerDisplay workerDisplay;

    [Header("Configuración del compañero")]
    [SerializeField] private RolTarea rolcompañero; // Rol fijo de este compañero

    [Header("Velocidades")]
    [SerializeField] private float velocidadNormal = 1.67f;
    [SerializeField] private float velocidadEnojado = 0.5f;

    [Header("Distracción")]
    [SerializeField] private float tiempoMinDistraccion = 10f;
    [SerializeField] private float tiempoMaxDistraccion = 30f;
    [SerializeField] private float duracionDistraccion = 3f;

    [Header("Animator")]
    [SerializeField] private Animator animator; // Asignar desde Inspector o se buscará automáticamente

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoTareaCompletada;
    [SerializeField] private AudioClip sonidoDistraccion;
    [SerializeField] private AudioClip sonidoVuelveATrabajar;
    [SerializeField] private AudioClip sonidoEnojado;

    [Header("Cansancio")]
    [Tooltip("Valor máximo de la barra de cansancio.")]
    [SerializeField] private float cansancioMaximo = 100f;
    [Tooltip("Valor inicial de la barra al empezar.")]
    [SerializeField] private float cansancioInicial = 100f;
    [Tooltip("Cuánto baja el cansancio cada vez que se le asigna una tarea.")]
    [SerializeField] private float cansancioPorTarea = 15f;
    [Tooltip("Segundos que tarda en despertarse una vez dormido.")]
    [SerializeField] private float duracionDormido = 8f;
    [Tooltip("Nivel de cansancio (0..cansancioMaximo) con el que despierta (medio lleno).")]
    [SerializeField] private float cansancioAlDespertar = 50f;

    [Header("Barra de cansancio")]
    [Tooltip("Componente BarraCansancio del canvas 'barraCansancio'. Si se deja vacío, se busca automáticamente.")]
    [SerializeField] private BarraCansancio barraCansancio;

    [Header("Selección (pulso al apuntar)")]
    [Tooltip("Cuánto crece/decrece el compañero al ser apuntado (1% = 0.01).")]
    [SerializeField] private float amplitudPulso = 0.01f;
    [Tooltip("Velocidad del pulso de selección.")]
    [SerializeField] private float velocidadPulso = 4f;

    [Header("Enojo")]
    [Tooltip("Segundos que dura la reacción de enojo al rechazar una tarea de otra categoría.")]
    [SerializeField] private float duracionEnojado = 2f;

    [Header("Apuntado del láser")]
    [Tooltip("Fracción de la altura del personaje que se suma por encima del centro al apuntar (0.4 = 40% más alto).")]
    [SerializeField] private float alturaExtraApuntado = 0.4f;

    [Header("Recreo")]
    [Tooltip("Segundos que dura el recreo mientras recupera la barra de cansancio.")]
    [SerializeField] private float duracionRecreo = 8f;
    [Tooltip("Animación que reproduce mientras está en recreo (arrastrala desde el Inspector).")]
    [SerializeField] private AnimationClip animacionRecreo;
    [Tooltip("Porcentaje de la altura del personaje que se eleva al ir a recreo.")]
    [SerializeField] private float porcentajeAlturaRecreo = 20f;

    /// <summary>Animación de recreo global, compartida por todos los compañeros.</summary>
    public static AnimationClip AnimacionRecreoGlobal;

    // --- Variables internas ---
    private string nombre => gameObject.name;
    private Estadocompañero estadoActual = Estadocompañero.Esperando;
    private Queue<Tarea> listaTareas = new Queue<Tarea>();
    private Tarea tareaActual;
    private float progresoTarea;

    private float temporizadorDistraccion;
    private bool pausadoPorDistraccion = false;

    private Coroutine rutinaDistraccion;

    // Cansancio
    private float cansancio;
    private float temporizadorDormido;

    // Selección (pulso)
    private bool apuntado = false;
    private Vector3 escalaOriginal;
    private float tiempoPulso = 0f;

    // Enojo
    private Coroutine rutinaEnojado;

    // Desaturación al dormir
    private Dictionary<Material, Color> coloresBaseCuerpo;

    // Recreo
    private float temporizadorRecreo;
    private float cansancioInicioRecreo;
    private Vector3 posicionRecreo;
    private Vector3 posicionOriginal;
    private static int cantidadEnRecreo = 0;
    private bool enCelebracion = false;
    private AnimatorOverrideController overrideRecreo;
    private RuntimeAnimatorController controllerOriginal;

    // Propiedades públicas
    public Estadocompañero EstadoActual => estadoActual;
    public Tarea TareaActual => tareaActual;
    public int TareasPendientes => listaTareas.Count;
    public bool EstaDormido => estadoActual == Estadocompañero.Dormido;
    public bool EstaEnRecreo => estadoActual == Estadocompañero.Recreo;
    public static int CantidadEnRecreo => cantidadEnRecreo;

    /// <summary>
    /// Punto del cuerpo al que apuntar (el centro, no los pies), desplazado un
    /// poco más arriba. Usa el centro del collider del personaje; si no hay
    /// collider, cae a un offset fijo.
    /// </summary>
    public Vector3 PuntoApuntado
    {
        get
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // Apunta "alturaExtraApuntado" más arriba del centro del cuerpo.
                return col.bounds.center + Vector3.up * (col.bounds.size.y * alturaExtraApuntado);
            }

            return transform.position + Vector3.up * 1.5f;
        }
    }

    private void Start()
    {
        // Buscar Animator en el GameObject o en los hijos si no se asignó manualmente
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Buscar AudioSource en el GameObject o en los hijos si no se asignó manualmente
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>();

        CambiarEstado(Estadocompañero.Esperando);
        ResetearTemporizadorDistraccion();

        cansancio = Mathf.Clamp(cansancioInicial, 0f, cansancioMaximo);
        escalaOriginal = transform.localScale;

        // Cachear los materiales del cuerpo para la desaturación al dormir.
        CapturarMaterialesCuerpo();

        if (barraCansancio == null)
            barraCansancio = ObtenerBarraCansancio();

        ActualizarBarra();

        if (workerDisplay != null) workerDisplay.InicializarRol(rolcompañero);
    }

    private void Update()
    {
        // Pulso de selección: crece/decrece un 1% mientras está apuntado.
        if (apuntado)
        {
            tiempoPulso += Time.deltaTime * velocidadPulso;
            transform.localScale = escalaOriginal * (1f + amplitudPulso * Mathf.Sin(tiempoPulso));
        }

        // Mientras duerme, regenera el cansancio y no procesa tareas.
        if (estadoActual == Estadocompañero.Dormido)
        {
            ProcesarDormido();
            ActualizarTextoEstado();
            return;
        }

        // Mientras está en recreo, recupera el cansancio y no procesa tareas.
        if (estadoActual == Estadocompañero.Recreo)
        {
            ProcesarRecreo();
            ActualizarTextoEstado();
            return;
        }

        if (estadoActual == Estadocompañero.Trabajando)
        {
            ProcesarTarea();
        }

        ActualizarTextoEstado();
    }

    public void SetApuntado(bool apuntado)
    {
        this.apuntado = apuntado;

        // Al dejar de apuntar, restauramos la escala original y reiniciamos el pulso.
        if (!apuntado)
        {
            tiempoPulso = 0f;
            transform.localScale = escalaOriginal;
        }
    }

    private void ActualizarTextoEstado()
    {
        if (workerDisplay == null) return;

        string texto;
        if (estadoActual == Estadocompañero.Trabajando)
            texto = $"{estadoActual} ({Mathf.Max(0, progresoTarea):F1}s)";
        else if (estadoActual == Estadocompañero.Dormido)
            texto = $"Dormido ({Mathf.Max(0, temporizadorDormido):F1}s)";
        else if (estadoActual == Estadocompañero.Recreo)
            texto = $"Recreo ({Mathf.Max(0, temporizadorRecreo):F1}s)";
        else
            texto = estadoActual.ToString();

        workerDisplay.ActualizarEstado(estadoActual, texto);
    }

    // MÉTODOS PÚBLICOS ---------------------------------

    public void AsignarTarea(Tarea nuevaTarea)
    {
        if (nuevaTarea == null)
        {
            Debug.LogWarning("Se intentó asignar una tarea nula.");
            return;
        }

        // Dormido o en recreo: no puede recibir tareas.
        if (estadoActual == Estadocompañero.Dormido || estadoActual == Estadocompañero.Recreo)
        {
            Debug.Log($"{nombre} no puede recibir tareas ahora ({estadoActual}).");
            return;
        }

        // Cada tarea asignada baja el cansancio (correcta o incorrecta).
        ReducirCansancio(cansancioPorTarea);

        // Si no es de su categoría, NO la recibe y se enoja.
        if (!EsRolCorrecto(nuevaTarea.rolRequerido))
        {
            RechazarTarea(nuevaTarea);
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
            Estadocompañero nuevoEstado = EstadoParaTarea(tareaActual);

            CambiarEstado(nuevoEstado);
            Debug.Log($"{nombre} comenzó '{tareaActual.nombreTarea}' (Rol requerido: {tareaActual.rolRequerido}, Rol propio: {rolcompañero}) - {(esCorrecta ? "¡Correcta!" : "Incorrecta")}");

            if (nuevoEstado == Estadocompañero.Trabajando)
            {
                ResetearTemporizadorDistraccion();
            }
            else
            {
                ReproducirSonido(sonidoEnojado);
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

        ReproducirSonido(sonidoTareaCompletada);
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
        if (animator == null)
        {
            Debug.LogWarning($"{nombre}: NO tiene Animator asignado.");
            return;
        }

        string nombreEstado = estadoActual.ToString();

        // Mapeo seguro: si el controller utiliza "Golpeando" para "Enojado" o "boludeando" para "Distraido"
        if (animator.HasState(0, Animator.StringToHash(nombreEstado)))
        {
            animator.Play(nombreEstado, 0, 0f);
        }
        else if (estadoActual == Estadocompañero.Enojado && animator.HasState(0, Animator.StringToHash("Golpeando")))
        {
            animator.Play("Golpeando", 0, 0f);
        }
        else if (estadoActual == Estadocompañero.Distraido && animator.HasState(0, Animator.StringToHash("boludeando")))
        {
            animator.Play("boludeando", 0, 0f);
        }
        else if (estadoActual == Estadocompañero.Dormido && animator.HasState(0, Animator.StringToHash("Esperando")))
        {
            // No hay animación de dormir; dejamos la pose de espera mientras duerme.
            animator.Play("Esperando", 0, 0f);
        }
        else if (estadoActual == Estadocompañero.Recreo)
        {
            ReproducirAnimacionRecreo();
        }
        else
        {
            Debug.LogWarning($"{nombre}: El Animator no contiene el estado '{nombreEstado}'.");
        }
    }

    // MÉTODOS DE DISTRACCIÓN ---------------------------------

    private void IniciarDistraccion()
    {
        if (estadoActual != Estadocompañero.Trabajando || pausadoPorDistraccion) return;

        CambiarEstado(Estadocompañero.Distraido);
        pausadoPorDistraccion = true;
        ReproducirSonido(sonidoDistraccion);
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
            ReproducirSonido(sonidoVuelveATrabajar);
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

    // MÉTODOS DE CANSANCIO / SUEÑO ---------------------------------

    private Estadocompañero EstadoParaTarea(Tarea tarea)
    {
        return (tarea.rolRequerido == rolcompañero) ? Estadocompañero.Trabajando : Estadocompañero.Enojado;
    }

    /// <summary>
    /// Reduce el cansancio al dar una tarea. Si llega a 0, el compañero se duerme.
    /// Mientras duerme no se reduce (está recuperándose).
    /// </summary>
    private void ReducirCansancio(float cantidad)
    {
        if (estadoActual == Estadocompañero.Dormido) return;

        cansancio = Mathf.Max(0f, cansancio - cantidad);
        ActualizarBarra();

        if (cansancio <= 0f)
        {
            Dormir();
        }
    }

    /// <summary>
    /// Rechaza una tarea de otra categoría: no la recibe y se enoja un instante.
    /// El cansancio ya se descontó en AsignarTarea (se cansa igual).
    /// </summary>
    private void RechazarTarea(Tarea tarea)
    {
        ReproducirSonido(sonidoEnojado);
        Debug.Log($"{nombre} rechazó la tarea '{tarea.nombreTarea}' (no es de su categoría) y se enojó.");

        // Dormido no reacciona.
        if (estadoActual == Estadocompañero.Dormido) return;

        if (rutinaEnojado != null) StopCoroutine(rutinaEnojado);
        rutinaEnojado = StartCoroutine(ReaccionEnojado());
    }

    private IEnumerator ReaccionEnojado()
    {
        CambiarEstado(Estadocompañero.Enojado);

        yield return new WaitForSeconds(duracionEnojado);

        rutinaEnojado = null;

        // Retoma la tarea que estaba haciendo o vuelve a esperar.
        if (tareaActual != null)
            CambiarEstado(Estadocompañero.Trabajando);
        else
            CambiarEstado(Estadocompañero.Esperando);
    }

    private void Dormir()
    {
        if (estadoActual == Estadocompañero.Dormido) return;

        // Cancela cualquier distracción activa.
        if (rutinaDistraccion != null)
        {
            StopCoroutine(rutinaDistraccion);
            rutinaDistraccion = null;
        }
        pausadoPorDistraccion = false;

        temporizadorDormido = duracionDormido;
        cansancio = 0f;

        CambiarEstado(Estadocompañero.Dormido);
        AplicarDesaturacion(true);
        ActualizarBarra();

        // Penalización de dinero por quedarse dormido.
        if (OleadasManager.Instancia != null)
        {
            OleadasManager.Instancia.RegistrarCompaneroDormido(PuntoApuntado);
        }

        Debug.Log($"{nombre} está completamente cansado y se quedó dormido ({duracionDormido}s).");
    }

    /// <summary>
    /// Mientras duerme, la barra va creciendo desde 0 hasta la mitad
    /// (cansancioAlDespertar) a lo largo de duracionDormido segundos.
    /// </summary>
    private void ProcesarDormido()
    {
        temporizadorDormido -= Time.deltaTime;

        float t = Mathf.Clamp01(1f - temporizadorDormido / duracionDormido);
        cansancio = Mathf.Lerp(0f, cansancioAlDespertar, t);
        ActualizarBarra();

        if (temporizadorDormido <= 0f)
        {
            Despertar();
        }
    }

    private void Despertar()
    {
        cansancio = cansancioAlDespertar;
        AplicarDesaturacion(false);
        ActualizarBarra();

        Debug.Log($"{nombre} despertó con la barra al {cansancio:F0}%.");

        // Si estaba a mitad de una tarea, la retoma; si no, arranca la siguiente.
        if (tareaActual != null)
        {
            CambiarEstado(EstadoParaTarea(tareaActual));
            ResetearTemporizadorDistraccion();
        }
        else
        {
            IniciarSiguienteTarea();
        }
    }

    // MÉTODOS DE RECREO ---------------------------------

    /// <summary>
    /// Envía al compañero a recreo: lo mueve al destino, cambia a estado Recreo
    /// y va recuperando toda la barra de cansancio durante duracionRecreo segundos.
    /// Devuelve false si no puede ir (ya está en recreo, dormido, o ya hay 1 en recreo).
    /// </summary>
    public bool EnviarARecreo(Vector3 destino)
    {
        if (estadoActual == Estadocompañero.Recreo || estadoActual == Estadocompañero.Dormido)
            return false;

        if (cantidadEnRecreo >= 1)
            return false;

        // Detener tarea/distracción.
        if (rutinaDistraccion != null)
        {
            StopCoroutine(rutinaDistraccion);
            rutinaDistraccion = null;
        }
        pausadoPorDistraccion = false;

        if (rutinaEnojado != null)
        {
            StopCoroutine(rutinaEnojado);
            rutinaEnojado = null;
        }

        posicionOriginal = transform.position;
        posicionRecreo = destino;
        temporizadorRecreo = duracionRecreo;
        cansancioInicioRecreo = cansancio;

        enCelebracion = false;
        cantidadEnRecreo++;
        CambiarEstado(Estadocompañero.Recreo);

        // Moverlo al área de recreo.
        transform.position = destino;

        Debug.Log($"{nombre} se fue a recreo ({duracionRecreo}s).");
        return true;
    }

    private void ProcesarRecreo()
    {
        // Mantenerlo en el punto de recreo.
        transform.position = posicionRecreo;

        temporizadorRecreo -= Time.deltaTime;
        float t = Mathf.Clamp01(1f - temporizadorRecreo / duracionRecreo);
        cansancio = Mathf.Lerp(cansancioInicioRecreo, cansancioMaximo, t);
        ActualizarBarra();

        // Mantener la animación de recreo en bucle hasta terminar el recreo.
        if (animator != null)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime >= 0.98f)
            {
                animator.Play(stateInfo.shortNameHash, 0, 0f);
            }
        }

        if (temporizadorRecreo <= 0f)
        {
            FinalizarRecreo();
        }
    }

    private void FinalizarRecreo()
    {
        cansancio = cansancioMaximo;
        ActualizarBarra();

        // Solo decrementa el contador si fue un recreo normal (no una celebración).
        if (!enCelebracion)
            cantidadEnRecreo--;
        enCelebracion = false;

        // Volver a su posición original (al escritorio).
        transform.position = posicionOriginal;

        // Restaurar el Animator Controller original.
        RestaurarController();

        Debug.Log($"{nombre} terminó su recreo con la barra llena y volvió a su lugar.");

        if (tareaActual != null)
        {
            CambiarEstado(EstadoParaTarea(tareaActual));
            ResetearTemporizadorDistraccion();
        }
        else
        {
            // Retoma la siguiente tarea en cola, o queda esperando.
            IniciarSiguienteTarea();
        }
    }

    /// <summary>
    /// Reproduce la animación de recreo. Si se asignó un clip, lo fuerza mediante
    /// un AnimatorOverrideController (reemplaza la pose de espera); si no, usa
    /// la pose de espera del controller.
    /// </summary>
    private void ReproducirAnimacionRecreo()
    {
        if (animator == null) return;

        // Usar la animación asignada en este compañero o, si no, la global.
        AnimationClip clip = (animacionRecreo != null) ? animacionRecreo : AnimacionRecreoGlobal;

        if (clip != null)
        {
            // Intento 1: reproducir por nombre de estado (si el clip está en el controller).
            if (animator.HasState(0, Animator.StringToHash(clip.name)))
            {
                animator.Play(clip.name, 0, 0f);
                return;
            }

            // Intento 2: forzar con AnimatorOverrideController reemplazando TODOS
            // los clips, así sin importar el nombre del estado reproduce el archivo.
            if (controllerOriginal == null)
                controllerOriginal = animator.runtimeAnimatorController;

            if (overrideRecreo == null)
            {
                overrideRecreo = new AnimatorOverrideController(controllerOriginal);
                animator.runtimeAnimatorController = overrideRecreo;
            }

            var clips = overrideRecreo.clips;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].overrideClip = clip;
            }
            overrideRecreo.clips = clips;
        }

        if (animator.HasState(0, Animator.StringToHash("Esperando")))
            animator.Play("Esperando", 0, 0f);
    }

    private void RestaurarController()
    {
        if (animator != null && controllerOriginal != null)
            animator.runtimeAnimatorController = controllerOriginal;

        overrideRecreo = null;
    }

    private float ObtenerAltura()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && col.bounds.size.y > 0.01f)
            return col.bounds.size.y;
        return 1.5f;
    }

    /// <summary>
    /// Celebración (usada al ganar): todos los compañeros hacen la animación de
    /// recreo, elevados, sin contar contra el límite de recreo.
    /// </summary>
    public void Celebrar()
    {
        if (estadoActual == Estadocompañero.Dormido || estadoActual == Estadocompañero.Recreo) return;

        posicionOriginal = transform.position;
        temporizadorRecreo = duracionRecreo;
        cansancioInicioRecreo = cansancio;

        float altura = ObtenerAltura();
        // Se eleva un porcentaje de su altura por encima de su posición.
        posicionRecreo = posicionOriginal + Vector3.up * (altura * porcentajeAlturaRecreo / 100f);

        enCelebracion = true;
        CambiarEstado(Estadocompañero.Recreo);
        transform.position = posicionRecreo;
    }

    private void ActualizarBarra()
    {
        if (barraCansancio != null)
        {
            barraCansancio.SetProgreso(cansancio / Mathf.Max(0.0001f, cansancioMaximo));
        }
    }

    /// <summary>
    /// Cachea los materiales del cuerpo (mesh renderers) y sus colores base para
    /// poder desaturarlos al dormir y restaurarlos al despertar.
    /// </summary>
    private void CapturarMaterialesCuerpo()
    {
        coloresBaseCuerpo = new Dictionary<Material, Color>();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            // .materials devuelve instancias propias de cada renderer.
            Material[] mats = r.materials;
            foreach (Material m in mats)
            {
                if (m == null || coloresBaseCuerpo.ContainsKey(m))
                    continue;

                Color baseColor = m.HasProperty("_BaseColor")
                    ? m.GetColor("_BaseColor")
                    : m.color;

                coloresBaseCuerpo[m] = baseColor;
            }
        }
    }

    /// <summary>
    /// Desatura (pone en gris) o restaura el color de los materiales del cuerpo.
    /// Se usa al dormir (gris) y al despertar (color original).
    /// </summary>
    private void AplicarDesaturacion(bool desaturar)
    {
        if (coloresBaseCuerpo == null)
            return;

        foreach (var kv in coloresBaseCuerpo)
        {
            Material m = kv.Key;
            if (m == null)
                continue;

            Color objetivo;
            if (desaturar)
            {
                float g = kv.Value.grayscale;
                objetivo = new Color(g, g, g, kv.Value.a);
            }
            else
            {
                objetivo = kv.Value;
            }

            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", objetivo);
            m.SetColor("_Color", objetivo);
        }
    }

    /// <summary>
    /// Busca el canvas "barraCansancio" (hijo del workerDisplay o, como respaldo,
    /// en cualquier descendiente) y le agrega/obtiene el componente BarraCansancio.
    /// </summary>
    private BarraCansancio ObtenerBarraCansancio()
    {
        Transform nodo = null;

        if (workerDisplay != null)
            nodo = workerDisplay.transform.Find("barraCansancio");

        if (nodo == null)
        {
            Transform[] todos = GetComponentsInChildren<Transform>(true);
            foreach (Transform hijo in todos)
            {
                if (hijo.name == "barraCansancio")
                {
                    nodo = hijo;
                    break;
                }
            }
        }

        if (nodo == null)
            return null;

        BarraCansancio barra = nodo.GetComponent<BarraCansancio>();
        if (barra == null)
            barra = nodo.gameObject.AddComponent<BarraCansancio>();

        return barra;
    }

    public bool EsRolCorrecto(RolTarea rol)
    {
        return rolcompañero == rol;
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
