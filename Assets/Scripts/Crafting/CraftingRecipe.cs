using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCraftingRecipe", menuName = "Crafting/Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [System.Serializable]
    public class Ingredient
    {
        public ResourceType item;
        public int amount = 1;
    }

    public List<Ingredient> ingredients = new List<Ingredient>();
    public ResourceType result;
    public int resultAmount = 1;
}