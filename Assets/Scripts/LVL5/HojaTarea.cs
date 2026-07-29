using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HojaTarea : MonoBehaviour
{
    public string nombreTarea = "Tarea física";
    public RolTarea rolRequerido = RolTarea.Programador;
    public float duracionBase = 5f;

    [SerializeField] private float velocidadSubida = 0.1f;
    [SerializeField] private float alturaMaxima = 0.9f;

    private bool salio = false;
    private XRGrabInteractable grab;
    private JugadorAsignadorTareas jugador;

    void Start()
    {
        grab = GetComponent<XRGrabInteractable>();
        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        jugador = args.interactorObject.transform.GetComponentInParent<JugadorAsignadorTareas>();
        if (jugador != null) jugador.AgarrarTarea(grab);
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
}