using Photon.Pun;
using UnityEngine;

public class LogCarrier : MonoBehaviourPun, IHandCarrier, IPunObservable
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PickupItemType logItem;
    [SerializeField] private BuildResource logResource;
    [SerializeField] private WeaponSlots slots;
    [Tooltip("Log models in the hands, one per carried log")]
    [SerializeField] private GameObject[] logVisuals;
    [SerializeField] [Range(1, 4)] private int maxLogs = 2;
    [SerializeField] private float reach = 3f;
    [SerializeField] private LayerMask pickupMask = ~0;
    [SerializeField] private KeyCode pickupKey = KeyCode.E;
    [SerializeField] private KeyCode dropKey = KeyCode.G;
    [SerializeField] private float dropDistance = 1.5f;
    [SerializeField] private LayerMask groundMask = ~0;

    private int carried;
    private int pending;
    private float pendingUntil;
    private DroppedItem aimed;

    public int Carried => carried;
    public bool IsFull => carried + pending >= maxLogs;

    private void OnEnable()
    {
        DropManager.LocalPicked += OnPicked;
    }

    private void OnDisable()
    {
        DropManager.LocalPicked -= OnPicked;
        SetAimed(null);
    }

    private void Start()
    {
        Refresh();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (pending > 0 && Time.time > pendingUntil) pending = 0;

        UpdateAim();

        if (Input.GetKeyDown(pickupKey) && aimed != null && !IsFull && DropManager.Instance != null)
        {
            pending++;
            pendingUntil = Time.time + 2f;
            DropManager.Instance.Pickup(aimed);
            SetAimed(null);
        }

        if (Input.GetKeyDown(dropKey) && carried > 0)
            DropOne();
    }

    private void UpdateAim()
    {
        DroppedItem target = null;
        if (playerCamera != null && !IsFull)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, reach, pickupMask, QueryTriggerInteraction.Ignore))
            {
                DroppedItem item = hit.collider.GetComponentInParent<DroppedItem>();
                if (item != null && item.Type == logItem) target = item;
            }
        }
        SetAimed(target);
    }

    private void SetAimed(DroppedItem target)
    {
        if (target == aimed) return;
        if (aimed != null) InteractOutline.Hide(aimed);
        aimed = target;
        if (aimed != null) InteractOutline.Show(aimed);
    }

    private void OnPicked(PickupItemType type, int amount)
    {
        if (logItem == null || type != logItem) return;
        pending = Mathf.Max(0, pending - amount);
        SetCarried(carried + amount);
    }

    private void DropOne()
    {
        if (DropManager.Instance == null || logItem == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(playerCamera.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = transform.forward;
        Vector3 point = transform.position + forward * dropDistance;
        if (Physics.Raycast(point + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
            point = hit.point;

        DropManager.Instance.RequestDrop(logItem, point);
        SetCarried(carried - 1);
    }

    private void SetCarried(int value)
    {
        int before = carried;
        carried = Mathf.Clamp(value, 0, maxLogs);
        if (before == carried) return;
        Refresh();
    }

    private void Refresh()
    {
        if (logVisuals != null)
            for (int i = 0; i < logVisuals.Length; i++)
                if (logVisuals[i] != null) logVisuals[i].SetActive(i < carried);

        KeepHandsEmpty();
    }

    private void LateUpdate()
    {
        KeepHandsEmpty();
    }

    private void KeepHandsEmpty()
    {
        if (slots != null && carried > 0 && slots.CurrentSlot != 0) slots.Select(0);
    }

    public bool IsCarrying(BuildResource resource)
    {
        return resource != null && resource == logResource && carried > 0;
    }

    public bool TryTake(BuildResource resource)
    {
        if (!IsCarrying(resource)) return false;
        SetCarried(carried - 1);
        return true;
    }

    public void Give(BuildResource resource)
    {
        if (resource == logResource) SetCarried(carried + 1);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((byte)carried);
            return;
        }

        int value = (byte)stream.ReceiveNext();
        if (value != carried)
        {
            carried = value;
            Refresh();
        }
    }
}
