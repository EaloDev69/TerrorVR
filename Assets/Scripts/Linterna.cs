
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Linterna : MonoBehaviour
{
    [Header ("Elementos Linterna")]
    [SerializeField] public GameObject Spotlight;
    [SerializeField] float batteryTime;
    [SerializeField] public bool encendido;
    //[SerializeField] Transform SocketLinterna;
 
    float contador;
    Coroutine rutinaBateria;
    private XRGrabInteractable grabInteract;

    void Start()
    {
        Spotlight.gameObject.SetActive(false);
        contador = batteryTime;
        encendido = false;

        grabInteract =GetComponent<XRGrabInteractable>();

        grabInteract.activated.AddListener (x => onActivated());
    }

    private void onActivated()
    {
        if (encendido) Apagar();
        else Encender();
    }

    void Encender()
    {
        if (contador <= 0f) return;
        encendido = true;
        Spotlight.gameObject.SetActive(true);
        rutinaBateria = StartCoroutine(GastarBateria());
    }


    void Apagar()
    {
        encendido = false;
        Spotlight.SetActive(false);
        if (rutinaBateria != null) { StopCoroutine(rutinaBateria); rutinaBateria = null; }
    }

    void OnDestroy()
    {
        if (grabInteract != null) grabInteract.activated.RemoveAllListeners();
    }

    IEnumerator GastarBateria()
    {
        while (contador > 0f)
        {
            contador -= Time.deltaTime;
            yield return null;
        }
        Apagar();
    }


    void OnTriggerEnter(Collider other)
    {
        
    }
}
