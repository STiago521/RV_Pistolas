using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;  // ← necesario para XRGrabInteractable
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRPickupToggleModes : MonoBehaviour
{
    [Header("Refs")]
    public GameObject cameraItem;      // prop con XRGrabInteractable
    public GameObject handsRoot;       // rig/modelos de manos
    public GameObject otherObject;     // objeto del “modo uso”

    [Header("Attach EN LA CÁMARA (no en las manos)")]
    public Transform gripRight;        // hijo del cameraItem: pose mano derecha
    public Transform gripLeft;         // hijo del cameraItem: pose mano izquierda

    [Header("Teclas")]
    public KeyCode storeKey = KeyCode.C; // guardar: SOLO manos ON/OFF
    public KeyCode useKey = KeyCode.V; // modo uso: manos+cam OFF, other ON

    bool storedHands = false;   // “guardado” = manos ocultas
    bool useMode = false;       // modo uso activo

    XRGrabInteractable grab;    // componente del cameraItem

    void Start()
    {
        if (!cameraItem || !handsRoot || !otherObject)
            Debug.LogWarning("Asigna cameraItem, handsRoot y otherObject.");

        // XRGrabInteractable + setup del attach en el ITEM
        grab = cameraItem ? cameraItem.GetComponent<XRGrabInteractable>() : null;
        if (grab == null)
            Debug.LogWarning("cameraItem no tiene XRGrabInteractable.");

        if (grab != null)
        {
          // que use la pose del attach del item
            grab.selectEntered.AddListener(OnGrab); // decidir grip L/R al agarrar
        }

        // Estado inicial
        if (handsRoot) handsRoot.SetActive(true);
        if (otherObject) otherObject.SetActive(false);
        if (cameraItem) cameraItem.SetActive(true);
    }

    void OnDestroy()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(OnGrab);
    }

    void Update()
    {
        if (Input.GetKeyDown(storeKey)) ToggleStoreHands();
        if (Input.GetKeyDown(useKey)) ToggleUseMode();
    }

    // Al agarrar con XR: elegir el grip correcto (izq/der) DEL ITEM
    void OnGrab(SelectEnterEventArgs args)
    {
        if (grab == null) return;

        // Intentamos inferir mano por nombre del interactor
        var interactorName = args.interactorObject.transform.name.ToLower();

        if (interactorName.Contains("left"))
        {
            if (gripLeft) grab.attachTransform = gripLeft;
        }
        else // por defecto derecha
        {
            if (gripRight) grab.attachTransform = gripRight;
        }
        // Con matchAttachTransform = true, la mano se alinea a este attach del ITEM
    }

    void ToggleStoreHands()
    {
        // No mezclar con modo uso
        if (useMode) return;

        storedHands = !storedHands;
        if (handsRoot) handsRoot.SetActive(!storedHands);
        // La cámara NO se toca en “store”
    }

    void ToggleUseMode()
    {
        useMode = !useMode;

        if (useMode)
        {
            // Entrar a “usar”: manos OFF + cámara OFF + otro ON
            if (handsRoot) handsRoot.SetActive(false);

            // Si estaba agarrada, la soltamos limpio antes de ocultarla (opcional seguro)
            SafeReleaseIfSelected();

            if (cameraItem) cameraItem.SetActive(false);
            if (otherObject) otherObject.SetActive(true);
        }
        else
        {
            // Salir: otro OFF, cámara ON, manos según “stored”
            if (otherObject) otherObject.SetActive(false);
            if (cameraItem) cameraItem.SetActive(true);
            if (handsRoot) handsRoot.SetActive(!storedHands);
        }
    }

    // Suelta la cámara si está seleccionada, para evitar estados raros al ocultarla
    void SafeReleaseIfSelected()
    {
        if (grab == null) return;
        var im = grab.interactionManager;
        if (im == null) return;

        var selecting = grab.interactorsSelecting.ToList();
        foreach (var ix in selecting)
        {
            if (ix is IXRSelectInteractor sel)
                im.SelectExit(sel, grab); // terminar selección
        }
    }
}
