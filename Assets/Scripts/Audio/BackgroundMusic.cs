using UnityEngine;

/// <summary>
/// Attach to any GameObject to play background music on a continuous loop.
/// Survives scene transitions (DontDestroyOnLoad) so the music keeps playing
/// seamlessly when you load a new scene — only one instance ever exists.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    [Header("Music")]
    public AudioClip clip;

    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Range(0.1f, 3f)]
    public float pitch = 1f;

    [Tooltip("Keep music playing across scene loads. Disable if you want per-scene music.")]
    public bool persistAcrossScenes = true;

    // ─────────────────────────────────────────────────────────────────────
    private static BackgroundMusic _instance;
    private AudioSource _source;

    void Awake()
    {
        // Singleton — destroy duplicate if one already exists from a previous scene
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        _source              = GetComponent<AudioSource>();
        _source.clip         = clip;
        _source.volume       = volume;
        _source.pitch        = pitch;
        _source.loop         = true;
        _source.playOnAwake  = false;
        _source.spatialBlend = 0f; // 2D — not affected by position

        if (clip != null)
            _source.Play();
        else
            Debug.LogWarning("[BackgroundMusic] No AudioClip assigned — assign one in the Inspector.");
    }

    void OnValidate()
    {
        // Live-update volume and pitch while tweaking in the Inspector during Play Mode
        if (_source == null) _source = GetComponent<AudioSource>();
        if (_source == null) return;

        _source.volume = volume;
        _source.pitch  = pitch;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// Fade volume to a target level over a given duration (in seconds).
    public void FadeTo(float targetVolume, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(FadeRoutine(targetVolume, duration));
    }

    /// Swap the clip and restart immediately.
    public void SwapClip(AudioClip newClip)
    {
        if (newClip == null) return;
        clip         = newClip;
        _source.clip = newClip;
        _source.Stop();
        _source.Play();
    }

    System.Collections.IEnumerator FadeRoutine(float target, float duration)
    {
        float start   = _source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed        += Time.deltaTime;
            _source.volume  = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        _source.volume = target;

        if (target <= 0f)
            _source.Pause();
    }
}
