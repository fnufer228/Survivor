using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipePageUI : MonoBehaviour
{
    [Tooltip("Picture of the building")]
    [SerializeField] Image picture;

    [Tooltip("Optional")]
    [SerializeField] TMP_Text title;

    [Tooltip("Object that holds the resource icons")]
    [SerializeField] Transform content;

    [Tooltip("One resource icon with the amount text. If empty, the first child of Content is used")]
    [SerializeField] RecipeCostRowUI rowTemplate;

    [Tooltip("Clicking it selects the recipe on this page")]
    [SerializeField] Button selectButton;

    public event System.Action<RecipePageUI> Selected;
    public BuildingRecipe Recipe { get; private set; }

    readonly List<RecipeCostRowUI> rows = new List<RecipeCostRowUI>();
    bool initialized;

    void Init()
    {
        if (initialized) return;
        initialized = true;
        if (rowTemplate == null && content != null && content.childCount > 0)
        {
            Transform first = content.GetChild(0);
            if (!first.TryGetComponent(out rowTemplate))
                rowTemplate = first.gameObject.AddComponent<RecipeCostRowUI>();
        }

        if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
        else Debug.LogError("RecipePageUI: Content has no resource icon to use as a template", this);

        if (selectButton != null)
            selectButton.onClick.AddListener(() =>
            {
                if (Recipe != null) Selected?.Invoke(this);
            });
    }

    public void Show(BuildingRecipe recipe)
    {
        Init();
        Recipe = recipe;
        bool hasRecipe = recipe != null;

        if (picture != null)
        {
            picture.sprite = hasRecipe ? recipe.picture : null;
            picture.enabled = picture.sprite != null;
        }

        if (title != null) title.text = hasRecipe ? recipe.displayName : string.Empty;
        if (selectButton != null) selectButton.interactable = hasRecipe;

        ShowCosts(hasRecipe ? recipe.costs : null);
    }

    void ShowCosts(RecipeCost[] costs)
    {
        if (rowTemplate == null) return;

        int count = costs == null ? 0 : costs.Length;

        while (rows.Count < count)
            rows.Add(Instantiate(rowTemplate, content));

        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < count && costs[i] != null && costs[i].resource != null;
            rows[i].gameObject.SetActive(used);
            if (used) rows[i].Set(costs[i]);
        }
    }
}
