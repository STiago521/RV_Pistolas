using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class HandedGrabSnap : MonoBehaviour
{
    [Header("Puntos de agarre")]
    public Transform leftAttach;
    public Transform rightAttach;

    [Header("Manos del XR Rig (asigna en el inspector)")]
    public XRBaseInteractor leftHandInteractor;
    public XRBaseInteractor rightHandInteractor;

    private XRGrabInteractable grab;
    private Transform originalAttach;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        originalAttach = grab.attachTransform;

        grab.selectEntered.AddListener(OnSelectEntered);
        grab.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnSelectEntered);
        grab.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Detecta con precisión cuál interactor está agarrando
        bool isLeft = args.interactorObject == leftHandInteractor;
        bool isRight = args.interactorObject == rightHandInteractor;

        Transform chosenAttach = null;

        if (isLeft && leftAttach != null)
            chosenAttach = leftAttach;
        else if (isRight && rightAttach != null)
            chosenAttach = rightAttach;
        else
            chosenAttach = originalAttach;

        grab.attachTransform = chosenAttach;
        grab.useDynamicAttach = false; // evita offsets adicionales
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        grab.attachTransform = originalAttach;
    }
}



