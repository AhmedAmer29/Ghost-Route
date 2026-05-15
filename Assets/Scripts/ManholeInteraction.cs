using System.Collections;
using UnityEngine;

// Attach this to the ManholeSidewalkTile root (or any parent of the cover).
// Requires a SphereCollider with isTrigger = true for proximity detection.
// On E press while the player is in range, the cover lifts straight up then
// slides sideways onto the sidewalk. Press E again to reverse.
[RequireComponent(typeof(SphereCollider))]
public class ManholeInteraction : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The manhole cover that animates. If empty, auto-finds a child named 'ManholeCover'.")]
    public Transform manholeCover;

    [Header("Open Animation")]
    [Tooltip("How high the cover lifts straight up before sliding (meters, world up).")]
    public float liftHeight = 0.15f;

    [Tooltip("How far the cover slides sideways after lifting (meters).")]
    public float slideDistance = 1.1f;

    [Tooltip("Direction the cover slides, in WORLD space. Will be projected onto the horizontal plane and normalized.")]
    public Vector3 slideDirection = new Vector3(0f, 0f, 1f);

    [Tooltip("Total time for the open animation (seconds).")]
    public float openDuration = 1.4f;

    [Tooltip("Total time for the close animation (seconds).")]
    public float closeDuration = 1.2f;

    [Tooltip("Animation easing.")]
    public AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Interaction")]
    [Tooltip("Key to press to open/close.")]
    public KeyCode interactKey = KeyCode.E;

    [Tooltip("Tag on the player GameObject. Default 'Player'.")]
    public string playerTag = "Player";

    [Tooltip("Optional UI prompt shown when the player is in range.")]
    public GameObject interactPrompt;

    // --- runtime state ---
    bool playerInRange = false;
    bool isAnimating   = false;
    bool isOpen        = false;

    /// <summary>True while the cover is in its open (lifted/slid) state.</summary>
    public bool IsOpen => isOpen;

    Vector3 closedWorldPos;
    Quaternion closedWorldRot;

    void Awake()
    {
        // Auto-find cover if not assigned
        if (manholeCover == null)
        {
            manholeCover = transform.Find("ManholeCover");
            if (manholeCover == null)
            {
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "ManholeCover") { manholeCover = t; break; }
                }
            }
        }

        if (manholeCover != null)
        {
            closedWorldPos = manholeCover.position;
            closedWorldRot = manholeCover.rotation;
        }
        else
        {
            Debug.LogWarning("[ManholeInteraction] No ManholeCover found. Assign it in the inspector.");
        }

        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
        if (col.radius < 1f) col.radius = 2.5f;

        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = true;
        if (interactPrompt != null) interactPrompt.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        playerInRange = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    bool IsPlayer(Collider other)
    {
        if (other == null) return false;
        if (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) return true;
        var root = other.transform.root;
        if (root != null && root.name.ToLower().Contains("player")) return true;
        return false;
    }

    void Update()
    {
        if (!playerInRange || isAnimating || manholeCover == null) return;
        if (Input.GetKeyDown(interactKey))
        {
            StartCoroutine(Animate(isOpen ? CloseSequence() : OpenSequence()));
        }
    }

    IEnumerator Animate(IEnumerator sequence)
    {
        isAnimating = true;
        yield return StartCoroutine(sequence);
        isAnimating = false;
        isOpen = !isOpen;
    }

    Vector3 GetHorizontalSlide()
    {
        Vector3 dir = slideDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward; // safe fallback
        return dir.normalized * slideDistance;
    }

    // Open: lift straight up (0% -> 50% of duration), then slide sideways (40% -> 100%)
    // The overlap (40-50%) makes the motion feel natural rather than two stiff phases.
    IEnumerator OpenSequence()
    {
        Vector3 startPos = closedWorldPos;
        Vector3 liftedPos = startPos + Vector3.up * liftHeight;
        Vector3 finalPos  = liftedPos + GetHorizontalSlide();

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / openDuration);

            float liftP  = easing.Evaluate(Mathf.Clamp01(p / 0.5f));         // 0..0.5 of total
            float slideP = easing.Evaluate(Mathf.Clamp01((p - 0.4f) / 0.6f)); // 0.4..1.0 of total

            Vector3 pos = Vector3.Lerp(startPos, liftedPos, liftP);
            pos += GetHorizontalSlide() * slideP;
            manholeCover.position = pos;
            manholeCover.rotation = closedWorldRot; // no tilt
            yield return null;
        }
        manholeCover.position = finalPos;
        manholeCover.rotation = closedWorldRot;
    }

    // Close: slide back (0% -> 60%), then drop straight down (50% -> 100%)
    IEnumerator CloseSequence()
    {
        Vector3 startPos = manholeCover.position;
        Vector3 liftedPos = closedWorldPos + Vector3.up * liftHeight;

        float t = 0f;
        while (t < closeDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / closeDuration);

            float slideP = easing.Evaluate(Mathf.Clamp01(p / 0.6f));         // 0..0.6
            float dropP  = easing.Evaluate(Mathf.Clamp01((p - 0.5f) / 0.5f)); // 0.5..1.0

            Vector3 pos = Vector3.Lerp(startPos, liftedPos, slideP);
            pos = Vector3.Lerp(pos, closedWorldPos, dropP);
            manholeCover.position = pos;
            manholeCover.rotation = closedWorldRot;
            yield return null;
        }
        manholeCover.position = closedWorldPos;
        manholeCover.rotation = closedWorldRot;
    }

    void OnDrawGizmosSelected()
    {
        var col = GetComponent<SphereCollider>();
        if (col != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.6f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(col.center, col.radius);
        }

        // Show slide preview
        if (manholeCover != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 start = Application.isPlaying ? closedWorldPos : manholeCover.position;
            Vector3 lifted = start + Vector3.up * liftHeight;
            Vector3 dir = slideDirection;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) dir = dir.normalized;
            Vector3 end = lifted + dir * slideDistance;
            Gizmos.DrawLine(start, lifted);
            Gizmos.DrawLine(lifted, end);
            Gizmos.DrawSphere(end, 0.05f);
        }
    }
}
