using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Place on the DreamZone root GameObject alongside a BoxCollider (isTrigger=true).
///
/// What it controls on enter/exit:
///   1. DreamZoneImageEffect.BlurStrength  — full-screen Gaussian blur (0→1)
///   2. PostProcessVolume.weight           — dream post-fx profile (bloom, color, DOF)
///   3. ParticleSystem                     — wispy cloud particles
///   4. Optional ambient audio fade
///
/// Requires the Player GameObject to have the tag "Player".
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DreamZoneTrigger : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Transition")]
    [Tooltip("Seconds to blend fully in or out.")]
    [SerializeField] private float transitionDuration = 1.8f;

    [Header("Post Processing")]
    [Tooltip("The LOCAL PostProcessVolume on this GameObject (or a child). " +
             "Set its profile to the Dream profile with DOF, Bloom, Color Grading, Vignette.")]
    [SerializeField] private PostProcessVolume dreamVolume;

    [Header("Particles")]
    [Tooltip("The DreamClouds Particle System (child of this object).")]
    [SerializeField] private ParticleSystem dreamClouds;

    [Header("Optional Audio")]
    [Tooltip("AudioSource that plays ambient dream audio. Volume is faded in/out.")]
    [SerializeField] private AudioSource dreamAudio;
    [SerializeField] [Range(0f, 1f)] private float dreamAudioMaxVolume = 0.4f;

    // ── Runtime ──────────────────────────────────────────────────────────────
    private Coroutine _activeTransition;
    private bool      _playerInside;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // Ensure box collider is a trigger
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;

        // Start fully inactive
        DreamZoneImageEffect.BlurStrength = 0f;
        if (dreamVolume != null) dreamVolume.weight = 0f;
        if (dreamAudio  != null) { dreamAudio.volume = 0f; dreamAudio.Stop(); }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || _playerInside) return;
        _playerInside = true;

        RestartTransition(true);

        if (dreamClouds != null && !dreamClouds.isPlaying)
            dreamClouds.Play();

        if (dreamAudio != null && !dreamAudio.isPlaying)
        {
            dreamAudio.volume = 0f;
            dreamAudio.Play();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player") || !_playerInside) return;
        _playerInside = false;

        RestartTransition(false);
    }

    // ── Transition helpers ────────────────────────────────────────────────────

    private void RestartTransition(bool entering)
    {
        if (_activeTransition != null) StopCoroutine(_activeTransition);
        _activeTransition = StartCoroutine(DoTransition(entering));
    }

    private IEnumerator DoTransition(bool entering)
    {
        float startBlur   = DreamZoneImageEffect.BlurStrength;
        float startVol    = dreamVolume != null ? dreamVolume.weight : 0f;
        float startAudio  = dreamAudio  != null ? dreamAudio.volume  : 0f;

        float targetBlur  = entering ? 1f : 0f;
        float targetVol   = entering ? 1f : 0f;
        float targetAudio = entering ? dreamAudioMaxVolume : 0f;

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            DreamZoneImageEffect.BlurStrength = Mathf.Lerp(startBlur,  targetBlur,  t);

            if (dreamVolume != null)
                dreamVolume.weight = Mathf.Lerp(startVol, targetVol, t);

            if (dreamAudio != null)
                dreamAudio.volume  = Mathf.Lerp(startAudio, targetAudio, t);

            yield return null;
        }

        // Snap to final values
        DreamZoneImageEffect.BlurStrength = targetBlur;
        if (dreamVolume != null) dreamVolume.weight = targetVol;
        if (dreamAudio  != null) dreamAudio.volume  = targetAudio;

        // Clouds keep playing — they form the always-visible fog wall.
        // Only audio fades fully out.
        if (!entering && dreamAudio != null)
        {
            dreamAudio.Stop();
        }
    }

#if UNITY_EDITOR
    // Draw the zone gizmo in the Scene view
    void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.25f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.85f);
        Gizmos.DrawWireCube(col.center, col.size);
    }
#endif
}
