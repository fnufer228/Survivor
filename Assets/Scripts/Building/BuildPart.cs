using UnityEngine;

public class BuildPart : MonoBehaviour
{
    [Tooltip("Index in BuildingRecipe.costs. Parts with the same index are filled in hierarchy order")]
    public int costIndex;

    [Tooltip("Materials of the finished part")]
    public Material[] solidMaterials;

    [Tooltip("Outline shown while the part is not built yet")]
    public Renderer outline;
}
