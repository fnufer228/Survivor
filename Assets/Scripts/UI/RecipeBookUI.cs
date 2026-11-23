using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeBookUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Tooltip("In page order")]
    [SerializeField] BuildingRecipe[] recipes;

    [Tooltip("Pages visible at once, left to right")]
    [SerializeField] RecipePageUI[] pages;

    [Tooltip("The whole book. Turned on and off")]
    [SerializeField] GameObject root;

    [Header("Keys")]
    [SerializeField] KeyCode toggleKey = KeyCode.B;
    [SerializeField] KeyCode previousKey = KeyCode.A;
    [SerializeField] KeyCode nextKey = KeyCode.D;

    [Header("Optional")]
    [SerializeField] TMP_Text pageLabel;
    [SerializeField] Button previousButton;
    [SerializeField] Button nextButton;

    int spread;

    int SpreadCount => Mathf.Max(1, Mathf.CeilToInt(recipes.Length / (float)pages.Length));

    void Awake()
    {
        foreach (RecipePageUI page in pages)
            if (page != null) page.Selected += OnPageSelected;

        if (previousButton != null) previousButton.onClick.AddListener(PreviousSpread);
        if (nextButton != null) nextButton.onClick.AddListener(NextSpread);

        if (root == gameObject)
            Debug.LogError("RecipeBookUI: Root must be a child object, otherwise the book can not be opened again", this);

        root.SetActive(false);
        IsOpen = false;
    }

    void OnDestroy()
    {
        IsOpen = false;
        CursorController.Hide(this);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetOpen(!IsOpen);
            return;
        }

        if (!IsOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
        else if (Input.GetKeyDown(previousKey) || Input.GetKeyDown(KeyCode.LeftArrow)) PreviousSpread();
        else if (Input.GetKeyDown(nextKey) || Input.GetKeyDown(KeyCode.RightArrow)) NextSpread();
    }

    public void SetOpen(bool open)
    {
        if (open && (recipes == null || recipes.Length == 0 || pages == null || pages.Length == 0))
        {
            Debug.LogWarning("RecipeBookUI: assign Recipes and Pages", this);
            return;
        }

        IsOpen = open;
        root.SetActive(open);

        CursorController.Set(this, open);

        if (open)
        {
            if (BuildPlacer.Local != null) BuildPlacer.Local.Cancel();
            ShowSpread(spread);
        }
    }

    public void PreviousSpread() => ShowSpread(spread - 1);

    public void NextSpread() => ShowSpread(spread + 1);

    void ShowSpread(int index)
    {
        spread = Mathf.Clamp(index, 0, SpreadCount - 1);

        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] == null) continue;
            int recipeIndex = spread * pages.Length + i;
            pages[i].Show(recipeIndex < recipes.Length ? recipes[recipeIndex] : null);
        }

        if (pageLabel != null) pageLabel.text = $"{spread + 1} / {SpreadCount}";
        if (previousButton != null) previousButton.interactable = spread > 0;
        if (nextButton != null) nextButton.interactable = spread < SpreadCount - 1;
    }

    void OnPageSelected(RecipePageUI page)
    {
        BuildPlacer placer = BuildPlacer.Local;
        if (placer == null)
        {
            Debug.LogWarning("RecipeBookUI: local player has no BuildPlacer", this);
            return;
        }

        BuildingRecipe recipe = page.Recipe;
        SetOpen(false);
        placer.StartPlacing(recipe);
    }
}
