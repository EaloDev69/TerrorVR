using UnityEngine;
using UnityEngine.Events;

// Va en el objeto de la hitbox de la linterna ("AreaDeteccionFantasmal"), con su collider en modo Trigger.
// Detecta al fantasma por TAG (no por un collider concreto). No le hace nada: el fantasma no se elimina.
public class DeteccionFantasma : MonoBehaviour
{
    [SerializeField] string tagFantasma = "Ghost";
    [SerializeField] UnityEvent alEntrarFantasma;   // conecta aqui tu animacion desde el Inspector

    int collidersDentro;   // el fantasma puede tener varios colliders: el evento salta una sola vez

    public bool HayFantasma => collidersDentro > 0;

    void OnTriggerEnter(Collider other)
    {
        if (!EsFantasma(other)) return;

        collidersDentro++;
        if (collidersDentro != 1) return;

        // TODO animacion: aqui va tu animacion de reaccion
        // Ejemplo: animator.SetTrigger("Reaccion");

        alEntrarFantasma.Invoke();
    }
    
    void OnDisable() => collidersDentro = 0;

    // El tag puede estar en el collider o en el objeto raiz con el Rigidbody
    bool EsFantasma(Collider other)
    {
        if (other.CompareTag(tagFantasma)) return true;
        return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(tagFantasma);
    }
}
