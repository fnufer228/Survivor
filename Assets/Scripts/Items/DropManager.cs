using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class DropManager : MonoBehaviourPunCallbacks
{
    public static DropManager Instance { get; private set; }

    [Tooltip("Every Pickup Item that can be dropped. Same order on all clients")]
    [SerializeField] PickupItemType[] itemTypes;
    [SerializeField] float scatterRadius = 1.5f;
    [SerializeField] LayerMask groundMask;

    [Header("Validation")]
    [SerializeField] float maxReachDistance = 5f;

    [Header("Lifetime")]
    [Tooltip("Seconds before a drop disappears. 0 = never")]
    [SerializeField] float lifetimeSeconds = 600f;
    [SerializeField] float checkInterval = 2f;

    readonly Dictionary<int, DroppedItem> drops = new Dictionary<int, DroppedItem>();
    readonly List<int> expired = new List<int>();

    int nextId;
    float nextCheck;
    bool stateRequested;

    void Awake() => Instance = this;

    void Start() => RequestStateIfNeeded();

    public override void OnJoinedRoom() => RequestStateIfNeeded();

    void RequestStateIfNeeded()
    {
        if (stateRequested || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
        stateRequested = true;
        photonView.RPC(nameof(RPC_RequestState), RpcTarget.MasterClient);
    }

    public void SpawnDrops(DropEntry entry, Vector3 center)
    {
        if (!PhotonNetwork.IsMasterClient || entry == null || entry.item == null) return;

        int min = Mathf.Max(0, entry.min);
        int amount = Random.Range(min, Mathf.Max(min, entry.max) + 1);
        for (int i = 0; i < amount; i++) Spawn(entry.item, center);
    }

    public void Spawn(PickupItemType type, Vector3 center)
    {
        if (!PhotonNetwork.IsMasterClient || type == null) return;

        int index = System.Array.IndexOf(itemTypes, type);
        if (index < 0)
        {
            Debug.LogError($"DropManager: {type.name} is not in Item Types", this);
            return;
        }

        Vector2 offset = Random.insideUnitCircle * scatterRadius;
        Vector3 pos = center + new Vector3(offset.x, 0f, offset.y);
        if (Physics.Raycast(pos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f, groundMask))
            pos = hit.point;

        float rotY = Random.Range(0f, 360f);
        int id = nextId++;

        photonView.RPC(nameof(RPC_Spawn), RpcTarget.All, id, index, pos, rotY, PhotonNetwork.ServerTimestamp);
    }

    public void Pickup(DroppedItem item)
    {
        if (item == null) return;
        photonView.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, item.Id);
    }

    [PunRPC]
    void RPC_Spawn(int id, int typeIndex, Vector3 pos, float rotY, int spawnTime)
    {
        SpawnLocal(id, typeIndex, pos, rotY, spawnTime);
    }

    void SpawnLocal(int id, int typeIndex, Vector3 pos, float rotY, int spawnTime)
    {
        if (drops.ContainsKey(id) || typeIndex < 0 || typeIndex >= itemTypes.Length) return;

        nextId = Mathf.Max(nextId, id + 1);

        PickupItemType type = itemTypes[typeIndex];
        if (type == null || type.prefab == null)
        {
            Debug.LogError($"DropManager: item type at index {typeIndex} has no prefab", this);
            return;
        }

        GameObject go = Instantiate(type.prefab, pos, Quaternion.Euler(0f, rotY, 0f), transform);
        if (!go.TryGetComponent(out DroppedItem item))
            item = go.AddComponent<DroppedItem>();

        item.Init(id, type, typeIndex, spawnTime);
        drops[id] = item;
    }

    [PunRPC]
    void RPC_RequestPickup(int id, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !drops.TryGetValue(id, out DroppedItem item)) return;
        if (!NetworkPlayer.IsNear(info.Sender, item.transform.position, maxReachDistance)) return;

        photonView.RPC(nameof(RPC_Picked), RpcTarget.All, id, info.Sender.ActorNumber);
    }

    [PunRPC]
    void RPC_Picked(int id, int actor)
    {
        if (!drops.TryGetValue(id, out DroppedItem item)) return;

        PickupItemType type = item.Type;
        RemoveLocal(id);

        if (actor == PhotonNetwork.LocalPlayer.ActorNumber && type != null)
            Loot.GiveLocal(type.ResolveInventoryItem(), type.amount);
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || lifetimeSeconds <= 0f || Time.time < nextCheck) return;
        nextCheck = Time.time + checkInterval;

        int now = PhotonNetwork.ServerTimestamp;
        expired.Clear();

        foreach (var kv in drops)
        {
            if (unchecked(now - kv.Value.SpawnTime) >= lifetimeSeconds * 1000f)
                expired.Add(kv.Key);
        }

        if (expired.Count > 0)
            photonView.RPC(nameof(RPC_Despawn), RpcTarget.All, expired.ToArray());
    }

    [PunRPC]
    void RPC_Despawn(int[] ids)
    {
        foreach (int id in ids) RemoveLocal(id);
    }

    void RemoveLocal(int id)
    {
        if (!drops.TryGetValue(id, out DroppedItem item)) return;
        drops.Remove(id);
        if (item != null) Destroy(item.gameObject);
    }

    [PunRPC]
    void RPC_RequestState(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int n = drops.Count;
        var ids = new int[n];
        var typeIndices = new int[n];
        var transforms = new float[n * 4];
        var times = new int[n];

        int i = 0;
        foreach (DroppedItem item in drops.Values)
        {
            Vector3 p = item.transform.position;
            ids[i] = item.Id;
            typeIndices[i] = item.TypeIndex;
            times[i] = item.SpawnTime;
            transforms[i * 4] = p.x;
            transforms[i * 4 + 1] = p.y;
            transforms[i * 4 + 2] = p.z;
            transforms[i * 4 + 3] = item.transform.eulerAngles.y;
            i++;
        }

        photonView.RPC(nameof(RPC_State), info.Sender, ids, typeIndices, transforms, times);
    }

    [PunRPC]
    void RPC_State(int[] ids, int[] typeIndices, float[] transforms, int[] times)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            Vector3 pos = new Vector3(transforms[i * 4], transforms[i * 4 + 1], transforms[i * 4 + 2]);
            SpawnLocal(ids[i], typeIndices[i], pos, transforms[i * 4 + 3], times[i]);
        }
    }
}
