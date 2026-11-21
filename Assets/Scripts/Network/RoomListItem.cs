using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour
{
    public TextMeshProUGUI roomName;
    public TextMeshProUGUI privateMod;
    public TextMeshProUGUI players;
    public Button button;
    private string _roomName;
    private RoomInfo _roomInfo;

    void Start()
    {
        button.onClick.AddListener(OnClick);
    }
    public void Setup(RoomInfo room)
    {
        roomName.text = room.Name;
        privateMod.text = room.IsOpen.ToString();
        players.text = room.PlayerCount.ToString();
        _roomName = room.Name;
        _roomInfo = room;
    }

    public void OnClick()
    {
        if (NetworkMenuManager.IsLocked(_roomInfo))
        {
            NetworkMenuManager.Instance.ShowEnterPasswordPanel();
            RoomJoinHandler.Instance.Setup(_roomInfo);
            return;
        }
        PhotonNetwork.JoinRoom(_roomName);
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);
    }
}
