using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Returner : MonoBehaviour
{
    [SerializeField] Transform Socket;

    Rigidbody rb;
    XRGrabInteractable grab;
    int contactosDentro;
    bool pendiente;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();
    }

    void OnEnable() => grab.selectExited.AddListener(onSoltado);
    void OnDisable() => grab.selectExited.RemoveListener(onSoltado);

    public void Entro()
    {
        contactosDentro++;
        pendiente = false;  
    }
    
    public void Salio()
    {
        contactosDentro = Mathf.Max(0, contactosDentro - 1);
        if (contactosDentro > 0) return;

        if (grab.isSelected) pendiente = true; // lo tiene en la mano: espera a que lo suelte
        else Regresar(); 
    }

    void onSoltado(SelectExitEventArgs args)
    {
        if (pendiente && contactosDentro == 0)
            StartCoroutine(RegresarTrasFisica());
    }

    IEnumerator RegresarTrasFisica()
    {
        yield return new WaitForFixedUpdate();
        Regresar();
    }

    void Regresar()
    {
        pendiente = false;
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        transform.SetPositionAndRotation(Socket.position, Socket.rotation);
    }
}
