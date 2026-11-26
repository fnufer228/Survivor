using Photon.Pun;
using UnityEngine;

public class PlayerHandsSync : MonoBehaviourPun, IPunObservable
{
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private GameObject handsRoot;
    [SerializeField] private WeaponSlots slots;
    [Tooltip("Layer the hands are drawn on for other players, so their main camera sees them")]
    [SerializeField] private string remoteLayer = "Default";
    [SerializeField] private float pitchSmoothing = 15f;

    private float targetPitch;
    private int targetSlot;

    private void Start()
    {
        if (photonView.IsMine) return;

        if (handsRoot != null)
        {
            handsRoot.SetActive(true);
            int layer = LayerMask.NameToLayer(remoteLayer);
            if (layer >= 0)
                foreach (Transform t in handsRoot.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = layer;
        }

        if (slots != null)
        {
            slots.InputEnabled = false;
            slots.Select(targetSlot);
        }
    }

    private void Update()
    {
        if (photonView.IsMine || cameraPivot == null) return;

        float current = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
        float pitch = Mathf.Lerp(current, targetPitch, 1f - Mathf.Exp(-pitchSmoothing * Time.deltaTime));
        cameraPivot.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(cameraPivot != null ? Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x) : 0f);
            stream.SendNext((byte)(slots != null ? slots.CurrentSlot : 0));
            return;
        }

        targetPitch = (float)stream.ReceiveNext();
        int slot = (byte)stream.ReceiveNext();
        if (slot == targetSlot) return;

        targetSlot = slot;
        if (slots != null) slots.Select(slot);
    }
}
