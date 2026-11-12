
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }
    
    [SerializeField] private ItemPlacement itemPlacement;
    [SerializeField] private List<CraftingRecipe> recipes = new List<CraftingRecipe>();
    
    private void Awake()
    {
        Instance = this;
    }
    public void CheckCraft()
    {
        if (itemPlacement == null || InventoryManager.Instance == null)
            return;

        foreach (CraftingRecipe recipe in recipes)
        {
            if (recipe == null || recipe.result == null || recipe.resultAmount <= 0)
                continue;

            if (!MatchesRecipe(recipe))
                continue;

            InventoryManager.Instance.AddItem(recipe.result, recipe.resultAmount);
            itemPlacement.ConsumePlacedItems();

            Debug.Log("Крафт успешен: " + recipe.result.displayName);
            return;
        }
    }

    private bool MatchesRecipe(CraftingRecipe recipe)
    {
        IReadOnlyList<ResourceType> placedItems = itemPlacement.PlacedItems;

        Dictionary<ResourceType, int> placedCounts = new Dictionary<ResourceType, int>();
        Dictionary<ResourceType, int> requiredCounts = new Dictionary<ResourceType, int>();

        foreach (ResourceType item in placedItems)
        {
            if (item == null)
                return false;

            if (!placedCounts.ContainsKey(item))
                placedCounts[item] = 0;

            placedCounts[item]++;
        }

        foreach (CraftingRecipe.Ingredient ingredient in recipe.ingredients)
        {
            if (ingredient.item == null || ingredient.amount <= 0)
                return false;

            if (!requiredCounts.ContainsKey(ingredient.item))
                requiredCounts[ingredient.item] = 0;

            requiredCounts[ingredient.item] += ingredient.amount;
        }

        if (placedCounts.Count != requiredCounts.Count)
            return false;

        foreach (KeyValuePair<ResourceType, int> required in requiredCounts)
        {
            if (!placedCounts.TryGetValue(required.Key, out int amount) || amount != required.Value)
                return false;
        }

        return requiredCounts.Count > 0;
    }
}
