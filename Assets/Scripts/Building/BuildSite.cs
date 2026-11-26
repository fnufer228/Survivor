using TMPro;
using UnityEngine;

public class BuildSite : MonoBehaviour, IInteractable
{
    public int Id { get; private set; }
    public int RecipeIndex { get; private set; }
    public float Yaw { get; private set; }
    public BuildingRecipe Recipe { get; private set; }
    public int[] Delivered { get; private set; }

    const float IconSize = 0.45f;
    const float EntryGap = 0.12f;

    Bounds bounds;
    BuildGhost ghost;
    Transform label;
    GameObject[] entries;
    TMP_Text[] counts;
    float[] entryWidths;
    float labelHeight;
    float labelDistance;

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
        ghost = GetComponent<BuildGhost>();

        for (int i = 0; i < Delivered.Length; i++)
            RefreshParts(i);

        BuildLabel(labelPrefab);
        RefreshLabel();
    }

    public int Required(int index) => Recipe.costs[index].amount;

    public int Remaining(int index) => Mathf.Max(0, Required(index) - Delivered[index]);

    public Vector3 Center => transform.position + transform.rotation * bounds.center;

    public void SetDelivered(int index, int value)
    {
        if (index < 0 || index >= Delivered.Length) return;
        Delivered[index] = value;
        RefreshParts(index);
        RefreshLabel();
    }

    void RefreshParts(int index)
    {
        if (ghost != null) ghost.SetProgress(index, Delivered[index], Required(index));
    }

    public void Interact()
    {
        if (BuildingManager.Instance != null) BuildingManager.Instance.TryDeliver(this);
    }

    void BuildLabel(TMP_Text labelPrefab)
    {
        label = new GameObject($"Label_{name}").transform;
        int count = Delivered.Length;
        entries = new GameObject[count];
        counts = new TMP_Text[count];
        entryWidths = new float[count];

        for (int i = 0; i < count; i++)
        {
            RecipeCost cost = Recipe.costs[i];
            if (cost == null || cost.resource == null) continue;

            var entry = new GameObject(cost.resource.displayName);
            entry.transform.SetParent(label, false);
            entries[i] = entry;

            if (cost.resource.icon != null)
            {
                var icon = new GameObject("Icon").AddComponent<SpriteRenderer>();
                icon.transform.SetParent(entry.transform, false);
                icon.sprite = cost.resource.icon;
                Vector2 size = cost.resource.icon.bounds.size;
                float scale = IconSize / Mathf.Max(0.0001f, Mathf.Max(size.x, size.y));
                icon.transform.localScale = Vector3.one * scale;
                icon.transform.localPosition = new Vector3(IconSize * 0.5f, 0f, 0f);
            }

            if (labelPrefab != null)
            {
                TMP_Text number = Instantiate(labelPrefab, entry.transform);
                number.name = "Count";
                number.alignment = TextAlignmentOptions.Left;
                number.fontSize = 2.6f;
                number.rectTransform.pivot = new Vector2(0f, 0.5f);
                number.rectTransform.sizeDelta = new Vector2(0.6f, IconSize);
                number.transform.localPosition = new Vector3(IconSize + 0.04f, 0f, 0f);
                number.transform.localRotation = Quaternion.identity;
                counts[i] = number;
            }
        }
    }

    void RefreshLabel()
    {
        if (label == null) return;

        float total = 0f;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null) continue;

            int left = Remaining(i);
            bool show = left > 0;
            entries[i].SetActive(show);
            entryWidths[i] = 0f;
            if (!show) continue;

            float width = IconSize;
            if (counts[i] != null)
            {
                counts[i].text = left.ToString();
                counts[i].ForceMeshUpdate();
                width += 0.04f + counts[i].preferredWidth;
            }
            entryWidths[i] = width;
            total += width + EntryGap;
        }

        float x = -(total - EntryGap) * 0.5f;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null || !entries[i].activeSelf) continue;
            entries[i].transform.localPosition = new Vector3(x, 0f, 0f);
            x += entryWidths[i] + EntryGap;
        }
    }

    void LateUpdate()
    {
        if (label == null) return;

        Camera cam = BuildPlacer.Local != null ? BuildPlacer.Local.Camera : Camera.main;
        if (cam == null) return;

        Vector3 top = transform.position + transform.rotation *
            new Vector3(bounds.center.x, bounds.max.y + labelHeight + IconSize * 0.5f, bounds.center.z);

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
