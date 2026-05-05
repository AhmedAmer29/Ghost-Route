using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class NightSkyController : MonoBehaviour
{
    [Header("Sky")]
    [SerializeField] int starCount = 850;
    [SerializeField] float skyDistance = 420f;
    [SerializeField] float moonSize = 34f;
    [SerializeField] Vector3 moonDirection = new Vector3(0.42f, 0.48f, 0.77f);

    [Header("Lighting")]
    [SerializeField] Color moonlightColor = new Color(0.58f, 0.68f, 1f, 1f);
    [SerializeField, Min(0f)] float moonlightIntensity = 0.32f;
    [SerializeField] Color streetLightColor = new Color(1f, 0.78f, 0.48f, 1f);

    GameObject skyRoot;
    Transform moonTransform;
    Material skyboxMaterial;
    Material starMaterial;
    Material moonMaterial;
    Mesh starMesh;
    Mesh moonMesh;
    Texture2D starTexture;
    Texture2D moonTexture;
    Camera activeCamera;

#if UNITY_EDITOR
    bool applyQueued;
#endif

    void OnEnable()
    {
        Camera.onPreCull += HandleCameraPreCull;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueEditorApply();
            return;
        }
#endif

        ApplyNightSettings();
    }

    void OnValidate()
    {
        starCount = Mathf.Max(0, starCount);
        skyDistance = Mathf.Max(100f, skyDistance);
        moonSize = Mathf.Max(1f, moonSize);

#if UNITY_EDITOR
        if (!Application.isPlaying && isActiveAndEnabled)
            QueueEditorApply();
#endif
    }

    void LateUpdate()
    {
        if (skyRoot == null || skyboxMaterial == null)
            ApplyNightSettings();
        else
        {
            ApplyRenderSettings();
            FollowCamera();
        }
    }

    void OnDisable()
    {
        Camera.onPreCull -= HandleCameraPreCull;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall -= ApplyQueuedNightSettings;
            applyQueued = false;
        }
#endif

        DestroyGeneratedObjects();
    }

    void ApplyNightSettings()
    {
        ApplyRenderSettings();
        ConfigureLights();
        EnsureSkyObjects();
        FollowCamera();
    }

#if UNITY_EDITOR
    void QueueEditorApply()
    {
        if (applyQueued)
            return;

        applyQueued = true;
        EditorApplication.delayCall += ApplyQueuedNightSettings;
    }

    void ApplyQueuedNightSettings()
    {
        applyQueued = false;

        if (this == null || !isActiveAndEnabled)
            return;

        ApplyNightSettings();
    }
#endif

    void ApplyRenderSettings()
    {
        if (skyboxMaterial == null)
        {
            Shader shader = Shader.Find("Skybox/Procedural");
            skyboxMaterial = new Material(shader)
            {
                name = "Generated Main Street Night Skybox",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        if (skyboxMaterial.HasProperty("_SkyTint"))
            skyboxMaterial.SetColor("_SkyTint", new Color(0.08f, 0.11f, 0.22f, 1f));

        if (skyboxMaterial.HasProperty("_GroundColor"))
            skyboxMaterial.SetColor("_GroundColor", new Color(0.003f, 0.004f, 0.01f, 1f));

        if (skyboxMaterial.HasProperty("_Exposure"))
            skyboxMaterial.SetFloat("_Exposure", 0.25f);

        if (skyboxMaterial.HasProperty("_AtmosphereThickness"))
            skyboxMaterial.SetFloat("_AtmosphereThickness", 0.25f);

        if (skyboxMaterial.HasProperty("_SunSize"))
            skyboxMaterial.SetFloat("_SunSize", 0.02f);

        RenderSettings.skybox = skyboxMaterial;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.006f, 0.01f, 0.024f, 1f);
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.035f, 0.046f, 0.085f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.018f, 0.025f, 0.05f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.005f, 0.007f, 0.014f, 1f);
        RenderSettings.ambientIntensity = 0.45f;
        RenderSettings.reflectionIntensity = 0.08f;
    }

    void ConfigureLights()
    {
        Light moonlight = null;
        Light[] lights = FindObjectsOfType<Light>(true);

        foreach (Light sceneLight in lights)
        {
            if (sceneLight.gameObject.scene != gameObject.scene)
                continue;

            if (sceneLight.type == LightType.Directional)
            {
                moonlight = sceneLight;
                continue;
            }

            sceneLight.color = streetLightColor;
        }

        if (moonlight == null)
        {
            GameObject lightObject = new GameObject("Moonlight Directional Light");
            moonlight = lightObject.AddComponent<Light>();
        }

        moonlight.gameObject.name = "Moonlight Directional Light";
        moonlight.gameObject.SetActive(true);
        moonlight.type = LightType.Directional;
        moonlight.color = moonlightColor;
        moonlight.intensity = moonlightIntensity;
        moonlight.shadows = LightShadows.Soft;
        moonlight.shadowStrength = 0.65f;
        moonlight.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        RenderSettings.sun = moonlight;
    }

    void EnsureSkyObjects()
    {
        if (skyRoot == null)
        {
            skyRoot = new GameObject("Generated Night Sky");
            skyRoot.hideFlags = HideFlags.HideAndDontSave;
        }

        if (starMaterial == null)
        {
            Shader shader = FindNightSkyShader();
            starMaterial = new Material(shader)
            {
                name = "Generated Star Material",
                hideFlags = HideFlags.HideAndDontSave
            };
            starTexture = CreateStarTexture();
            starMaterial.mainTexture = starTexture;

            if (starMaterial.HasProperty("_Color"))
                starMaterial.SetColor("_Color", new Color(0.92f, 0.94f, 1f, 1f));
        }

        MeshFilter starFilter = EnsureChild<MeshFilter>("Stars", out GameObject stars);
        MeshRenderer starRenderer = stars.GetComponent<MeshRenderer>();
        if (starRenderer == null)
            starRenderer = stars.AddComponent<MeshRenderer>();

        if (starMesh == null || starMesh.vertexCount != starCount * 4)
            starMesh = CreateStarMesh();

        starFilter.sharedMesh = starMesh;
        starRenderer.sharedMaterial = starMaterial;
        starRenderer.shadowCastingMode = ShadowCastingMode.Off;
        starRenderer.receiveShadows = false;

        MeshFilter moonFilter = EnsureChild<MeshFilter>("Moon", out GameObject moon);
        MeshRenderer moonRenderer = moon.GetComponent<MeshRenderer>();
        if (moonRenderer == null)
            moonRenderer = moon.AddComponent<MeshRenderer>();

        if (moonMaterial == null)
            moonMaterial = CreateMoonMaterial();

        if (moonMesh == null)
            moonMesh = CreateMoonMesh();

        moonFilter.sharedMesh = moonMesh;
        moonRenderer.sharedMaterial = moonMaterial;
        moonRenderer.shadowCastingMode = ShadowCastingMode.Off;
        moonRenderer.receiveShadows = false;
        moonTransform = moon.transform;
        moonTransform.localPosition = moonDirection.normalized * (skyDistance * 0.88f);
        moonTransform.localScale = Vector3.one * moonSize;
    }

    MeshFilter EnsureChild<T>(string childName, out GameObject child) where T : Component
    {
        Transform found = skyRoot.transform.Find(childName);
        child = found != null ? found.gameObject : new GameObject(childName);
        child.hideFlags = HideFlags.HideAndDontSave;
        child.transform.SetParent(skyRoot.transform, false);

        T component = child.GetComponent<T>();
        if (component == null)
            component = child.AddComponent<T>();

        return component as MeshFilter;
    }

    Mesh CreateStarMesh()
    {
        Vector3[] vertices = new Vector3[starCount * 4];
        Vector2[] uvs = new Vector2[starCount * 4];
        int[] triangles = new int[starCount * 6];
        System.Random random = new System.Random(137);

        for (int i = 0; i < starCount; i++)
        {
            float azimuth = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
            float elevation = Mathf.Lerp(0.12f, 1.05f, (float)random.NextDouble());
            Vector3 direction = new Vector3(
                Mathf.Cos(elevation) * Mathf.Cos(azimuth),
                Mathf.Sin(elevation),
                Mathf.Cos(elevation) * Mathf.Sin(azimuth)
            ).normalized;

            Vector3 center = direction * skyDistance;
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            if (right.sqrMagnitude < 0.001f)
                right = Vector3.right;

            Vector3 up = Vector3.Cross(direction, right).normalized;
            float size = Mathf.Lerp(0.9f, 1.8f, (float)random.NextDouble());
            if (random.NextDouble() > 0.93)
                size *= 2.1f;

            int vertexIndex = i * 4;
            vertices[vertexIndex] = center - right * size - up * size;
            vertices[vertexIndex + 1] = center - right * size + up * size;
            vertices[vertexIndex + 2] = center + right * size + up * size;
            vertices[vertexIndex + 3] = center + right * size - up * size;
            uvs[vertexIndex] = new Vector2(0f, 0f);
            uvs[vertexIndex + 1] = new Vector2(0f, 1f);
            uvs[vertexIndex + 2] = new Vector2(1f, 1f);
            uvs[vertexIndex + 3] = new Vector2(1f, 0f);

            int triangleIndex = i * 6;
            triangles[triangleIndex] = vertexIndex;
            triangles[triangleIndex + 1] = vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex + 2;
            triangles[triangleIndex + 3] = vertexIndex;
            triangles[triangleIndex + 4] = vertexIndex + 2;
            triangles[triangleIndex + 5] = vertexIndex + 3;
        }

        Mesh mesh = new Mesh
        {
            name = "Generated Star Field",
            hideFlags = HideFlags.HideAndDontSave,
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };
        mesh.RecalculateBounds();
        return mesh;
    }

    Material CreateMoonMaterial()
    {
        Shader shader = FindNightSkyShader();
        Material material = new Material(shader)
        {
            name = "Generated Moon Material",
            hideFlags = HideFlags.HideAndDontSave
        };

        moonTexture = CreateMoonTexture();
        material.mainTexture = moonTexture;
        return material;
    }

    Texture2D CreateStarTexture()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = Mathf.Pow(alpha, 2.4f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }

    Texture2D CreateMoonTexture()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, clear);
        }

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.32f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 offset = new Vector2(x, y) - center;
                float distance = offset.magnitude / radius;

                if (distance <= 1f)
                {
                    float shade = Mathf.InverseLerp(-radius, radius, offset.x);
                    Color moon = Color.Lerp(new Color(0.58f, 0.64f, 0.78f, 1f), new Color(0.9f, 0.92f, 0.98f, 1f), shade);
                    texture.SetPixel(x, y, moon);
                }
                else if (distance <= 1.65f)
                {
                    float alpha = Mathf.Pow(1f - Mathf.InverseLerp(1f, 1.65f, distance), 2f) * 0.45f;
                    texture.SetPixel(x, y, new Color(0.45f, 0.54f, 0.85f, alpha));
                }
            }
        }

        DrawCrater(texture, new Vector2(100f, 148f), 14f);
        DrawCrater(texture, new Vector2(147f, 114f), 10f);
        DrawCrater(texture, new Vector2(122f, 91f), 8f);
        texture.Apply();
        return texture;
    }

    void DrawCrater(Texture2D texture, Vector2 center, float radius)
    {
        int minX = Mathf.FloorToInt(center.x - radius);
        int maxX = Mathf.CeilToInt(center.x + radius);
        int minY = Mathf.FloorToInt(center.y - radius);
        int maxY = Mathf.CeilToInt(center.y + radius);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
                    continue;

                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                if (distance > 1f)
                    continue;

                Color current = texture.GetPixel(x, y);
                Color crater = new Color(0.42f, 0.47f, 0.6f, current.a);
                texture.SetPixel(x, y, Color.Lerp(current, crater, 0.28f * (1f - distance)));
            }
        }
    }

    Mesh CreateMoonMesh()
    {
        Mesh mesh = new Mesh
        {
            name = "Generated Moon Quad",
            hideFlags = HideFlags.HideAndDontSave
        };

        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
        mesh.RecalculateBounds();
        return mesh;
    }

    void FollowCamera()
    {
        Camera camera = activeCamera != null ? activeCamera : Camera.main;
        if (camera == null || skyRoot == null)
            return;

        skyRoot.transform.position = camera.transform.position;

        if (moonTransform != null)
            moonTransform.rotation = Quaternion.LookRotation(moonTransform.position - camera.transform.position, Vector3.up);
    }

    void HandleCameraPreCull(Camera camera)
    {
        if (camera == null || !camera.isActiveAndEnabled)
            return;

        if (camera.cameraType != CameraType.Game && camera.cameraType != CameraType.SceneView)
            return;

        activeCamera = camera;

        if (skyRoot == null || skyboxMaterial == null)
            ApplyNightSettings();
        else
            FollowCamera();
    }

    void DestroyGeneratedObjects()
    {
        DestroyGeneratedObject(skyRoot);
        DestroyGeneratedObject(skyboxMaterial);
        DestroyGeneratedObject(starMaterial);
        DestroyGeneratedObject(moonMaterial);
        DestroyGeneratedObject(starMesh);
        DestroyGeneratedObject(moonMesh);
        DestroyGeneratedObject(starTexture);
        DestroyGeneratedObject(moonTexture);
    }

    static void DestroyGeneratedObject(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }

    static Shader FindNightSkyShader()
    {
        Shader shader = Shader.Find("GhostRoute/NightSkyUnlitTransparent");
        return shader != null ? shader : Shader.Find("Unlit/Transparent");
    }
}
