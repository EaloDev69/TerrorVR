using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class Camara : MonoBehaviour
{
    [Header("Configs camara")]
    [SerializeField] Volume nvis;
    [SerializeField] Collider zonaActivable;   // hitbox en la cabeza del jugador: la camara debe estar dentro
    [SerializeField] float velocidadFade = 6f;
    [SerializeField] bool isActive;

    [Header("Visibilidad del fantasma")]
    [SerializeField] GameObject camaraJugador;   // GameObject con la Camera que ve el jugador (la Main Camera del XR Origin, NO el modelo); vacio = Camera.main
    [SerializeField] string capaFantasma = "Fantasma";

    XRGrabInteractable grabInteract;
    int mascaraFantasma;
    Camera camRender;   // el componente Camera real del jugador (se obtiene de camaraJugador)
    bool visionActiva;

    // true SOLO cuando la camara esta encendida y dentro de la hitbox de la cabeza
    public bool VisionActiva => visionActiva;
    public event System.Action<bool> VisionCambio;

    void Awake()
    {
        grabInteract = GetComponent<XRGrabInteractable>();

        if (camaraJugador != null) camRender = camaraJugador.GetComponentInChildren<Camera>(true);
        if (camRender == null)
        {
            if (camaraJugador != null)
                Debug.LogWarning("Camara: 'camaraJugador' no tiene un componente Camera; se usa Camera.main", this);
            camRender = Camera.main;
        }

        int capa = LayerMask.NameToLayer(capaFantasma);
        if (capa >= 0) mascaraFantasma = 1 << capa;
        else Debug.LogWarning($"Camara: no existe la capa '{capaFantasma}'", this);
        CambiarVision(false); // el fantasma empieza invisible

        if (nvis != null)
        {
            nvis.weight = 0f;
            nvis.priority = 10f; // por encima del Global Volume
        }
    }

    void OnEnable()
    {
        grabInteract.activated.AddListener(OnActivated);
        grabInteract.selectExited.AddListener(OnSoltada);
    }

    void OnDisable()
    {
        if (visionActiva) CambiarVision(false); // si se apaga o destruye, el fantasma vuelve a ser invisible
        grabInteract.activated.RemoveListener(OnActivated);
        grabInteract.selectExited.RemoveListener(OnSoltada);
    }

    void OnActivated(ActivateEventArgs args)
    {
        if (EstaEnZona()) isActive = !isActive;
    }

    void OnSoltada(SelectExitEventArgs args) => isActive = false;

    // Muestra u oculta la capa del fantasma en la camara del jugador
    void CambiarVision(bool activa)
    {
        visionActiva = activa;

        if (camRender != null && mascaraFantasma != 0)
        {
            camRender.cullingMask = activa
                ? camRender.cullingMask | mascaraFantasma
                : camRender.cullingMask & ~mascaraFantasma;
        }

        VisionCambio?.Invoke(activa);
    }

    bool EstaEnZona()
    {
        if (zonaActivable == null) return false;
        Vector3 p = transform.position; // posicion de ESTA camara (el objeto que sostienes)
        return zonaActivable.ClosestPoint(p) == p; // true si esta dentro de la hitbox de la cabeza
    }

    void Update()
    {
        // Estrictamente activa: encendida Y dentro de la hitbox de la cabeza (no depende del fade)
        bool activa = isActive && EstaEnZona();
        if (activa != visionActiva) CambiarVision(activa);

        if (nvis == null) return;
        nvis.weight = Mathf.MoveTowards(nvis.weight, activa ? 1f : 0f, velocidadFade * Time.deltaTime);
    }
}