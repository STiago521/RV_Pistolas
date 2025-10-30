using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRPickupUseMode : MonoBehaviour
{
    [Header("Refs")]
    public XRGrabInteractable cameraItem;      // Cámara con XRGrabInteractable
    public GameObject leftHandRoot;            // GO raíz mano izquierda (malla/rig)
    public GameObject rightHandRoot;           // GO raíz mano derecha
    public GameObject otherObject;             // Objeto que se activa en modo "usar"

    [Header("Attach EN LA CÁMARA")]
    public Transform gripRight;                // hijo en cameraItem (pose derecha)
    public Transform gripLeft;                 // hijo en cameraItem (pose izquierda)

    [Header("Interactores (XRDirectInteractor)")]
    public XRBaseInteractor leftHandInteractor;
    public XRBaseInteractor rightHandInteractor;

    [Header("Controles")]
    public KeyCode pickupKey = KeyCode.G;      // Recoger (no suelta)
    public KeyCode useKey = KeyCode.V;      // Modo usar ON/OFF

    [Header("Pickup")]
    public Transform playerRef;                // referencia (cabeza/pecho)
    public float pickupRadius = 2.0f;          // distancia máxima para G

    bool useMode;                              // ¿modo usar activo?

    void Start()
    {
        if (!cameraItem || !leftHandRoot || !rightHandRoot || !otherObject)
            Debug.LogWarning("Asigna cameraItem, leftHandRoot, rightHandRoot y otherObject.");

        // Suscripción: cuando se agarre normalmente, coloca el grip correcto
        cameraItem.selectEntered.AddListener(OnGrabSetGrip);

        // Estado inicial
        SetHandsVisible(true);
        otherObject.SetActive(false);
        cameraItem.gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        if (cameraItem) cameraItem.selectEntered.RemoveListener(OnGrabSetGrip);
    }

    void Update()
    {
        // Recoger con G (no suelta)
        if (Input.GetKeyDown(pickupKey))
            TryPickupWithG();

        // Usar: manos OFF + cámara OFF + otro ON (y revertir)
        if (Input.GetKeyDown(useKey))
            ToggleUseMode();
    }

    // --- Alinear grips al agarrar normalmente ---
    void OnGrabSetGrip(SelectEnterEventArgs args)
    {
        if (!cameraItem) return;

        // ¿Qué mano agarró?
        bool isLeft = args.interactorObject.transform.name.ToLower().Contains("left");
        Transform grip = isLeft ? gripLeft : gripRight;

        if (!grip) return;

        // 1) Fija el attach del ITEM al grip adecuado
        cameraItem.attachTransform = grip;

        // 2) (Re)coloca el attach de la MANO sobre el grip (por si su attach está distinto)
        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor && interactor.attachTransform)
        {
            interactor.attachTransform.position = grip.position;
            interactor.attachTransform.rotation = grip.rotation;
        }
    }

    // --- Recoger con G: NO suelta si ya está agarrada ---
    void TryPickupWithG()
    {
        if (!cameraItem) return;

        // Si ya está agarrada por cualquier mano, no hacemos nada
        if (cameraItem.isSelected) return;

        // Comprobar distancia al jugador
        if (playerRef && Vector3.Distance(playerRef.position, cameraItem.transform.position) > pickupRadius)
            return;

        // Elegir mano disponible (prioriza derecha)
        XRBaseInteractor hand = null;
        if (rightHandInteractor && rightHandInteractor.isActiveAndEnabled) hand = rightHandInteractor;
        else if (leftHandInteractor && leftHandInteractor.isActiveAndEnabled) hand = leftHandInteractor;

        if (!hand)
        {
            Debug.LogWarning("No hay interactor de mano activo para recoger con G.");
            return;
        }

        // Determinar grip por mano
        bool isLeft = hand.transform.name.ToLower().Contains("left");
        Transform grip = isLeft ? gripLeft : gripRight;

        if (grip)
        {
            // Coloca el attach de la mano sobre el grip ANTES de seleccionar
            if (hand.attachTransform)
            {
                hand.attachTransform.position = grip.position;
                hand.attachTransform.rotation = grip.rotation;
            }
            // Fija attach del item al grip
            cameraItem.attachTransform = grip;
        }

        // Forzar selección XR (no se suelta)
        var im = cameraItem.interactionManager ?? hand.interactionManager;
        if (im != null)
            im.SelectEnter(hand as IXRSelectInteractor, cameraItem as IXRSelectInteractable);
        else
            Debug.LogWarning("Sin XRInteractionManager para SelectEnter.");
    }

    // --- Modo usar (V) ---
    void ToggleUseMode()
    {
        useMode = !useMode;

        if (useMode)
        {
            // 1) Apagar manos
            SetHandsVisible(false);

            // 2) Si está agarrada, soltar de forma segura y apagar la cámara
            SafeReleaseIfSelected();
            cameraItem.gameObject.SetActive(false);

            // 3) Activar el objeto alterno
            otherObject.SetActive(true);
        }
        else
        {
            // Revertir
            otherObject.SetActive(false);
            cameraItem.gameObject.SetActive(true);
            SetHandsVisible(true);
        }
    }

    // Utilidades
    void SetHandsVisible(bool visible)
    {
        if (leftHandRoot) leftHandRoot.SetActive(visible);
        if (rightHandRoot) rightHandRoot.SetActive(visible);
    }

    void SafeReleaseIfSelected()
    {
        if (!cameraItem || !cameraItem.isSelected) return;

        var im = cameraItem.interactionManager;
        if (im == null) return;

        foreach (var ix in cameraItem.interactorsSelecting.ToList())
            if (ix is IXRSelectInteractor sel) im.SelectExit(sel, cameraItem);
    }
}
