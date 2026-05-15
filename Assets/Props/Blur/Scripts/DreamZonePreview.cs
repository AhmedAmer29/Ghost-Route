using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Attach to DreamZone. Right-click → "Preview ON" to see the full dream effect
/// in the Editor without hitting Play. Right-click → "Preview OFF" to reset.
/// </summary>
public class DreamZonePreview : MonoBehaviour
{
    [Range(0f, 1f)]
    public float previewStrength = 1f;

    [ContextMenu("Preview ON")]
    public void PreviewOn()
    {
        DreamZoneImageEffect.BlurStrength = previewStrength;

        var vol = GetComponent<PostProcessVolume>();
        if (vol != null) vol.weight = previewStrength;

        var clouds = GetComponentInChildren<ParticleSystem>();
        if (clouds != null) clouds.Play();

        Debug.Log("Dream Zone preview ON");
    }

    [ContextMenu("Preview OFF")]
    public void PreviewOff()
    {
        DreamZoneImageEffect.BlurStrength = 0f;

        var vol = GetComponent<PostProcessVolume>();
        if (vol != null) vol.weight = 0f;

        var clouds = GetComponentInChildren<ParticleSystem>();
        if (clouds != null) clouds.Stop();

        Debug.Log("Dream Zone preview OFF");
    }
}
