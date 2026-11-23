using Photon.Pun;
using UnityEngine;

public class BuildPlacer : MonoBehaviourPun
{
    public static BuildPlacer Local { get; private set; }
    public static bool IsPlacing => Local != null && Local.ghost != null;
    public static bool BlocksMouse => IsPlacing || RecipeBookUI.IsOpen || Time.frameCount == mouseUsedFrame;
    static int mouseUsedFrame = -1;
    public Camera Camera => cam;

    [SerializeField] Camera cam;
    [SerializeField] float maxDistance = 8f;
    [SerializeField] LayerMask groundMask;

    [Header("Ghost look")]
    [Tooltip("Transparent URP material (Surface Type = Transparent)")]
    [SerializeField] Material ghostMaterial;
    [SerializeField] Color validColor = new Color(0.4f, 1f, 0.5f, 0.45f);
    [SerializeField] Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.45f);

    [Header("Placement check")]
    [Tooltip("Everything a building must not overlap: trees, stones, buildings, blueprints, players")]
    [SerializeField] LayerMask blockingMask;
    [SerializeField] float maxSlope = 30f;
    [Tooltip("Max height difference of the ground under the corners")]
    [SerializeField] float maxHeightDifference = 0.8f;
    [Tooltip("Shrinks the overlap box so buildings can stand close to each other")]
    [Range(0.5f, 1f)] [SerializeField] float boxShrink = 0.9f;
    [Tooltip("Bottom part of the box ignored by the overlap check, so grass and small bumps do not block")]
    [SerializeField] float groundClearance = 0.2f;

    [Header("Cancel blueprint")]
    [SerializeField] LayerMask blueprintMask;
    [SerializeField] float cancelReach = 4f;
    [SerializeField] KeyCode cancelKey = KeyCode.C;
    [SerializeField] float cancelHoldTime = 1f;

    [Header("Keys")]
    [SerializeField] KeyCode rotateKey = KeyCode.R;

    public event System.Action<BuildingRecipe, Vector3, Quaternion> PlaceRequested;
    public event System.Action<float> CancelProgressChanged;

    BuildingRecipe recipe;
    BuildGhost ghost;
    float yaw;
    bool hasGround;
    Vector3 groundNormal;
    bool isValid;

    float cancelTimer;
    bool waitCancelRelease;

    void Start()
    {
        if (photonView.IsMine) Local = this;
    }

    void OnDestroy()
    {
        Cancel();
        if (Local == this) Local = null;
    }

    public void StartPlacing(BuildingRecipe newRecipe)
    {
        Cancel();

        if (newRecipe == null || (newRecipe.buildingPrefab == null && newRecipe.ghostPrefab == null))
        {
            Debug.LogError($"BuildPlacer: recipe {(newRecipe != null ? newRecipe.name : "null")} has no prefab", newRecipe);
            return;
        }

        recipe = newRecipe;
        ghost = BuildGhost.Create(recipe, ghostMaterial);
        yaw = cam.transform.eulerAngles.y;
        UpdatePose();
        UpdateValidity();
    }

    public void Cancel()
    {
        if (ghost != null) Destroy(ghost.gameObject);
        ghost = null;
        recipe = null;
    }

    void Update()
    {
        if (!photonView.IsMine || RecipeBookUI.IsOpen) return;

        if (ghost == null)
        {
            UpdateBlueprintCancel();
            return;
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            mouseUsedFrame = Time.frameCount;
            Cancel();
            return;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f) yaw += Mathf.Sign(scroll) * recipe.rotationStep;
        if (Input.GetKeyDown(rotateKey)) yaw += recipe.rotationStep;

        UpdatePose();
        UpdateValidity();

        if (Input.GetMouseButtonDown(0))
        {
            mouseUsedFrame = Time.frameCount;
            if (isValid) Place();
        }
    }

    void Place()
    {
        Transform t = ghost.transform;
        BuildingRecipe placed = recipe;
        Vector3 position = t.position;
        Quaternion rotation = t.rotation;

        if (BuildingManager.Instance == null)
        {
            Debug.LogError("BuildPlacer: no BuildingManager in the scene", this);
            return;
        }

        BuildingManager.Instance.RequestPlace(placed, position, yaw);
        PlaceRequested?.Invoke(placed, position, rotation);
        Cancel();
    }

    void UpdatePose()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Vector3 point;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            hasGround = true;
            groundNormal = hit.normal;
        }
        else
        {
            Vector3 far = ray.GetPoint(maxDistance);
            hasGround = Physics.Raycast(far + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask, QueryTriggerInteraction.Ignore);
            point = hasGround ? hit.point : far;
            groundNormal = hasGround ? hit.normal : Vector3.up;
        }

        point.y += recipe.groundOffset;
        ghost.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
    }

    void UpdateValidity()
    {
        isValid = CheckPlacement();
        ghost.SetColor(isValid ? validColor : invalidColor);
    }

    bool CheckPlacement()
    {
        if (!hasGround) return false;
        if (Vector3.Angle(groundNormal, Vector3.up) > maxSlope) return false;

        Transform t = ghost.transform;
        Bounds b = ghost.LocalBounds;
        Quaternion rotation = t.rotation;
        Vector3 position = t.position;
        Vector3 ext = b.extents;

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 corner = position + rotation * new Vector3(b.center.x + sx * ext.x, 0f, b.center.z + sz * ext.z);
                if (!Physics.Raycast(corner + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundMask, QueryTriggerInteraction.Ignore))
                    return false;

                minY = Mathf.Min(minY, hit.point.y);
                maxY = Mathf.Max(maxY, hit.point.y);
            }
        }

        if (maxY - minY > maxHeightDifference) return false;

        float bottom = b.min.y + groundClearance;
        float height = Mathf.Max(0.02f, b.max.y - bottom);
        Vector3 localCenter = new Vector3(b.center.x, bottom + height * 0.5f, b.center.z);
        Vector3 half = new Vector3(ext.x * boxShrink, height * 0.5f, ext.z * boxShrink);

        return !Physics.CheckBox(position + rotation * localCenter, half, rotation, blockingMask, QueryTriggerInteraction.Collide);
    }

    void UpdateBlueprintCancel()
    {
        if (!Input.GetKey(cancelKey))
        {
            waitCancelRelease = false;
            ResetCancel();
            return;
        }

        if (waitCancelRelease) return;

        BuildSite site = null;
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, cancelReach, blueprintMask, QueryTriggerInteraction.Collide))
            site = hit.collider.GetComponentInParent<BuildSite>();

        if (site == null)
        {
            ResetCancel();
            return;
        }

        cancelTimer += Time.deltaTime;
        CancelProgressChanged?.Invoke(Mathf.Clamp01(cancelTimer / cancelHoldTime));

        if (cancelTimer >= cancelHoldTime)
        {
            ResetCancel();
            waitCancelRelease = true;
            if (BuildingManager.Instance != null) BuildingManager.Instance.RequestCancel(site);
        }
    }

    void ResetCancel()
    {
        if (cancelTimer <= 0f) return;
        cancelTimer = 0f;
        CancelProgressChanged?.Invoke(0f);
    }
}
