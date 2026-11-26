using UnityEngine;

public class CampfireFire : MonoBehaviour
{
    [SerializeField] Transform[] flames;
    [SerializeField] Light fireLight;

    [Header("Flames")]
    [SerializeField] float speed = 6f;
    [SerializeField] float heightAmount = 0.22f;
    [SerializeField] float widthAmount = 0.1f;
    [SerializeField] float swayAngle = 7f;
    [SerializeField] float spinSpeed = 25f;

    [Header("Light")]
    [SerializeField] float lightIntensity = 2.5f;
    [SerializeField] float lightFlicker = 0.3f;
    [SerializeField] float lightJitter = 0.04f;

    Vector3[] baseScales;
    Quaternion[] baseRotations;
    int[] upAxis;
    float[] seeds;
    Vector3[] parentUp;
    Vector3[] parentSide;
    Vector3[] parentFront;
    Vector3 lightBase;

    void Awake()
    {
        int count = flames.Length;
        baseScales = new Vector3[count];
        baseRotations = new Quaternion[count];
        upAxis = new int[count];
        seeds = new float[count];
        parentUp = new Vector3[count];
        parentSide = new Vector3[count];
        parentFront = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            Transform f = flames[i];
            if (f == null) continue;
            baseScales[i] = f.localScale;
            baseRotations[i] = f.localRotation;

            Vector3 up = f.parent != null ? f.parent.InverseTransformDirection(Vector3.up).normalized : Vector3.up;
            parentUp[i] = up;
            parentSide[i] = Vector3.Cross(up, Mathf.Abs(up.z) < 0.9f ? Vector3.forward : Vector3.right).normalized;
            parentFront[i] = Vector3.Cross(parentSide[i], up).normalized;

            Vector3 local = Quaternion.Inverse(f.localRotation) * up;
            Vector3 a = new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z));
            upAxis[i] = a.x > a.y && a.x > a.z ? 0 : (a.y > a.z ? 1 : 2);
            seeds[i] = Random.Range(0f, 100f);
        }

        if (fireLight != null) lightBase = fireLight.transform.localPosition;
    }

    void Update()
    {
        float t = Time.time * speed;

        for (int i = 0; i < flames.Length; i++)
        {
            Transform f = flames[i];
            if (f == null) continue;

            float s = seeds[i];
            float h = 1f + (Mathf.PerlinNoise(s, t) - 0.5f) * 2f * heightAmount;
            float w = 1f + (Mathf.PerlinNoise(s + 17f, t * 0.8f) - 0.5f) * 2f * widthAmount;

            Vector3 scale = new Vector3(w, w, w);
            scale[upAxis[i]] = h;
            f.localScale = Vector3.Scale(baseScales[i], scale);

            float swayX = (Mathf.PerlinNoise(s + 31f, t * 0.6f) - 0.5f) * 2f * swayAngle;
            float swayZ = (Mathf.PerlinNoise(s + 47f, t * 0.6f) - 0.5f) * 2f * swayAngle;
            float spin = Mathf.Sin(Time.time * 0.7f + s) * spinSpeed;
            f.localRotation = Quaternion.AngleAxis(spin, parentUp[i])
                * Quaternion.AngleAxis(swayX, parentSide[i])
                * Quaternion.AngleAxis(swayZ, parentFront[i])
                * baseRotations[i];
        }

        if (fireLight != null)
        {
            float n = Mathf.PerlinNoise(t * 1.3f, 3.7f);
            fireLight.intensity = lightIntensity * (1f + (n - 0.5f) * 2f * lightFlicker);
            fireLight.transform.localPosition = lightBase + new Vector3(
                (Mathf.PerlinNoise(t, 11f) - 0.5f) * lightJitter,
                (Mathf.PerlinNoise(t, 23f) - 0.5f) * lightJitter,
                (Mathf.PerlinNoise(t, 37f) - 0.5f) * lightJitter);
        }
    }
}
