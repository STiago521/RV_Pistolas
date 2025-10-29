using UnityEngine;

public class CameraEquipManager : MonoBehaviour
{
    public Transform player;              // referencia para medir distancia (cabeza/pecho del jugador)
    public GameObject cameraItem;         // la cámara física del juego (empieza en el suelo)
    public Transform handSocket;          // punto en la mano
    public Transform holsterSocket;       // punto de guardado
    public GameObject handsRoot;          // modelos/rig de manos
    public GameObject otherModeObject;    // objeto que se activa en modo alterno

    [Header("Controles")]
    public KeyCode pickupKey = KeyCode.E;  // recoger del suelo
    public KeyCode toggleCameraKey = KeyCode.C;  // mano <-> holster
    public KeyCode dropKey = KeyCode.G;  // soltar al suelo
    public KeyCode toggleModeKey = KeyCode.V;  // modo alterno

    [Header("Parámetros")]
    public float pickupRadius = 2.0f;           // distancia máxima para recoger
    public bool hideCameraItemInOtherMode = true;
    public bool freezePhysicsWhenSocketed = true;

    enum State { OnGround, InHand, InHolster }
    State state = State.OnGround;
    bool otherMode;

    Rigidbody camRb;
    Collider[] camCols;

    void Awake()
    {
        if (!player || !cameraItem || !handSocket || !holsterSocket)
        {
            Debug.LogError("Asigna player, cameraItem, handSocket y holsterSocket.");
            enabled = false; return;
        }
        camRb = cameraItem.GetComponent<Rigidbody>();
        camCols = cameraItem.GetComponentsInChildren<Collider>(true);

        // Asegura estado inicial: en el suelo
        SetPhysicsFree(true);
        if (handsRoot) handsRoot.SetActive(true);
        if (otherModeObject) otherModeObject.SetActive(false);
    }

    void Update()
    {
        // 1) Recoger del suelo
        if (state == State.OnGround && Input.GetKeyDown(pickupKey) && CanPickup())
            EquipToHand();

        // 2) Alternar mano <-> holster
        if (!otherMode && Input.GetKeyDown(toggleCameraKey))
        {
            if (state == State.InHand) PutInHolster();
            else if (state == State.InHolster) EquipToHand();
            // si está en el suelo, primero recoge (E)
        }

        // 3) Soltar al suelo
        if (!otherMode && Input.GetKeyDown(dropKey))
            DropToGround();

        // 4) Modo alterno (apaga manos y cámara, enciende otro objeto)
        if (Input.GetKeyDown(toggleModeKey))
            SetOtherMode(!otherMode);
    }

    bool CanPickup()
    {
        float d = Vector3.Distance(player.position, cameraItem.transform.position);
        return d <= pickupRadius;
    }

    void EquipToHand()
    {
        StickToSocket(handSocket);
        state = State.InHand;
    }

    void PutInHolster()
    {
        StickToSocket(holsterSocket);
        state = State.InHolster;
    }

    void DropToGround()
    {
        // suelta con física delante del jugador
        cameraItem.transform.SetParent(null, true);
        SetPhysicsFree(true);
        if (camRb)
        {
            camRb.AddForce(Camera.main.transform.forward * 1.5f, ForceMode.VelocityChange);
            camRb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
        }
        state = State.OnGround;
    }

    void StickToSocket(Transform socket)
    {
        cameraItem.transform.SetParent(socket, false);
        cameraItem.transform.localPosition = Vector3.zero;
        cameraItem.transform.localRotation = Quaternion.identity;
        SetPhysicsFree(false); // congelar física al “anclar”
    }

    void SetPhysicsFree(bool free)
    {
        if (camRb) camRb.isKinematic = !free;
        if (camCols != null) foreach (var c in camCols) c.isTrigger = !free;
        if (camRb && !free)
        {
            camRb.linearVelocity = Vector3.zero;
            camRb.angularVelocity = Vector3.zero;
        }
    }

    void SetOtherMode(bool enable)
    {
        otherMode = enable;

        // apagar/encender manos
        if (handsRoot) handsRoot.SetActive(!enable);

        // cámara visible/invisible
        if (hideCameraItemInOtherMode && cameraItem)
            cameraItem.SetActive(!enable);

        // objeto alterno
        if (otherModeObject) otherModeObject.SetActive(enable);

        // si entro en modo alterno con la cámara en mano, la guardo
        if (enable && state == State.InHand)
            PutInHolster();

        // al salir, no hacemos nada extra; queda donde estaba (holster o suelo)
    }
}
