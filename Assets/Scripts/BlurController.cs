using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

// Attach this to the PostProcessVolume GameObject.
public class BlurController : MonoBehaviour
{
    public static BlurController Instance;

    private PostProcessVolume volume;
    private DepthOfField       dof;

    // Blur amount range
    // focalLength  : 10 (sharp)  →  300 (very blurry)
    // aperture     : 22 (sharp)  →  0.1 (very blurry)
    // focusDistance: 10 (sharp)  →  0.1 (very blurry)

    void Awake()
    {
        Instance = this;
        volume   = GetComponent<PostProcessVolume>();

        if (!volume.profile.TryGetSettings(out dof))
            Debug.LogError("BlurController: No Depth of Field found in Post Process Profile.");
    }

    // amount: 0 = perfectly clear, 1 = maximum blur
    public void SetBlur(float amount)
    {
        if (dof == null) return;
        float t = Mathf.Clamp01(amount);
        dof.focalLength.value  = Mathf.Lerp(10f,  300f, t);
        dof.aperture.value     = Mathf.Lerp(22f,  0.1f, t);
        dof.focusDistance.value = Mathf.Lerp(10f, 0.1f, t);
    }

    // Smoothly animate blur from one amount to another
    public IEnumerator FadeBlur(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            SetBlur(Mathf.Lerp(from, to, s));
            yield return null;
        }
        SetBlur(to);
    }
}
