using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter))]
public class LayeredDarknessVoid : MonoBehaviour
{
    [Header("Depth")]
    [SerializeField, Min(1)] int layerCount = 7;
    [SerializeField, Min(0.01f)] float layerSpacing = 0.35f;
    [SerializeField] Vector3 localDepthDirection = Vector3.forward;
    [SerializeField, Min(1f)] float scaleGrowth = 1.08f;
    [SerializeField] bool hideSourceRenderer = true;

    [Header("Darkness Ramp")]
    [SerializeField, Range(0f, 1f)] float frontAlpha = 0.18f;
    [SerializeField, Range(0f, 1f)] float backAlpha = 0.86f;
    [SerializeField] Color frontColor = new Color(0.025f, 0.03f, 0.04f, 1f);
    [SerializeField] Color backColor = Color.black;

    [Header("Cloud Shape")]
    [SerializeField, Range(0.01f, 0.5f)] float edgeFade = 0.24f;
    [SerializeField, Range(1f, 30f)] float cloudScale = 5.5f;
    [SerializeField, Range(0f, 1f)] float cloudBreakup = 0.42f;

    [Header("Back Stop")]
    [SerializeField] bool addSolidBackLayer = true;
    [SerializeField, Min(0f)] float backLayerExtraDistance = 0.18f;

    readonly List<GameObject> generatedLayers = new List<GameObject>();
    readonly List<Texture2D> generatedTextures = new List<Texture2D>();
    MeshFilter sourceFilter;
    MeshRenderer sourceRenderer;

#if UNITY_EDITOR
    bool rebuildQueued;
#endif

    void OnEnable()
    {
        QueueRebuild();
    }

    void OnValidate()
    {
        layerCount = Mathf.Max(1, layerCount);
        layerSpacing = Mathf.Max(0.01f, layerSpacing);
        scaleGrowth = Mathf.Max(1f, scaleGrowth);

        if (isActiveAndEnabled)
            QueueRebuild();
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.delayCall -= RebuildFromEditorDelay;
        rebuildQueued = false;
#endif
        SetSourceRendererVisible(true);
        ClearGeneratedLayers();
    }

    void QueueRebuild()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (rebuildQueued)
                return;

            rebuildQueued = true;
            EditorApplication.delayCall += RebuildFromEditorDelay;
            return;
        }
#endif

        Rebuild();
    }

#if UNITY_EDITOR
    void RebuildFromEditorDelay()
    {
        rebuildQueued = false;

        if (this == null || !isActiveAndEnabled)
            return;

        Rebuild();
    }
#endif

    [ContextMenu("Rebuild Layers")]
    public void Rebuild()
    {
        sourceFilter = GetComponent<MeshFilter>();
        sourceRenderer = GetComponent<MeshRenderer>();

        if (sourceFilter == null || sourceFilter.sharedMesh == null)
            return;

        ClearGeneratedLayers();
        SetSourceRendererVisible(!hideSourceRenderer);

        Vector3 direction = localDepthDirection.sqrMagnitude > 0.001f
            ? localDepthDirection.normalized
            : Vector3.forward;

        for (int i = 0; i < layerCount; i++)
        {
            float t = layerCount <= 1 ? 1f : i / (float)(layerCount - 1);
            CreateLayer(i, t, direction, false);
        }

        if (addSolidBackLayer)
            CreateLayer(layerCount, 1f, direction, true);
    }

    void CreateLayer(int index, float t, Vector3 direction, bool solidBackLayer)
    {
        GameObject layer = new GameObject(solidBackLayer ? "Darkness Back Layer" : $"Darkness Layer {index + 1:00}");
        layer.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        layer.transform.SetParent(transform, false);
        layer.transform.localPosition = direction * ((index * layerSpacing) + (solidBackLayer ? backLayerExtraDistance : 0f));
        layer.transform.localRotation = Quaternion.identity;

        float layerScale = Mathf.Pow(scaleGrowth, index);
        layer.transform.localScale = new Vector3(layerScale, layerScale, layerScale);

        MeshFilter filter = layer.AddComponent<MeshFilter>();
        filter.sharedMesh = sourceFilter.sharedMesh;

        MeshRenderer renderer = layer.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = CreateLayerMaterial(index, t, solidBackLayer);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        generatedLayers.Add(layer);
    }

    Material CreateLayerMaterial(int index, float t, bool solidBackLayer)
    {
        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader)
        {
            name = solidBackLayer ? "Generated Darkness Back" : $"Generated Darkness Layer {index + 1:00}",
            hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor
        };

        float alpha = solidBackLayer ? 1f : Mathf.Lerp(frontAlpha, backAlpha, t);
        Color color = Color.Lerp(frontColor, backColor, t);
        color.a = alpha;

        Texture2D texture = CreateLayerTexture(index, t, solidBackLayer);
        generatedTextures.Add(texture);

        if (material.HasProperty("_MainTex"))
            material.SetTexture("_MainTex", texture);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        return material;
    }

    Texture2D CreateLayerTexture(int layerIndex, float t, bool solidBackLayer)
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = solidBackLayer ? "Generated Darkness Back Texture" : $"Generated Darkness Texture {layerIndex + 1:00}",
            hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float layerSeed = 17.31f + layerIndex * 9.73f;
        float cutoff = Mathf.Lerp(0.18f, 0.4f, t);
        float scale = Mathf.Max(1f, cloudScale + layerIndex * 0.35f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (size - 1f), y / (size - 1f));
                float edgeDistance = Mathf.Min(Mathf.Min(uv.x, 1f - uv.x), Mathf.Min(uv.y, 1f - uv.y));
                float edge = SmoothStep(0f, solidBackLayer ? edgeFade * 0.55f : edgeFade, edgeDistance);
                float alpha = edge;

                if (!solidBackLayer)
                {
                    float cloud = FractalNoise(uv * scale + new Vector2(layerSeed, layerSeed * 0.47f));
                    float breakup = SmoothStep(cutoff, 1f, cloud);
                    alpha *= Mathf.Lerp(1f, breakup, cloudBreakup);
                }

                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    static float FractalNoise(Vector2 p)
    {
        float value = Mathf.PerlinNoise(p.x, p.y) * 0.55f;
        value += Mathf.PerlinNoise(p.x * 2.07f + 8.31f, p.y * 2.07f - 3.77f) * 0.3f;
        value += Mathf.PerlinNoise(p.x * 4.13f - 2.19f, p.y * 4.13f + 4.91f) * 0.15f;
        return Mathf.Clamp01(value);
    }

    static float SmoothStep(float from, float to, float value)
    {
        if (Mathf.Approximately(from, to))
            return value >= to ? 1f : 0f;

        float t = Mathf.Clamp01((value - from) / (to - from));
        return t * t * (3f - 2f * t);
    }

    void SetSourceRendererVisible(bool visible)
    {
        if (sourceRenderer != null)
            sourceRenderer.enabled = visible;
    }

    void ClearGeneratedLayers()
    {
        for (int i = generatedLayers.Count - 1; i >= 0; i--)
            DestroyLayer(generatedLayers[i]);

        generatedLayers.Clear();

        for (int i = generatedTextures.Count - 1; i >= 0; i--)
            DestroyGeneratedObject(generatedTextures[i]);

        generatedTextures.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Darkness Layer") || child.name == "Darkness Back Layer")
                DestroyLayer(child.gameObject);
        }
    }

    void DestroyLayer(GameObject layer)
    {
        if (layer == null)
            return;

        MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
        if (renderer != null)
            DestroyGeneratedObject(renderer.sharedMaterial);

        DestroyGeneratedObject(layer);
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
}
