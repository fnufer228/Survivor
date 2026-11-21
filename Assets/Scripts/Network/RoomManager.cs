using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    [Header("List of Room")]
    public Transform container;
    public RoomListItem itemPrefab;
    private Dictionary<string, RoomInfo> rooms = new Dictionary<string, RoomInfo>();
    private List<RoomListItem> items = new List<RoomListItem>();

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined lobby");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"OnRoomListUpdate {roomList.Count}");
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList)
            {
                rooms.Remove(room.Name);
            }
            else
            {
                rooms[room.Name] = room;
            }
        }

        RedrawUI();
    }

    public override void OnLeftRoom()
    {
        rooms.Clear();
        PhotonNetwork.JoinLobby();
    }

    private void RedrawUI()
    {
        foreach (var item in items) Destroy(item.gameObject);
        items.Clear();

        foreach (RoomInfo info in rooms.Values)
        {
            RoomListItem item = Instantiate(itemPrefab, container);
            item.Setup(info);
            items.Add(item);
        }
        Debug.Log("Room Updated");
    }
}
