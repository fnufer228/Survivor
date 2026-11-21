using UnityEngine;

[System.Serializable]
public class RecipeCost
{
    public BuildResource resource;
    [Min(1)] public int amount = 1;
}

[CreateAssetMenu(menuName = "Game/Building Recipe")]
public class BuildingRecipe : ScriptableObject
{
    [Header("Book page")]
    public string displayName = "Shelter";
    [TextArea(2, 6)] public string description;
    public Sprite picture;
    public RecipeCost[] costs;

    [Header("Placement")]
    [Tooltip("Finished building prefab")]
    public GameObject buildingPrefab;

    [Tooltip("Optional visual-only prefab for the ghost. If empty, the ghost is built from the meshes of the building prefab")]
    public GameObject ghostPrefab;

    [Tooltip("Degrees per rotation step")]
    [Min(1f)] public float rotationStep = 15f;

    [Tooltip("Lift the ghost above the ground")]
    public float groundOffset = 0f;
}
