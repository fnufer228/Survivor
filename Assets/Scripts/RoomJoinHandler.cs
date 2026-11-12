using System;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomJoinHandler : MonoBehaviourPunCallbacks
{
    public static RoomJoinHandler Instance { get; private set; }
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField enterPasswordInput;
    private RoomInfo _roomInfo;
    
    private string PwdKey = "pwd";

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        joinButton.onClick.AddListener(() => TryJoin(_roomInfo));
    }

    public void Setup(RoomInfo info)
    {
        _roomInfo = info;
    }
    public void TryJoin(RoomInfo info)
    {
        if (NetworkMenuManager.IsLocked(info))
        {
            if (!info.CustomProperties.TryGetValue(PwdKey, out object pwd) || pwd == null)
            {
                Debug.LogError("Password not found");
            }
            if ((string)pwd != enterPasswordInput.text)
            {
                Debug.Log("WrongPassword");
                Debug.Log((string)pwd);
                Debug.Log(enterPasswordInput.text);
                return;
            }
        }
        PhotonNetwork.JoinRoom(info.Name);
    }
}
