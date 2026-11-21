using UnityEngine;
using UnityEngine.Rendering;

public class BuildGhost : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    Renderer[] renderers;
    MaterialPropertyBlock block;
    Color currentColor;
    bool hasColor;

    public Bounds LocalBounds { get; private set; }

    public static BuildGhost Create(BuildingRecipe recipe, Material material)
    {
        GameObject root = recipe.ghostPrefab != null
            ? Instantiate(recipe.ghostPrefab)
            : BuildFromMeshes(recipe.buildingPrefab);

        root.name = $"Ghost_{recipe.name}";
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            Destroy(col);

        Renderer[] found = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (Renderer r in found)
        {
            if (r.enabled && r.gameObject.activeInHierarchy)
            {
                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (material != null)
            {
                var mats = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                for (int i = 0; i < mats.Length; i++) mats[i] = material;
                r.sharedMaterials = mats;
            }
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        BuildGhost ghost = root.AddComponent<BuildGhost>();
        ghost.renderers = found;
        ghost.block = new MaterialPropertyBlock();
        ghost.LocalBounds = hasBounds ? bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
        return ghost;
    }

    static GameObject BuildFromMeshes(GameObject prefab)
    {
        var root = new GameObject();
        root.transform.localScale = prefab.transform.localScale;
        Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;

        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || !filter.TryGetComponent(out MeshRenderer source)) continue;

            var part = new GameObject(filter.name, typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(root.transform, false);

            Matrix4x4 local = toRoot * filter.transform.localToWorldMatrix;
            part.transform.localPosition = local.GetPosition();
            part.transform.localRotation = local.rotation;
            part.transform.localScale = local.lossyScale;

            part.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            part.GetComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
        }

        return root;
    }

    public void SetColor(Color color)
    {
        if (hasColor && color == currentColor) return;
        hasColor = true;
        currentColor = color;

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            r.SetPropertyBlock(block);
        }
    }
}
