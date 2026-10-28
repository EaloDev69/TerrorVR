using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class TagSocket : XRSocketInteractor
{
    public string targetTag;

    public override bool CanHover(IXRHoverInteractable interactable)
    {
        return base.CanHover(interactable) && interactable.transform.tag == targetTag;
    }

    public override bool CanSelect(IXRSelectInteractable interactable)
    {
        return base.CanSelect(interactable) && interactable.transform.tag == targetTag;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        ActivarPanel(args.interactableObject);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        DesactivarPanel(args.interactableObject);
    }

    public void ActivarPanel(IXRSelectInteractable interactable)
    {
        switch (interactable.transform.tag)
        {
            case "Liberadora":
                InteractionManager.instance.panel1 = true;
                break;

            case "Railgun":
                InteractionManager.instance.panel2 = true;
                InteractionManager.instance.panel3 = true;
                break;
        }
    }

    public void DesactivarPanel(IXRSelectInteractable interactable)
    {
        switch (interactable.transform.tag)
        {
            case "Liberadora":
                InteractionManager.instance.panel1 = false;
                break;

            case "Railgun":
                InteractionManager.instance.panel2 = false;
                InteractionManager.instance.panel3 = false;
                break;
        }       
    }
}
