using UnityEngine;

public class ZoneReturn : MonoBehaviour
{
    [SerializeField] string[] tags = {"camara" , "flash"};

    void OnTriggerEnter(Collider other)
    {
        var item = ObtenerItem(other);
        if (item !=null) item.Entro();
    }

    void OnTriggerExit(Collider other)
    {
        var item = ObtenerItem(other);
        if (item !=null) item.Salio();
    }

    Returner ObtenerItem(Collider other)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return null;
        foreach (var t in tags)
            if (rb.CompareTag(t)) return rb.GetComponent<Returner>();
        return null;
    }
}
