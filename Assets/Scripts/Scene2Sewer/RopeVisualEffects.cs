using UnityEngine;

public class RopeVisualEffects : MonoBehaviour
{
    [Header("Sway Settings")]
    public float baseSwaySpeed = 1.0f;
    public float baseSwayAmount = 0.2f;
    public float intensityMultiplier = 1.0f;

    [Header("Audio")]
    public AudioSource creakSource;
    public float minCreakPitch = 0.8f;
    public float maxCreakPitch = 1.2f;

    [Header("Rope Mesh (Optional)")]
    public SkinnedMeshRenderer ropeRenderer;
    
    private RopeCrossingController _controller;
    private Vector3 _initialRotation;

    void Start()
    {
        _controller = GetComponentInParent<RopeCrossingController>();
        _initialRotation = transform.localEulerAngles;
    }

    void Update()
    {
        float progress = _controller != null ? _controller.progress : 0f;
        
        // Increase intensity as we progress
        float currentIntensity = intensityMultiplier * (1f + progress);
        
        // Calculate Sway
        float sway = Mathf.Sin(Time.time * baseSwaySpeed * currentIntensity) * baseSwayAmount * currentIntensity;
        
        // Apply rotation to represent swaying rope
        transform.localEulerAngles = _initialRotation + new Vector3(0, 0, sway * 5f);

        // Audio Modulation
        if (creakSource != null && creakSource.isPlaying)
        {
            float pitch = Mathf.Lerp(minCreakPitch, maxCreakPitch, Mathf.Abs(sway));
            creakSource.pitch = pitch;
            creakSource.volume = 0.2f + Mathf.Abs(sway) * 0.5f;
        }

        // Fraying Visuals (if using BlendShapes)
        if (ropeRenderer != null)
        {
            // Assuming blendshape 0 is "Fray"
            ropeRenderer.SetBlendShapeWeight(0, progress * 100f);
        }
    }
}
