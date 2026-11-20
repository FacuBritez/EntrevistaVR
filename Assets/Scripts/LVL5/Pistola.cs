using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Lógica de la "Pistola" del LVL5:
/// - Se agarra con el botón de agarre (Select/grip = "gatillo trasero").
/// - Tiene un láser para apuntar.
/// - Al gatillar (Activate/trigger = "gatillo del costado") manda al compañero
///   apuntado al "planoRecreo" (estado Recreo).
/// - Solo deja mandar hasta 2 compañeros a recreo a la vez.
/// - Cuando no se puede usar (ya hay 2 en recreo) baja su opacidad.
/// - Al soltarse, siempre vuelve a su posición/orientación de origen.
///
/// Setup: agregar a un GameObject con XRGrabInteractable, Rigidbody y un
/// LineRenderer (para el láser). Asignar las referencias en el Inspector.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class Pistola : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private LineRenderer lineaLaser;
    [Tooltip("Punto desde donde sale el láser (normalmente la boca del cañón).")]
    [SerializeField] private Transform puntoOrigenLaser;
    [SerializeField] private Transform planoRecreo;
    [SerializeField] private float rangoLaser = 10f;

    [Header("Apariencia cuando no se puede usar")]
    [SerializeField] private Renderer[] renderersPistola;
    [SerializeField] private float opacidadDeshabilitada = 0.2f;

    [Header("Láser")]
    [SerializeField] private Color colorLaser = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float anchoLaser = 0.01f;

    [Header("Sonido de disparo")]
    [Tooltip("Sonido que se reproduce al disparar (arrastralo desde el Inspector).")]
    [SerializeField] private AudioClip sonidoDisparo;

    private XRGrabInteractable grab;
    private Rigidbody rb;
    private AudioSource audioSource;
    private Vector3 posicionOrigen;
    private Quaternion rotacionOrigen;
    private bool agarrada = false;
    private Coroutine rutinaVuelta;
    private bool gatilloAntes = false;
    private Dictionary<Material, Color> coloresOriginalesPistola;

    private void Start()
    {
        posicionOrigen = transform.position;
        rotacionOrigen = transform.rotation;

        rb = GetComponent<Rigidbody>();

        // AudioSource para el sonido de disparo.
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Auto-configuración para reducir el setup en el editor.
        if (planoRecreo == null)
        {
            // El planoRecreo está desactivado en la escena, así que hay que
            // buscar también objetos inactivos.
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "planoRecreo")
                {
                    planoRecreo = t;
                    break;
                }
            }
        }

        if (lineaLaser == null)
            lineaLaser = gameObject.AddComponent<LineRenderer>();

        if (renderersPistola == null || renderersPistola.Length == 0)
            renderersPistola = GetComponentsInChildren<Renderer>();

        // Cachear los colores originales para poder hacer blanco y negro.
        coloresOriginalesPistola = new Dictionary<Material, Color>();
        foreach (var r in renderersPistola)
        {
            if (r == null) continue;
            foreach (var m in r.materials)
            {
                if (m != null && !coloresOriginalesPistola.ContainsKey(m))
                {
                    coloresOriginalesPistola[m] = m.HasProperty("_BaseColor")
                        ? m.GetColor("_BaseColor")
                        : m.color;
                }
            }
        }

        grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
        {
            grab.selectEntered.AddListener(OnAgarrada);
            grab.selectExited.AddListener(OnSoltada);
        }

        if (lineaLaser != null)
        {
            ConfigurarLaser();
            lineaLaser.enabled = false;
        }

        ActualizarApariencia();
    }

    private void Update()
    {
        if (agarrada && lineaLaser != null)
            ActualizarLaser();

        // Detectar el gatillo (trigger) directamente del dispositivo XR.
        bool gatillo = LeerGatillo();
        if (agarrada && gatillo && !gatilloAntes)
        {
            Disparar();
        }
        gatilloAntes = gatillo;

        ActualizarApariencia();
    }

    /// <summary>
    /// Lee el botón de gatillo (trigger) de cualquiera de las dos manos.
    /// </summary>
    private bool LeerGatillo()
    {
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        bool r = false, l = false;
        if (right.isValid) right.TryGetFeatureValue(CommonUsages.triggerButton, out r);
        if (left.isValid) left.TryGetFeatureValue(CommonUsages.triggerButton, out l);

        return r || l;
    }

    private void OnAgarrada(SelectEnterEventArgs args)
    {
        agarrada = true;

        if (rutinaVuelta != null)
        {
            StopCoroutine(rutinaVuelta);
            rutinaVuelta = null;
        }

        if (lineaLaser != null)
            lineaLaser.enabled = true;
    }

    private void OnSoltada(SelectExitEventArgs args)
    {
        agarrada = false;

        if (lineaLaser != null)
            lineaLaser.enabled = false;

        VolverAlOrigen();
    }

    private void Disparar()
    {
        if (planoRecreo == null)
        {
            Debug.LogWarning("🔫 Pistola: no se encontró 'planoRecreo'.");
            return;
        }

        // Sonido de disparo.
        if (audioSource != null && sonidoDisparo != null)
            audioSource.PlayOneShot(sonidoDisparo);

        Compañero comp = DetectarCompañeroApuntado();
        if (comp == null)
        {
            Debug.Log("🔫 Disparo sin impactar a ningún compañero.");
            return;
        }

        if (comp.EnviarARecreo(planoRecreo.position))
            Debug.Log($"🔫 {comp.name} mandado a recreo.");
        else
            Debug.Log($"🔫 {comp.name} no pudo ir a recreo (límite o estado no válido).");
    }

    /// <summary>
    /// Devuelve el primer compañero que está en la línea del láser.
    /// </summary>
    private Compañero DetectarCompañeroApuntado()
    {
        Transform origen = puntoOrigenLaser != null ? puntoOrigenLaser : transform;
        RaycastHit[] hits = Physics.SphereCastAll(origen.position, 0.08f, origen.forward, rangoLaser);
        foreach (var hit in hits)
        {
            Compañero comp = hit.collider.GetComponentInParent<Compañero>();
            if (comp != null)
                return comp;
        }
        return null;
    }

    private void ActualizarLaser()
    {
        Transform origen = puntoOrigenLaser != null ? puntoOrigenLaser : transform;
        lineaLaser.SetPosition(0, origen.position);

        // Auto-apuntado: si hay un compañero en la línea, el láser se engancha a su centro.
        Compañero comp = DetectarCompañeroApuntado();
        if (comp != null)
        {
            lineaLaser.SetPosition(1, comp.PuntoApuntado);
            return;
        }

        Vector3 destino = origen.position + origen.forward * rangoLaser;
        if (Physics.SphereCast(origen.position, 0.05f, origen.forward, out RaycastHit hit, rangoLaser))
            destino = hit.point;

        lineaLaser.SetPosition(1, destino);
    }

    private void ConfigurarLaser()
    {
        if (lineaLaser.material == null ||
            lineaLaser.material.shader == null ||
            lineaLaser.material.shader.name != "Sprites/Default")
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                lineaLaser.material = new Material(shader);
        }

        lineaLaser.useWorldSpace = true;
        lineaLaser.positionCount = 2;
        lineaLaser.widthMultiplier = anchoLaser;
        lineaLaser.startColor = colorLaser;
        lineaLaser.endColor = colorLaser;
    }

    private void VolverAlOrigen()
    {
        if (rutinaVuelta != null)
            StopCoroutine(rutinaVuelta);

        rutinaVuelta = StartCoroutine(AnimacionVuelta());
    }

    private IEnumerator AnimacionVuelta()
    {
        // Congelamos la física para que no se caiga/arroje mientras vuelve.
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        float duracion = 0.5f;
        float t = 0f;
        Vector3 posInicio = transform.position;
        Quaternion rotInicio = transform.rotation;

        while (t < 1f)
        {
            t += Time.deltaTime / duracion;
            float u = Mathf.Clamp01(t);

            transform.position = Vector3.Lerp(posInicio, posicionOrigen, u);
            transform.rotation = Quaternion.Slerp(rotInicio, rotacionOrigen, u);

            yield return null;
        }

        transform.position = posicionOrigen;
        transform.rotation = rotacionOrigen;
        rutinaVuelta = null;
    }

    private void ActualizarApariencia()
    {
        bool usable = Compañero.CantidadEnRecreo < 1;

        if (renderersPistola == null || coloresOriginalesPistola == null)
            return;

        foreach (var kv in coloresOriginalesPistola)
        {
            Material m = kv.Key;
            if (m == null || !m.HasProperty("_BaseColor"))
                continue;

            Color c;
            if (usable)
            {
                // Restaurar el color original.
                c = kv.Value;
            }
            else
            {
                // Blanco y negro + baja opacidad (dithered transparency la da el shader Custom/Puntos).
                float g = kv.Value.grayscale;
                c = new Color(g, g, g, opacidadDeshabilitada);
            }

            m.SetColor("_BaseColor", c);
        }
    }
}
