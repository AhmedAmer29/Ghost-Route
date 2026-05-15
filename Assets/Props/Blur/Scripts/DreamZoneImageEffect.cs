using UnityEngine;

/// <summary>
/// Attach to the Main Camera.
/// Reads DreamZoneTrigger.BlurStrength (0-1) and applies a multi-pass
/// separated Gaussian blur via OnRenderImage.
/// Assign the Hidden/DreamBlur shader in the Inspector.
/// </summary>
[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class DreamZoneImageEffect : MonoBehaviour
{
    [Tooltip("Assign the 'Hidden/DreamBlur' shader here.")]
    public Shader blurShader;

    [Tooltip("Max pixel spread at full blur strength.")]
    [Range(0.5f, 6f)]
    public float maxSpread = 3.5f;

    [Tooltip("How many H+V passes at full strength (more = softer but heavier).")]
    [Range(1, 4)]
    public int maxPasses = 2;

    // Set by DreamZoneTrigger; range 0 (clear) to 1 (max blur)
    public static float BlurStrength = 0f;

    private Material _mat;
    private static readonly int OffsetID = Shader.PropertyToID("_Offset");

    private Material Mat
    {
        get
        {
            if (_mat == null && blurShader != null)
                _mat = new Material(blurShader) { hideFlags = HideFlags.HideAndDontSave };
            return _mat;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (BlurStrength < 0.005f || Mat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        float spread = BlurStrength * maxSpread;
        int   passes = Mathf.Max(1, Mathf.RoundToInt(BlurStrength * maxPasses));

        RenderTexture a = RenderTexture.GetTemporary(src.descriptor);
        RenderTexture b = RenderTexture.GetTemporary(src.descriptor);

        Graphics.Blit(src, a);

        for (int p = 0; p < passes; p++)
        {
            // Horizontal
            Mat.SetVector(OffsetID, new Vector4(spread / src.width, 0f, 0f, 0f));
            Graphics.Blit(a, b, Mat);

            // Vertical
            Mat.SetVector(OffsetID, new Vector4(0f, spread / src.height, 0f, 0f));
            Graphics.Blit(b, a, Mat);
        }

        Graphics.Blit(a, dest);

        RenderTexture.ReleaseTemporary(a);
        RenderTexture.ReleaseTemporary(b);
    }

    void OnDestroy()
    {
        if (_mat != null)
            DestroyImmediate(_mat);
    }
}
