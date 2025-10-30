using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRPickupUseMode : MonoBehaviour
{
    // --- Evento estático: avisa un flash (posición, radio) ---
    public static System.Action<Vector3, float> OnFlash;

    [Header("Refs")]
    public XRGrabInteractable cameraItem;      // Cámara con XRGrabInteractable
    public GameObject leftHandRoot;            // mano izquierda (GO raíz)
    public GameObject rightHandRoot;           // mano derecha (GO raíz)
    public GameObject otherObject;             // objeto activo en modo "usar"

    [Header("Attach EN LA CÁMARA")]
    public Transform gripRight;                // pose mano derecha
    public Transform gripLeft;                 // pose mano izquierda

    [Header("Interactores (XRDirectInteractor)")]
    public XRBaseInteractor leftHandInteractor;
    public XRBaseInteractor rightHandInteractor;

    [Header("Controles")]
    public KeyCode pickupKey = KeyCode.G;      // Recoger (no suelta)
    public KeyCode useKey = KeyCode.V;      // Modo usar ON/OFF
    public KeyCode flashKey = KeyCode.Mouse0; // Tecla alternativa al click

    [Header("Pickup")]
    public Transform playerRef;
    public float pickupRadius = 2.0f;

    [Header("Flash de luz")]
    public Light flashLight;                   // Point Light (asígnala)
    public float flashDuration = 0.12f;        // segundos encendida
    public float flashRadius = 5.0f;         // radio para eliminar fantasmas
    public Transform flashOrigin;              // desde dónde medir (si null, usa flashLight.transform)

    bool useMode;

    void Start()
    {
        if (!cameraItem || !leftHandRoot || !rightHandRoot || !otherObject)
            Debug.LogWarning("Asigna cameraItem, leftHandRoot, rightHandRoot y otherObject.");

        cameraItem.selectEntered.AddListener(OnGrabSetGrip);

        SetHandsVisible(true);
        otherObject.SetActive(false);
        cameraItem.gameObject.SetActive(true);

        if (flashLight) flashLight.enabled = false;
    }

    void OnDestroy()
    {
        if (cameraItem) cameraItem.selectEntered.RemoveListener(OnGrabSetGrip);
    }

    void Update()
    {
        if (Input.GetKeyDown(pickupKey)) TryPickupWithG();
        if (Input.GetKeyDown(useKey)) ToggleUseMode();

        // Disparo del flash cuando ESTÁ en uso (manos OFF, cam OFF, other ON)
        if (useMode && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(flashKey)))
            StartCoroutine(DoFlash());
    }

    // Alinear grips al agarrar normalmente
    void OnGrabSetGrip(SelectEnterEventArgs args)
    {
        if (!cameraItem) return;
        bool isLeft = args.interactorObject.transform.name.ToLower().Contains("left");
        Transform grip = isLeft ? gripLeft : gripRight;
        if (!grip) return;

        cameraItem.attachTransform = grip;

        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor && interactor.attachTransform)
        {
            interactor.attachTransform.position = grip.position;
            interactor.attachTransform.rotation = grip.rotation;
        }
    }

    // Recoger con G (no suelta si ya está agarrada)
    void TryPickupWithG()
    {
        if (!cameraItem || cameraItem.isSelected) return;
        if (playerRef && Vector3.Distance(playerRef.position, cameraItem.transform.position) > pickupRadius) return;

        XRBaseInteractor hand = (rightHandInteractor && rightHandInteractor.isActiveAndEnabled)
            ? rightHandInteractor
            : (leftHandInteractor && leftHandInteractor.isActiveAndEnabled ? leftHandInteractor : null);

        if (!hand) { Debug.LogWarning("No hay interactor de mano activo."); return; }

        bool isLeft = hand.transform.name.ToLower().Contains("left");
        Transform grip = isLeft ? gripLeft : gripRight;

        if (grip && hand.attachTransform)
        {
            hand.attachTransform.position = grip.position;
            hand.attachTransform.rotation = grip.rotation;
            cameraItem.attachTransform = grip;
        }

        var im = cameraItem.interactionManager ?? hand.interactionManager;
        if (im != null)
            im.SelectEnter(hand as IXRSelectInteractor, cameraItem as IXRSelectInteractable);
        else
            Debug.LogWarning("Sin XRInteractionManager para SelectEnter.");
    }

    // Modo usar: manos OFF + cam OFF + other ON (y revertir)
    void ToggleUseMode()
    {
        useMode = !useMode;

        if (useMode)
        {
            SetHandsVisible(false);
            SafeReleaseIfSelected();
            cameraItem.gameObject.SetActive(false);
            otherObject.SetActive(true);
        }
        else
        {
            otherObject.SetActive(false);
            cameraItem.gameObject.SetActive(true);
            SetHandsVisible(true);
        }
    }

    IEnumerator DoFlash()
    {
        if (!flashLight) yield break;

        // Posición del flash
        Transform origin = flashOrigin ? flashOrigin : flashLight.transform;

        // Enciende luz
        flashLight.enabled = true;

        // Notifica a los fantasmas para que se eliminen en el radio
        OnFlash?.Invoke(origin.position, flashRadius);

        yield return new WaitForSeconds(flashDuration);

        // Apaga luz
        flashLight.enabled = false;
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
