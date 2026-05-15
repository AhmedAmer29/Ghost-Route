using UnityEngine;

[ExecuteAlways]
public class SewerFlashlight : MonoBehaviour
{
    [Header("Scene Darkness")]
    public bool darkenScene = true;
    [ColorUsage(false, true)] public Color ambientColor = new Color(0.012f, 0.012f, 0.018f);
    public float fogDensity = 0.035f;
    public Color fogColor = new Color(0.005f, 0.005f, 0.008f);
    [Tooltip("Disable existing directional/sun lights so the flashlight is the only real light source")]
    public bool killOtherDirectionalLights = true;
    [Tooltip("Dim reflection probe / skybox bounce that fakes ambient")]
    public bool killReflectionContribution = true;

    [Header("Flashlight Beam")]
    [Range(0f, 8f)] public float intensity = 1.1f;
    [Range(2f, 50f)] public float range = 15f;
    [Range(5f, 100f)] public float spotAngle = 34f;
    [Range(2f, 100f)] public float innerSpotAngle = 16f;
    public Color color = new Color(1f, 0.9f, 0.72f);
    public bool shadows = true;
    public bool enabledBeam = true;

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.L;

    private Light _light;
    private bool _killedDirOnce;

    void OnEnable()
    {
        EnsureBeam();
    }

    void Start()
    {
        EnsureBeam();
    }

    void Update()
    {
        if (Application.isPlaying && Input.GetKeyDown(toggleKey))
            enabledBeam = !enabledBeam;

        ApplyDarkness();
        ApplyBeam();
    }

    void OnValidate()
    {
        // Push values in editor immediately when user tweaks in inspector
        ApplyDarkness();
        ApplyBeam();
    }

    void EnsureBeam()
    {
        if (_light != null) return;

        Transform existing = transform.Find("Flashlight_Beam");
        GameObject beamGO = existing != null ? existing.gameObject : new GameObject("Flashlight_Beam");
        beamGO.transform.SetParent(transform, false);
        beamGO.transform.localPosition = Vector3.zero;
        beamGO.transform.localRotation = Quaternion.identity;

        _light = beamGO.GetComponent<Light>();
        if (_light == null) _light = beamGO.AddComponent<Light>();
        _light.type = LightType.Spot;
        _light.renderMode = LightRenderMode.ForcePixel;
    }

    void ApplyBeam()
    {
        if (_light == null) return;
        _light.enabled = enabledBeam;
        _light.color = color;
        _light.intensity = intensity;
        _light.range = range;
        _light.spotAngle = spotAngle;
        _light.innerSpotAngle = Mathf.Min(innerSpotAngle, spotAngle - 1f);
        _light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
    }

    void ApplyDarkness()
    {
        if (!darkenScene) return;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;
        RenderSettings.ambientIntensity = 0f;

        if (killReflectionContribution)
        {
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = null;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.skybox = null;
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        if (Application.isPlaying && killOtherDirectionalLights && !_killedDirOnce)
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l == _light) continue;
                if (l.type == LightType.Directional) l.enabled = false;
            }
            _killedDirOnce = true;
        }
    }
}
