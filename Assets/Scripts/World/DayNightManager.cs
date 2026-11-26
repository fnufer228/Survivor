using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[ExecuteAlways]
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance { get; private set; }

    public const string StartTimeKey = "dayStart";

    [Header("Time")]
    [Min(1f)] public float dayDuration = 60f;
    [Min(1f)] public float nightDuration = 60f;
    [Range(0f, 1f)] public float startTimeOfDay = 0.3f;
    [Range(0f, 1f)] public float previewTimeOfDay = 0.5f;

    [Header("Orbit")]
    [Range(-180f, 180f)] public float orbitYaw = 30f;
    [Range(0f, 60f)] public float orbitTilt = 25f;
    public float skyDistance = 700f;

    [Header("Scene")]
    public Light sunLight;
    public Transform skyRoot;
    public Transform sun;
    public Transform sunRays;
    public Transform moon;
    public Transform stars;
    public Transform clouds;

    [Header("Materials")]
    public Material skyMaterial;
    public Material cloudMaterial;
    public Material sunMaterial;
    public Material moonMaterial;
    public Material starMaterial;

    [Header("Lighting")]
    public float sunIntensity = 1.3f;
    public float moonIntensity = 0.35f;
    public Color moonLightColor = new Color(0.62f, 0.68f, 1f);
    public float cloudSpeed = 0.6f;
    public float sunRaySpin = 6f;

    public Gradient skyTop = G((0f, C(10, 18, 84)), (0.3f, C(26, 30, 107)), (0.4f, C(77, 89, 178)), (0.6f, C(38, 128, 235)), (1f, C(31, 140, 245)));
    public Gradient skyHorizon = G((0f, C(54, 43, 173)), (0.3f, C(115, 77, 166)), (0.4f, C(255, 158, 133)), (0.6f, C(140, 217, 255)), (1f, C(110, 230, 255)));
    public Gradient lightColor = G((0.37f, C(255, 140, 77)), (0.6f, C(255, 235, 209)), (1f, C(255, 250, 240)));
    public Gradient ambientSky = G((0f, C(36, 36, 82)), (0.4f, C(140, 115, 140)), (1f, C(158, 184, 217)));
    public Gradient ambientEquator = G((0f, C(26, 26, 56)), (0.4f, C(140, 102, 102)), (1f, C(128, 148, 158)));
    public Gradient ambientGround = G((0f, C(13, 13, 26)), (1f, C(64, 61, 56)));
    public Gradient cloudLit = G((0f, C(184, 173, 242)), (0.4f, C(255, 199, 184)), (1f, C(255, 255, 255)));
    public Gradient cloudShadow = G((0f, C(92, 97, 204)), (0.4f, C(158, 140, 217)), (1f, C(173, 191, 250)));

    public float TimeOfDay { get; private set; }
    public bool IsDay => TimeOfDay >= 0.25f && TimeOfDay < 0.75f;
    public int DayIndex { get; private set; }

    Material skyInstance, cloudInstance, sunInstance, moonInstance, starInstance;
    double roomStartTime = double.NaN;

    static Color C(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

    static Gradient G(params (float t, Color c)[] keys)
    {
        var g = new Gradient();
        var ck = new GradientColorKey[keys.Length];
        for (int i = 0; i < keys.Length; i++) ck[i] = new GradientColorKey(keys[i].c, keys[i].t);
        g.SetKeys(ck, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }

    void OnEnable()
    {
        Instance = this;
        if (!Application.isPlaying) return;
        skyInstance = Instantiate(skyMaterial);
        cloudInstance = Swap(clouds, cloudMaterial);
        sunInstance = Swap(sun, sunMaterial);
        moonInstance = Swap(moon, moonMaterial);
        starInstance = Swap(stars, starMaterial);
        RenderSettings.skybox = skyInstance;
    }

    Material Swap(Transform root, Material shared)
    {
        if (shared == null) return null;
        var inst = Instantiate(shared);
        if (root != null)
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterial == shared) r.sharedMaterial = inst;
        return inst;
    }

    float ComputeTimeOfDay()
    {
        if (!Application.isPlaying) return previewTimeOfDay;
        if (!PhotonNetwork.InRoom) roomStartTime = double.NaN;
        double now = PhotonNetwork.InRoom ? RoomElapsed() : Time.timeSinceLevelLoad;
        double cycle = dayDuration + nightDuration;
        float startOffset = ToCycleSeconds(startTimeOfDay);
        double t = now + startOffset;
        DayIndex = (int)System.Math.Floor(t / cycle);
        float p = (float)(t - DayIndex * cycle);
        return p < dayDuration
            ? 0.25f + 0.5f * (p / dayDuration)
            : Mathf.Repeat(0.75f + 0.5f * ((p - dayDuration) / nightDuration), 1f);
    }

    double RoomElapsed()
    {
        if (double.IsNaN(roomStartTime))
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room.CustomProperties.TryGetValue(StartTimeKey, out object stored) && stored is double start)
            {
                roomStartTime = start;
            }
            else if (PhotonNetwork.IsMasterClient)
            {
                roomStartTime = PhotonNetwork.Time;
                room.SetCustomProperties(new Hashtable { { StartTimeKey, roomStartTime } });
            }
            else
            {
                return 0d; // властивість від майстра ще не прийшла — поки що ранок (startTimeOfDay)
            }
        }

        double elapsed = PhotonNetwork.Time - roomStartTime;
        if (elapsed < 0d) elapsed += 4294967.296d; // PhotonNetwork.Time переповнюється раз на ~49 днів
        return elapsed;
    }

    float ToCycleSeconds(float tod)
    {
        float fromSunrise = Mathf.Repeat(tod - 0.25f, 1f);
        return fromSunrise < 0.5f ? fromSunrise / 0.5f * dayDuration : dayDuration + (fromSunrise - 0.5f) / 0.5f * nightDuration;
    }

    Vector3 SunDirection(float tod)
    {
        float a = (tod - 0.25f) * Mathf.PI * 2f;
        Quaternion yaw = Quaternion.Euler(0f, orbitYaw, 0f);
        Vector3 east = yaw * Vector3.right;
        Vector3 south = yaw * Vector3.back;
        Vector3 up = Vector3.up * Mathf.Cos(orbitTilt * Mathf.Deg2Rad) + south * Mathf.Sin(orbitTilt * Mathf.Deg2Rad);
        return (east * Mathf.Cos(a) + up * Mathf.Sin(a)).normalized;
    }

    void LateUpdate() => Refresh();

    public void Refresh()
    {
        TimeOfDay = ComputeTimeOfDay();
        Vector3 sunDir = SunDirection(TimeOfDay);
        Vector3 moonDir = -sunDir;
        float k = Mathf.InverseLerp(-0.35f, 0.6f, sunDir.y);
        float dayWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.12f, 0.08f, sunDir.y));

        Material sky = Application.isPlaying ? skyInstance : skyMaterial;
        Material cloudM = Application.isPlaying ? cloudInstance : cloudMaterial;
        Material sunM = Application.isPlaying ? sunInstance : sunMaterial;
        Material moonM = Application.isPlaying ? moonInstance : moonMaterial;

        Color horizon = skyHorizon.Evaluate(k);
        if (sky != null)
        {
            sky.SetColor("_TopColor", skyTop.Evaluate(k));
            sky.SetColor("_HorizonColor", horizon);
            sky.SetColor("_BottomColor", Color.Lerp(horizon, skyTop.Evaluate(k), 0.4f));
            sky.SetVector("_SunDir", sunDir);
            sky.SetVector("_MoonDir", moonDir);
            float sunset = 1f - Mathf.Clamp01(Mathf.Abs(sunDir.y) / 0.35f);
            sky.SetColor("_SunGlowColor", Color.Lerp(new Color(1f, 0.92f, 0.7f), new Color(1f, 0.45f, 0.3f), sunset));
            sky.SetFloat("_SunGlowStrength", Mathf.Lerp(0.35f, 0.9f, sunset) * Mathf.Clamp01((sunDir.y + 0.2f) * 4f));
            sky.SetFloat("_SunGlowSize", Mathf.Lerp(24f, 5f, sunset));
            sky.SetFloat("_MoonGlowStrength", 0.35f * (1f - dayWeight));
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky.Evaluate(k);
        RenderSettings.ambientEquatorColor = ambientEquator.Evaluate(k);
        RenderSettings.ambientGroundColor = ambientGround.Evaluate(k);
        RenderSettings.fogColor = horizon;

        if (sunLight != null)
        {
            Quaternion sunRot = Quaternion.LookRotation(-sunDir);
            Quaternion moonRot = Quaternion.LookRotation(-moonDir);
            sunLight.transform.rotation = dayWeight > 0.5f ? sunRot : moonRot;
            sunLight.color = Color.Lerp(moonLightColor, lightColor.Evaluate(k), dayWeight);
            float si = sunIntensity * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.02f, 0.25f, sunDir.y));
            float mi = moonIntensity * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.02f, 0.25f, moonDir.y));
            sunLight.intensity = Mathf.Max(si, mi);
            RenderSettings.sun = sunLight;
        }

        Vector3 center = skyRoot != null ? skyRoot.position : transform.position;
        if (Application.isPlaying && skyRoot != null)
        {
            var cam = Camera.main;
            if (cam != null) { skyRoot.position = cam.transform.position; center = skyRoot.position; }
        }

        if (sun != null)
        {
            sun.position = center + sunDir * skyDistance;
            sun.rotation = Quaternion.LookRotation(sunDir);
            sun.gameObject.SetActive(sunDir.y > -0.25f);
        }
        if (sunRays != null)
            sunRays.localRotation = Quaternion.Euler(0f, 0f, (Application.isPlaying ? Time.time : 0f) * sunRaySpin);
        if (sunM != null)
        {
            float low = 1f - Mathf.Clamp01(sunDir.y / 0.3f);
            sunM.SetColor("_LitColor", Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(1f, 0.55f, 0.15f), low));
            sunM.SetColor("_ShadowColor", Color.Lerp(new Color(1f, 0.72f, 0.12f), new Color(0.95f, 0.38f, 0.1f), low));
            sunM.SetVector("_LightDirWS", -sunDir + Vector3.up * 0.6f);
        }

        if (moon != null)
        {
            moon.position = center + moonDir * skyDistance;
            moon.rotation = Quaternion.LookRotation(moonDir);
            moon.gameObject.SetActive(moonDir.y > -0.25f);
        }
        if (moonM != null)
        {
            float phase = Mathf.Repeat(DayIndex / 8f + 0.5f, 1f) * Mathf.PI * 2f;
            Vector3 toViewer = -moonDir;
            Vector3 side = Vector3.Cross(Vector3.up, toViewer).normalized;
            Vector3 phaseLight = (-Mathf.Cos(phase) * toViewer + Mathf.Sin(phase) * side).normalized;
            moonM.SetVector("_LightDirWS", phaseLight);
        }

        if (stars != null)
        {
            float vis = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.2f, 0.05f, sunDir.y));
            stars.gameObject.SetActive(vis > 0.01f);
            stars.localScale = Vector3.one * Mathf.Max(vis, 0.01f);
            stars.localRotation = Quaternion.Euler(orbitTilt, orbitYaw, TimeOfDay * 360f);
        }

        if (clouds != null)
        {
            if (Application.isPlaying) clouds.Rotate(0f, cloudSpeed * Time.deltaTime, 0f, Space.World);
            if (cloudM != null)
            {
                cloudM.SetColor("_LitColor", cloudLit.Evaluate(k));
                cloudM.SetColor("_ShadowColor", cloudShadow.Evaluate(k));
                Vector3 l = dayWeight > 0.5f ? sunDir : moonDir;
                cloudM.SetVector("_LightDirWS", (l + Vector3.up * 0.8f).normalized);
            }
        }
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }
}
