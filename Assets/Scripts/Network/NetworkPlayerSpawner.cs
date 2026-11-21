using Photon.Pun;
using UnityEngine;

public class NetworkPlayerSpawner : MonoBehaviourPunCallbacks
{
    private const string playerPrefabName = "Player";
    public Transform spawnPoint;
    public float spawnSpread;
    private bool spawned;

    void Start()
    {
        TrySpawn();
    }

    public override void OnJoinedRoom()
    {
        TrySpawn();
    }

    private void TrySpawn()
    {
        if (spawned || !PhotonNetwork.InRoom) return;
        spawned = true;

        int index = PhotonNetwork.LocalPlayer.ActorNumber;
        Vector3 offset = new Vector3((index % 4) * spawnSpread, 0f, (index / 4) * spawnSpread);

        PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position + offset, spawnPoint.rotation);
    }
}
