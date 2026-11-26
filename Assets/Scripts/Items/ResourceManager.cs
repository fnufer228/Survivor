using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.Serialization;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(PhotonView))]
public class ResourceManager : MonoBehaviourPunCallbacks
{
    public const string SeedKey = "seed";
    public static ResourceManager Instance { get; private set; }
    

    [Header("Generation")]
    [SerializeField] ResourceType[] types;
    [SerializeField] int count = 3000;
    [Tooltip("Size of the spawn area around this object")]
    [SerializeField] Vector2 areaSize = new Vector2(500f, 500f);
    [SerializeField] LayerMask groundMask;
    [SerializeField] float maxSlope = 35f;

    [FormerlySerializedAs("maxHitDistance")]
    [Header("Validation")]
    [Tooltip("Max distance from player to resource when hitting (with lag tolerance)")]
    [SerializeField] float maxReachDistance = 6f;

    [Header("Respawn")]
    [SerializeField] float respawnCheckInterval = 1f;
    [Tooltip("Do not respawn if a player is within this radius")]
    [SerializeField] float playerBlockRadius = 1.5f;
    [SerializeField] LayerMask playerMask;

    public event System.Action<ResourceType> LocalHarvested;

    readonly List<ResourceNode> nodes = new List<ResourceNode>();
    readonly Dictionary<int, int> removedAt = new Dictionary<int, int>();
    readonly Dictionary<int, int> damage = new Dictionary<int, int>();
    readonly List<int> toRespawn = new List<int>();

    bool generated;
    float nextCheck;

    void Awake() => Instance = this;

    void Start()
    {
        TryBegin();
    }

    public override void OnJoinedRoom()
    {
        TryBegin();
    }

    private void TryBegin()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("ResourceManager: scene started outside of a Photon room");
            return;
        }

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.TryGetValue(SeedKey, out object seed))
        {
            Begin((int)seed);
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            int newSeed = Random.Range(int.MinValue, int.MaxValue);
            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { { SeedKey, newSeed } });
            Begin(newSeed);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable changed)
    {
        if (!generated && changed.TryGetValue(SeedKey, out object seed))
            Begin((int)seed);
    }

    void Begin(int seed)
    {
        if (generated) return;
        Generate(seed);
        generated = true;

        if (!PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_RequestState), RpcTarget.MasterClient);
    }

    void Generate(int seed)
    {
        var rng = new System.Random(seed);

        float totalWeight = 0f;
        foreach (var t in types)
        {
            if (t == null || t.prefab == null)
            {
                Debug.LogError("ResourceManager: Types has an empty entry or a type without prefab", this);
                return;
            }
            totalWeight += Mathf.Max(0f, t.spawnWeight);
        }

        for (int i = 0; i < count; i++)
        {
            float x = (float)rng.NextDouble();
            float z = (float)rng.NextDouble();
            float pick = (float)rng.NextDouble() * totalWeight;
            float rotY = (float)rng.NextDouble() * 360f;
            float scale = 0.85f + (float)rng.NextDouble() * 0.3f;

            nodes.Add(null);

            Vector3 origin = transform.position +
                new Vector3((x - 0.5f) * areaSize.x, 1000f, (z - 0.5f) * areaSize.y);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2000f, groundMask)) continue;
            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) continue;

            ResourceType type = PickType(pick);
            GameObject go = Instantiate(type.prefab, hit.point, Quaternion.Euler(0f, rotY, 0f), transform);
            go.transform.localScale *= scale;

            if (!go.TryGetComponent(out ResourceNode node))
                node = go.AddComponent<ResourceNode>();

            node.Init(i, type);
            nodes[i] = node;
        }
    }

    ResourceType PickType(float pick)
    {
        foreach (var t in types)
        {
            if (pick < t.spawnWeight) return t;
            pick -= t.spawnWeight;
        }
        return types[types.Length - 1];
    }

    bool IsValid(int id) => id >= 0 && id < nodes.Count && nodes[id] != null;

    public void Hit(ResourceNode node, int dmg = 1)
    {
        if (!generated || node == null) return;
        photonView.RPC(nameof(RPC_RequestHit), RpcTarget.MasterClient, node.Id, dmg);
    }

    [PunRPC]
    void RPC_Hit(int id)
    {
        if (IsValid(id)) nodes[id].PlayHit();
    }

    [PunRPC]
    void RPC_Removed(int id, int serverTime, int actor)
    {
        if (!IsValid(id)) return;

        removedAt[id] = serverTime;
        ResourceNode node = nodes[id];
        node.Hide();

        if (actor != PhotonNetwork.LocalPlayer.ActorNumber) return;

        if (node.Type.harvestMode == HarvestMode.Pickup)
            Loot.GiveLocal(node.Type, 1);

        LocalHarvested?.Invoke(node.Type);
    }

    void Update()
    {
        if (!generated || !PhotonNetwork.IsMasterClient || Time.time < nextCheck) return;
        nextCheck = Time.time + respawnCheckInterval;

        int now = PhotonNetwork.ServerTimestamp;
        toRespawn.Clear();

        foreach (var kv in removedAt)
        {
            ResourceNode node = nodes[kv.Key];
            float seconds = node.Type.respawnSeconds;
            if (seconds <= 0f) continue;

            int elapsedMs = unchecked(now - kv.Value);
            if (elapsedMs < seconds * 1000f) continue;

            if (Physics.CheckSphere(node.transform.position, playerBlockRadius, playerMask)) continue;

            toRespawn.Add(kv.Key);
        }

        if (toRespawn.Count > 0)
            photonView.RPC(nameof(RPC_Respawn), RpcTarget.All, toRespawn.ToArray());
    }

    [PunRPC]
    void RPC_Respawn(int[] ids)
    {
        foreach (int id in ids)
        {
            if (!IsValid(id)) continue;
            removedAt.Remove(id);
            damage.Remove(id);
            nodes[id].PlayRespawn();
        }
    }

    [PunRPC]
    void RPC_RequestState(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var ids = new int[removedAt.Count];
        var times = new int[removedAt.Count];
        int i = 0;
        foreach (var kv in removedAt)
        {
            ids[i] = kv.Key;
            times[i] = kv.Value;
            i++;
        }
        photonView.RPC(nameof(RPC_State), info.Sender, ids, times);
    }

    [PunRPC]
    void RPC_State(int[] ids, int[] times)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (!IsValid(ids[i])) continue;
            removedAt[ids[i]] = times[i];
            nodes[ids[i]].Hide();
        }
    }
    public void Pickup(ResourceNode node)
    {
        if (!generated || node == null) return;
        photonView.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, node.Id);
    }
    [PunRPC]
    void RPC_RequestHit(int id, int dmg, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !CanAct(id, HarvestMode.Hit, info.Sender)) return;
 
        ResourceNode node = nodes[id];
        damage.TryGetValue(id, out int total);
        total += Mathf.Clamp(dmg, 1, 100);
 
        if (total < node.Type.maxHealth)
        {
            damage[id] = total;
            photonView.RPC(nameof(RPC_Hit), RpcTarget.All, id);
            return;
        }
 
        damage.Remove(id);
        Remove(node, info.Sender);
    }
    [PunRPC]
    void RPC_RequestPickup(int id, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !CanAct(id, HarvestMode.Pickup, info.Sender)) return;
        Remove(nodes[id], info.Sender);
    }
 
    void Remove(ResourceNode node, Player sender)
    {
        photonView.RPC(nameof(RPC_Removed), RpcTarget.All,
            node.Id, PhotonNetwork.ServerTimestamp, sender.ActorNumber);
        SpawnWorldDrops(node);
    }
 
    void SpawnWorldDrops(ResourceNode node)
    {
        if (node.Type.drop == null || node.Type.drop.item == null || node.Type.harvestMode == HarvestMode.Pickup) return;
        if (DropManager.Instance != null)

            DropManager.Instance.SpawnDrops(node.Type.drop, node.transform.position);
        if (node.Type.extraDrops == null) return;
        foreach (RandomDrop extra in node.Type.extraDrops)
            DropManager.Instance.SpawnRandom(extra, node.transform.position);
    }
    bool CanAct(int id, HarvestMode mode, Player sender) =>
        IsValid(id) &&
        !removedAt.ContainsKey(id) &&
        nodes[id].Type.harvestMode == mode &&
        NetworkPlayer.IsNear(sender, nodes[id].transform.position, maxReachDistance);
}
