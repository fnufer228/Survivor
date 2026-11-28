using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class PageRenderTarget : MonoBehaviour
{
    static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    [Tooltip("Camera that renders only this page canvas. If empty, one is created")]
    [SerializeField] Camera pageCamera;

    [Tooltip("Quad on the book page with a URP Lit material")]
    [SerializeField] Renderer pageSurface;

    [Tooltip("X is the texture width. Height follows the page proportions")]
    [SerializeField] Vector2Int resolution = new Vector2Int(1024, 1400);

    [Tooltip("Alpha 0 lets the book paper show through")]
    [SerializeField] Color clearColor = new Color(0f, 0f, 0f, 0f);

    RenderTexture texture;
    MaterialPropertyBlock block;
    int framesLeft;
    bool ready;

    public RenderTexture Texture => texture;

    void Awake()
    {
        if (pageSurface == null)
        {
            Debug.LogError($"PageRenderTarget: {name} has no Page Surface", this);
            return;
        }

        if (gameObject.layer == 0)
            Debug.LogWarning($"PageRenderTarget: {name} is on the Default layer. Put the canvas and its children on the BookPage layer", this);

        if (pageCamera == null) pageCamera = CreateCamera();

        RectTransform rect = (RectTransform)transform;
        Vector2 design = rect.rect.size;
        int width = Mathf.Max(64, resolution.x);
        int height = design.x > 0f && design.y > 0f
            ? Mathf.Max(64, Mathf.RoundToInt(width * design.y / design.x))
            : Mathf.Max(64, resolution.y);

        texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            name = $"Page_{name}",
            useMipMap = true,
            autoGenerateMips = true,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 4
        };
        texture.Create();

        pageCamera.targetTexture = texture;
        pageCamera.clearFlags = CameraClearFlags.SolidColor;
        pageCamera.backgroundColor = clearColor;
        pageCamera.cullingMask = 1 << gameObject.layer;
        pageCamera.enabled = false;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = pageCamera;
        canvas.planeDistance = 1f;

        float scale = design.x > 0f ? width / design.x : 1f;
        if (TryGetComponent(out CanvasScaler scaler))
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = scale;
        }
        else
        {
            canvas.scaleFactor = scale;
        }

        if (TryGetComponent(out GraphicRaycaster raycaster)) raycaster.enabled = false;

        block = new MaterialPropertyBlock();
        pageSurface.GetPropertyBlock(block);
        block.SetTexture(BaseMapId, texture);
        block.SetTexture(MainTexId, texture);
        pageSurface.SetPropertyBlock(block);

        ready = true;
        Refresh();
    }

    Camera CreateCamera()
    {
        var go = new GameObject($"{name}_Camera");
        go.transform.SetParent(transform.parent, false);
        go.transform.localPosition = transform.localPosition;
        go.transform.localRotation = transform.localRotation;

        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 1f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 2f;
        cam.depth = -10f;
        return cam;
    }

    public void Refresh()
    {
        if (!ready) return;
        framesLeft = 3;
        pageCamera.enabled = true;
    }

    void LateUpdate()
    {
        if (framesLeft <= 0) return;

        framesLeft--;
        if (framesLeft == 0) pageCamera.enabled = false;
    }

    void OnDestroy()
    {
        if (pageCamera != null) pageCamera.targetTexture = null;
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
    }
}
