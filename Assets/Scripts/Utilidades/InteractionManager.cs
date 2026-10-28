using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager instance {get; private set; }
    
    public bool panel1, panel2, panel3, panelBotonApagado;
    public string clave = "1234";
    public string regClave;

    public Animation Puerta;
    private void Awake()
    {
        if(instance != this && instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
        }

        panel1 = false;
        panel2 = false;
        panel3 = false;
        panelBotonApagado = true;
    }

    void Update()
    {
        isPanelActive();
    }

    public void isPanelActive()
    {
        if ((panel1 && panel2 && panel3) && panelBotonApagado)
        {
            Debug.Log("Paneles encendidos, puede activar botones");
            panelBotonApagado = false;
        }

        if((!panel1 || !panel2 || !panel3) && !panelBotonApagado)
        {
            Debug.Log("Panel apagado, No puedes activar");
            panelBotonApagado = true;
        }
    }

    public void OpenDoors()
    {
        Puerta.Play();
    }
}
