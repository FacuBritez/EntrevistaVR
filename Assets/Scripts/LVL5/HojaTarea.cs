using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;

public class HojaTarea : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text textoTarea;

    public string nombreTarea = "Tarea física";
    public RolTarea rolRequerido = RolTarea.Programador;
    public float duracionBase = 5f;
    public bool EsTutorial = false; // para distinguir si es la tarea tutorial

    [SerializeField] private float alturaMaxima = 0.9f;

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoSalida;

    private float velocidadSubida;
    private bool salio = false;
    private XRGrabInteractable grab;
    private JugadorAsignadorTareas jugador;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    void Start()
    {
        posicionInicial = transform.position;
        rotacionInicial = transform.rotation;

        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);

        if (sonidoSalida != null)
        {
            velocidadSubida = alturaMaxima / sonidoSalida.length;
            audioSource.PlayOneShot(sonidoSalida);
        }
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        jugador = args.interactorObject.transform.GetComponentInParent<JugadorAsignadorTareas>();
        if (jugador != null) jugador.AgarrarTarea(grab, args.interactorObject.transform);

        // Notificar al OleadasManager si es la tarea tutorial
        if (EsTutorial && OleadasManager.Instancia != null)
            OleadasManager.Instancia.AgarrarTareaTutorial();
    }

    public void ActualizarTexto()
    {
        if (textoTarea != null)
            textoTarea.text = nombreTarea;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (jugador != null) jugador.SoltarTarea();
        jugador = null;
    }

    void Update()
    {
        if (!salio)
        {
            transform.position += Vector3.up * Time.deltaTime * velocidadSubida;
            if (transform.position.y >= alturaMaxima)
            {
                salio = true;
            }
        }
    }

    // Método para resetear la posición (llamado desde OleadasManager cuando falla)
    public void ResetearPosicion()
    {
        transform.position = posicionInicial;
        transform.rotation = rotacionInicial;
        salio = false;
        // Reiniciar animación de salida (opcional)
        if (sonidoSalida != null)
            audioSource.PlayOneShot(sonidoSalida);
    }
}