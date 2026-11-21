using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class NicknameVisualization : MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text nicknameText;
    [SerializeField] private string fallbackName = "Player";

    private PhotonView ownerView;

    private void Awake()
    {
        ownerView = GetComponentInParent<PhotonView>();
        if (nicknameText == null) nicknameText = GetComponentInChildren<TMP_Text>(true);
    }

    private void Start()
    {
        if (ownerView == null || ownerView.IsMine)
        {
            gameObject.SetActive(false);
            return;
        }
        Refresh();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (ownerView != null && targetPlayer == ownerView.Owner) Refresh();
    }

    private void Refresh()
    {
        if (nicknameText == null || ownerView == null) return;
        string nick = ownerView.Owner != null ? ownerView.Owner.NickName : null;
        nicknameText.text = string.IsNullOrEmpty(nick) ? fallbackName : nick;
    }

    private static Camera FindLocalCamera()
    {
        if (PhotonNetwork.LocalPlayer != null && PhotonNetwork.LocalPlayer.TagObject is GameObject local && local != null)
            foreach (var cam in local.GetComponentsInChildren<Camera>())
                if (cam.isActiveAndEnabled) return cam;
        return Camera.main;
    }
}
