using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class Camara : MonoBehaviour
{
    [Header("Configs camara")]
    [SerializeField] Volume nvis;
    [SerializeField] Collider zonaActivable;   // collider de la zona (puede ser trigger)
    [SerializeField] Transform cabeza;         // si se deja vacío usa Camera.main
    [SerializeField] float velocidadFade = 6f;
    [SerializeField] bool isActive;

    XRGrabInteractable grabInteract;

    void Awake()
    {
        grabInteract = GetComponent<XRGrabInteractable>();
        if (cabeza == null && Camera.main != null) cabeza = Camera.main.transform;

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
        grabInteract.activated.RemoveListener(OnActivated);
        grabInteract.selectExited.RemoveListener(OnSoltada);
    }

    void OnActivated(ActivateEventArgs args)
    {
        if (EstaEnZona()) isActive = !isActive;
    }

    void OnSoltada(SelectExitEventArgs args) => isActive = false;

    bool EstaEnZona()
    {
        if (zonaActivable == null || cabeza == null) return false;
        Vector3 p = cabeza.position;
        return zonaActivable.ClosestPoint(p) == p; // true si p está dentro
    }

    void Update()
    {
        if (nvis == null) return;

        float objetivo = (isActive && EstaEnZona()) ? 1f : 0f;
        nvis.weight = Mathf.MoveTowards(nvis.weight, objetivo, velocidadFade * Time.deltaTime);
    }
}