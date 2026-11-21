using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class BuildingManager : MonoBehaviourPunCallbacks
{
    public static BuildingManager Instance { get; private set; }

    [Tooltip("Every recipe that can be built. Same order on all clients")]
    [SerializeField] BuildingRecipe[] recipes;

    [Header("Blueprint")]
    [Tooltip("Transparent URP material")]
    [SerializeField] Material blueprintMaterial;
    [SerializeField] Color blueprintColor = new Color(0.75f, 0.9f, 1f, 0.35f);
    [SerializeField] string blueprintLayer = "Blueprint";

    [Header("Progress label (optional)")]
    [Tooltip("TextMeshPro (3D) prefab, not TextMeshPro - Text (UI)")]
    [SerializeField] TMP_Text labelPrefab;
    [SerializeField] float labelHeight = 0.5f;
    [SerializeField] float labelDistance = 12f;

    [Header("Validation")]
    [SerializeField] float maxReach = 12f;

    [Header("Testing")]
    [Tooltip("Lets players deliver resources carried in hands without carrying them. Turn off when carrying is done")]
    [SerializeField] bool freeHandResources = true;

    struct PlacedBuilding
    {
        public int RecipeIndex;
        public Vector3 Position;
        public float Yaw;
        public GameObject Instance;
    }

    readonly Dictionary<int, BuildSite> sites = new Dictionary<int, BuildSite>();
    readonly Dictionary<int, PlacedBuilding> buildings = new Dictionary<int, PlacedBuilding>();
    readonly StringBuilder prompt = new StringBuilder();

    int nextId;
    bool stateRequested;
    int layer = -1;

    void Awake()
    {
        Instance = this;
        layer = LayerMask.NameToLayer(blueprintLayer);
        if (layer < 0) Debug.LogError($"BuildingManager: layer '{blueprintLayer}' does not exist", this);
    }

    void Start() => RequestStateIfNeeded();

    public override void OnJoinedRoom() => RequestStateIfNeeded();

    void RequestStateIfNeeded()
    {
        if (stateRequested || !PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient) return;
        stateRequested = true;
        photonView.RPC(nameof(RPC_RequestState), RpcTarget.MasterClient);
    }

    bool ValidRecipe(int index) => index >= 0 && index < recipes.Length && recipes[index] != null;

    public void RequestPlace(BuildingRecipe recipe, Vector3 position, float yaw)
    {
        int index = System.Array.IndexOf(recipes, recipe);
        if (index < 0)
        {
            Debug.LogError($"BuildingManager: {recipe.name} is not in Recipes", this);
            return;
        }
        photonView.RPC(nameof(RPC_RequestPlace), RpcTarget.MasterClient, index, position, yaw);
    }

    [PunRPC]
    void RPC_RequestPlace(int recipeIndex, Vector3 position, float yaw, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !ValidRecipe(recipeIndex)) return;
        if (!NetworkPlayer.IsNear(info.Sender, position, maxReach)) return;

        int id = nextId++;
        photonView.RPC(nameof(RPC_SitePlaced), RpcTarget.All, id, recipeIndex, position, yaw);

        if (sites.TryGetValue(id, out BuildSite site) && site.IsComplete)
            photonView.RPC(nameof(RPC_Completed), RpcTarget.All, id);
    }

    [PunRPC]
    void RPC_SitePlaced(int id, int recipeIndex, Vector3 position, float yaw)
    {
        CreateSite(id, recipeIndex, position, yaw, null);
    }

    void CreateSite(int id, int recipeIndex, Vector3 position, float yaw, int[] delivered)
    {
        if (!ValidRecipe(recipeIndex) || sites.ContainsKey(id) || buildings.ContainsKey(id)) return;
        nextId = Mathf.Max(nextId, id + 1);

        BuildingRecipe recipe = recipes[recipeIndex];
        int costCount = recipe.costs == null ? 0 : recipe.costs.Length;

        BuildGhost ghost = BuildGhost.Create(recipe, blueprintMaterial);
        ghost.SetColor(blueprintColor);
        ghost.name = $"Blueprint_{id}_{recipe.name}";

        Transform t = ghost.transform;
        t.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        t.SetParent(transform, true);
        if (layer >= 0) SetLayer(t, layer);

        Bounds b = ghost.LocalBounds;
        Vector3 scale = t.lossyScale;
        BoxCollider box = ghost.gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Divide(b.center, scale);
        box.size = Divide(b.size, scale);

        BuildSite site = ghost.gameObject.AddComponent<BuildSite>();
        site.Init(id, recipeIndex, yaw, recipe, delivered ?? new int[costCount], b,
            labelPrefab, labelHeight, labelDistance);
        sites[id] = site;
    }

    public string GetPrompt(BuildSite site)
    {
        int index = FindDeliverable(site);
        if (index >= 0)
        {
            RecipeCost cost = site.Recipe.costs[index];
            return $"Add {cost.resource.displayName} ({site.Delivered[index]}/{cost.amount})";
        }

        prompt.Clear().Append("Need: ");
        bool first = true;
        for (int i = 0; i < site.Delivered.Length; i++)
        {
            if (site.Remaining(i) <= 0 || site.Recipe.costs[i].resource == null) continue;
            if (!first) prompt.Append(", ");
            prompt.Append(site.Recipe.costs[i].resource.displayName).Append(' ').Append(site.Remaining(i));
            first = false;
        }
        return prompt.ToString();
    }

    public int FindDeliverable(BuildSite site)
    {
        for (int i = 0; i < site.Delivered.Length; i++)
        {
            RecipeCost cost = site.Recipe.costs[i];
            if (cost != null && cost.resource != null && site.Remaining(i) > 0 && PlayerHas(cost.resource))
                return i;
        }
        return -1;
    }

    public void TryDeliver(BuildSite site)
    {
        int index = FindDeliverable(site);
        if (index < 0) return;

        BuildResource resource = site.Recipe.costs[index].resource;
        if (!TakeFromPlayer(resource)) return;

        photonView.RPC(nameof(RPC_RequestDeliver), RpcTarget.MasterClient, site.Id, site.RecipeIndex, index);
    }

    [PunRPC]
    void RPC_RequestDeliver(int siteId, int recipeIndex, int costIndex, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        bool accepted = sites.TryGetValue(siteId, out BuildSite site)
            && costIndex >= 0 && costIndex < site.Delivered.Length
            && site.Remaining(costIndex) > 0
            && NetworkPlayer.IsNear(info.Sender, site.Center, maxReach);

        if (!accepted)
        {
            photonView.RPC(nameof(RPC_Refund), info.Sender, recipeIndex, costIndex);
            return;
        }

        photonView.RPC(nameof(RPC_Delivered), RpcTarget.All, siteId, costIndex, site.Delivered[costIndex] + 1);

        if (site.IsComplete)
            photonView.RPC(nameof(RPC_Completed), RpcTarget.All, siteId);
    }

    [PunRPC]
    void RPC_Delivered(int siteId, int costIndex, int value)
    {
        if (sites.TryGetValue(siteId, out BuildSite site))
            site.SetDelivered(costIndex, value);
    }

    [PunRPC]
    void RPC_Refund(int recipeIndex, int costIndex)
    {
        if (!ValidRecipe(recipeIndex)) return;
        RecipeCost[] costs = recipes[recipeIndex].costs;
        if (costs == null || costIndex < 0 || costIndex >= costs.Length || costs[costIndex].resource == null) return;
        GiveToPlayer(costs[costIndex].resource);
    }

    [PunRPC]
    void RPC_Completed(int siteId)
    {
        if (!sites.TryGetValue(siteId, out BuildSite site)) return;
        sites.Remove(siteId);

        int recipeIndex = site.RecipeIndex;
        Vector3 position = site.transform.position;
        float yaw = site.Yaw;
        Destroy(site.gameObject);

        CreateBuilding(siteId, recipeIndex, position, yaw);
    }

    void CreateBuilding(int id, int recipeIndex, Vector3 position, float yaw)
    {
        if (!ValidRecipe(recipeIndex) || buildings.ContainsKey(id)) return;
        nextId = Mathf.Max(nextId, id + 1);

        BuildingRecipe recipe = recipes[recipeIndex];
        if (recipe.buildingPrefab == null)
        {
            Debug.LogError($"BuildingManager: {recipe.name} has no Building Prefab", recipe);
            return;
        }

        GameObject instance = Instantiate(recipe.buildingPrefab, position, Quaternion.Euler(0f, yaw, 0f), transform);
        instance.name = $"Building_{id}_{recipe.name}";

        if (instance.GetComponentInChildren<PhotonView>(true) != null)
            Debug.LogWarning($"BuildingManager: {recipe.buildingPrefab.name} has a PhotonView. Buildings are spawned locally, remove it", recipe.buildingPrefab);

        buildings[id] = new PlacedBuilding
        {
            RecipeIndex = recipeIndex,
            Position = position,
            Yaw = yaw,
            Instance = instance
        };
    }

    public void RequestCancel(BuildSite site)
    {
        if (site == null) return;
        photonView.RPC(nameof(RPC_RequestCancel), RpcTarget.MasterClient, site.Id);
    }

    [PunRPC]
    void RPC_RequestCancel(int siteId, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || !sites.TryGetValue(siteId, out BuildSite site)) return;
        if (!NetworkPlayer.IsNear(info.Sender, site.Center, maxReach)) return;

        if (DropManager.Instance != null)
        {
            for (int i = 0; i < site.Delivered.Length; i++)
            {
                BuildResource resource = site.Recipe.costs[i].resource;
                if (resource == null || resource.dropItem == null) continue;

                for (int n = 0; n < site.Delivered[i]; n++)
                    DropManager.Instance.Spawn(resource.dropItem, site.Center);
            }
        }

        photonView.RPC(nameof(RPC_Cancelled), RpcTarget.All, siteId);
    }

    [PunRPC]
    void RPC_Cancelled(int siteId)
    {
        if (!sites.TryGetValue(siteId, out BuildSite site)) return;
        sites.Remove(siteId);
        Destroy(site.gameObject);
    }

    bool PlayerHas(BuildResource resource)
    {
        if (resource.inventoryItem != null)
            return InventoryManager.Instance != null && InventoryManager.Instance.HasItem(resource.inventoryItem);

        if (resource.carriedInHands)
        {
            if (TryGetCarrier(out IHandCarrier carrier)) return carrier.IsCarrying(resource);
            return freeHandResources;
        }

        return false;
    }

    bool TakeFromPlayer(BuildResource resource)
    {
        if (resource.inventoryItem != null)
            return InventoryManager.Instance != null && InventoryManager.Instance.RemoveItem(resource.inventoryItem);

        if (resource.carriedInHands)
        {
            if (TryGetCarrier(out IHandCarrier carrier)) return carrier.TryTake(resource);
            return freeHandResources;
        }

        return false;
    }

    void GiveToPlayer(BuildResource resource)
    {
        if (resource.inventoryItem != null)
        {
            if (InventoryManager.Instance != null) InventoryManager.Instance.AddItem(resource.inventoryItem);
            return;
        }

        if (resource.carriedInHands && TryGetCarrier(out IHandCarrier carrier))
            carrier.Give(resource);
    }

    static bool TryGetCarrier(out IHandCarrier carrier)
    {
        carrier = null;
        return BuildPlacer.Local != null && BuildPlacer.Local.TryGetComponent(out carrier);
    }

    [PunRPC]
    void RPC_RequestState(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int siteCount = sites.Count;
        var siteIds = new int[siteCount];
        var siteRecipes = new int[siteCount];
        var sitePoses = new float[siteCount * 4];
        var delivered = new List<int>();

        int i = 0;
        foreach (BuildSite site in sites.Values)
        {
            siteIds[i] = site.Id;
            siteRecipes[i] = site.RecipeIndex;
            WritePose(sitePoses, i, site.transform.position, site.Yaw);
            delivered.AddRange(site.Delivered);
            i++;
        }

        int buildingCount = buildings.Count;
        var buildingIds = new int[buildingCount];
        var buildingRecipes = new int[buildingCount];
        var buildingPoses = new float[buildingCount * 4];

        i = 0;
        foreach (var kv in buildings)
        {
            buildingIds[i] = kv.Key;
            buildingRecipes[i] = kv.Value.RecipeIndex;
            WritePose(buildingPoses, i, kv.Value.Position, kv.Value.Yaw);
            i++;
        }

        photonView.RPC(nameof(RPC_State), info.Sender,
            siteIds, siteRecipes, sitePoses, delivered.ToArray(),
            buildingIds, buildingRecipes, buildingPoses);
    }

    [PunRPC]
    void RPC_State(int[] siteIds, int[] siteRecipes, float[] sitePoses, int[] delivered,
        int[] buildingIds, int[] buildingRecipes, float[] buildingPoses)
    {
        for (int i = 0; i < buildingIds.Length; i++)
            CreateBuilding(buildingIds[i], buildingRecipes[i], ReadPosition(buildingPoses, i), buildingPoses[i * 4 + 3]);

        int offset = 0;
        for (int i = 0; i < siteIds.Length; i++)
        {
            if (!ValidRecipe(siteRecipes[i])) continue;

            RecipeCost[] costs = recipes[siteRecipes[i]].costs;
            int count = costs == null ? 0 : costs.Length;
            var slice = new int[count];
            for (int n = 0; n < count && offset + n < delivered.Length; n++)
                slice[n] = delivered[offset + n];
            offset += count;

            CreateSite(siteIds[i], siteRecipes[i], ReadPosition(sitePoses, i), sitePoses[i * 4 + 3], slice);
        }
    }

    static void WritePose(float[] poses, int i, Vector3 position, float yaw)
    {
        poses[i * 4] = position.x;
        poses[i * 4 + 1] = position.y;
        poses[i * 4 + 2] = position.z;
        poses[i * 4 + 3] = yaw;
    }

    static Vector3 ReadPosition(float[] poses, int i) =>
        new Vector3(poses[i * 4], poses[i * 4 + 1], poses[i * 4 + 2]);

    static Vector3 Divide(Vector3 a, Vector3 b) =>
        new Vector3(a.x / Mathf.Max(0.0001f, Mathf.Abs(b.x)),
                    a.y / Mathf.Max(0.0001f, Mathf.Abs(b.y)),
                    a.z / Mathf.Max(0.0001f, Mathf.Abs(b.z)));

    static void SetLayer(Transform t, int value)
    {
        t.gameObject.layer = value;
        foreach (Transform child in t) SetLayer(child, value);
    }
}
