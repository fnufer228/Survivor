using System.Text;
using TMPro;
using UnityEngine;

public class BuildSite : MonoBehaviour, IInteractable
{
    public int Id { get; private set; }
    public int RecipeIndex { get; private set; }
    public float Yaw { get; private set; }
    public BuildingRecipe Recipe { get; private set; }
    public int[] Delivered { get; private set; }

    Bounds bounds;
    TMP_Text label;
    float labelHeight;
    float labelDistance;
    readonly StringBuilder text = new StringBuilder();

    public string Prompt => BuildingManager.Instance != null ? BuildingManager.Instance.GetPrompt(this) : string.Empty;
    public bool CanInteract => true;

    public bool IsComplete
    {
        get
        {
            for (int i = 0; i < Delivered.Length; i++)
                if (Remaining(i) > 0) return false;
            return true;
        }
    }

    public void Init(int id, int recipeIndex, float yaw, BuildingRecipe recipe, int[] delivered,
        Bounds localBounds, TMP_Text labelPrefab, float height, float distance)
    {
        Id = id;
        RecipeIndex = recipeIndex;
        Yaw = yaw;
        Recipe = recipe;
        Delivered = delivered;
        bounds = localBounds;
        labelHeight = height;
        labelDistance = distance;

        if (labelPrefab != null)
        {
            label = Instantiate(labelPrefab);
            label.name = $"Label_{name}";
        }

        RefreshLabel();
    }

    public int Required(int index) => Recipe.costs[index].amount;

    public int Remaining(int index) => Mathf.Max(0, Required(index) - Delivered[index]);

    public Vector3 Center => transform.position + transform.rotation * bounds.center;

    public void SetDelivered(int index, int value)
    {
        if (index < 0 || index >= Delivered.Length) return;
        Delivered[index] = value;
        RefreshLabel();
    }

    public void Interact()
    {
        if (BuildingManager.Instance != null) BuildingManager.Instance.TryDeliver(this);
    }

    void RefreshLabel()
    {
        if (label == null) return;

        text.Clear();
        text.AppendLine(Recipe.displayName);
        for (int i = 0; i < Delivered.Length; i++)
        {
            RecipeCost cost = Recipe.costs[i];
            if (cost == null || cost.resource == null) continue;
            text.Append(cost.resource.displayName).Append(' ')
                .Append(Delivered[i]).Append('/').Append(cost.amount).AppendLine();
        }
        label.text = text.ToString();
    }

    void LateUpdate()
    {
        if (label == null) return;

        Camera cam = BuildPlacer.Local != null ? BuildPlacer.Local.Camera : Camera.main;
        if (cam == null) return;

        Vector3 top = transform.position + transform.rotation *
            new Vector3(bounds.center.x, bounds.max.y + labelHeight, bounds.center.z);

        bool visible = (cam.transform.position - top).sqrMagnitude <= labelDistance * labelDistance;
        if (label.gameObject.activeSelf != visible) label.gameObject.SetActive(visible);
        if (!visible) return;

        label.transform.SetPositionAndRotation(top,
            Quaternion.LookRotation(top - cam.transform.position));
    }

    void OnDestroy()
    {
        if (label != null) Destroy(label.gameObject);
    }
}
