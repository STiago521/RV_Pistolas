using UnityEngine;

public class CameraEquipManager : MonoBehaviour
{
    [Header("Refs")]
    public GameObject cameraItem;        // cámara prop (el modelo que sostienes)
    public Transform handSocket;         // punto en la mano
    public Transform holsterSocket;      // punto de guardado
    public GameObject handsRoot;         // modelos/rig de manos
    public GameObject otherModeObject;   // objeto a activar en modo alterno

    [Header("Teclas")]
    public KeyCode toggleCameraKey = KeyCode.C;  // sacar/guardar cámara
    public KeyCode toggleModeKey = KeyCode.V;  // activar/desactivar modo alterno

    [Header("Ajustes")]
    public bool freezePhysicsWhenEquipped = true; // congela rigidbody al equipar/guardar
    public bool hideCameraItemInOtherMode = true; // oculta la cámara en modo alterno

    bool cameraEquipped;   // ¿en mano?
    bool otherMode;        // ¿modo alterno activo?
    Rigidbody camRb;
    Collider[] camCols;

    void Awake()
    {
        if (!cameraItem || !handSocket || !holsterSocket)
        {
            Debug.LogError("⚠️ Asigna cameraItem, handSocket y holsterSocket en el Inspector.");
            enabled = false; return;
        }

        camRb = cameraItem.GetComponent<Rigidbody>();
        camCols = cameraItem.GetComponentsInChildren<Collider>(true);

        // Arranca guardada en el holster
        PutInSocket(cameraItem.transform, holsterSocket, true);
        cameraEquipped = false;

        // Asegura estado inicial del modo alterno
        SetOtherMode(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleCameraKey))
        {
            ToggleCameraEquip();
        }

        if (Input.GetKeyDown(toggleModeKey))
        {
            SetOtherMode(!otherMode);
        }
    }

    void ToggleCameraEquip()
    {
        if (otherMode) return; // si estás en modo alterno, no permitas sacar/guardar

        cameraEquipped = !cameraEquipped;
        if (cameraEquipped)
            PutInSocket(cameraItem.transform, handSocket, true);
        else
            PutInSocket(cameraItem.transform, holsterSocket, true);
    }

    void SetOtherMode(bool enable)
    {
        otherMode = enable;

        // 1) Manos ON/OFF
        if (handsRoot) handsRoot.SetActive(!enable);

        // 2) Cámara-item ON/OFF (según preferencia)
        if (hideCameraItemInOtherMode && cameraItem)
        {
            cameraItem.SetActive(!enable);
        }

        // 3) Objeto alterno ON/OFF
        if (otherModeObject) otherModeObject.SetActive(enable);

        // 4) Si entro al modo alterno y la cámara estaba equipada, la guardo
        if (enable && cameraEquipped)
        {
            cameraEquipped = false;
            PutInSocket(cameraItem.transform, holsterSocket, true);
        }
    }

    void PutInSocket(Transform t, Transform socket, bool freeze)
    {
        t.SetParent(socket, worldPositionStays: false);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        if (freezePhysicsWhenEquipped && freeze)
        {
            if (camRb)
            {
                camRb.isKinematic = true;
                camRb.linearVelocity = Vector3.zero;
                camRb.angularVelocity = Vector3.zero;
            }
            if (camCols != null)
                foreach (var c in camCols) c.isTrigger = true; // evita empujones
        }
        else
        {
            if (camRb) camRb.isKinematic = false;
            if (camCols != null)
                foreach (var c in camCols) c.isTrigger = false;
        }
    }

    // Si quieres “soltar” la cámara al mundo (no holster)
    public void DropCameraToWorld()
    {
        cameraEquipped = false;
        cameraItem.transform.SetParent(null, true);
        if (camRb)
        {
            camRb.isKinematic = false;
            camRb.AddForce(Camera.main.transform.forward * 1.5f, ForceMode.VelocityChange);
        }
        if (camCols != null)
            foreach (var c in camCols) c.isTrigger = false;
    }
}
