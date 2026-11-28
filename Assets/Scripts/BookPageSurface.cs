using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BookPageSurface : MonoBehaviour
{
    [SerializeField] RecipePageUI page;

    public RecipePageUI Page => page;
}
