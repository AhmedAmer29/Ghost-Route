using UnityEngine;
using System.Collections;

// Attach this to an empty DoorPivot object placed at the hinge edge.
// The door mesh (Cylinder) should be a child of DoorPivot.
public class VaultDoor : MonoBehaviour
{
    [Header("Opening")]
    [Tooltip("Degrees to rotate. Negative = left, Positive = right.")]
    public float openAngle    = -100f;
    public float openDuration = 3f;

    bool       _open;
    bool       _animating;
    Collider[] _colliders;

    public bool IsOpen => _open;

    void Start()
    {
        _colliders = GetComponentsInChildren<Collider>(true);
    }

    [ContextMenu("Test Open")]
    public void Open()
    {
        if (_open || _animating) return;
        StartCoroutine(OpenRoutine());
    }

    [ContextMenu("Reset Door")]
    public void ResetDoor()
    {
        StopAllCoroutines();
        _open      = false;
        _animating = false;
        if (_colliders != null)
            foreach (Collider col in _colliders) col.enabled = true;
    }

    IEnumerator OpenRoutine()
    {
        _animating = true;
        foreach (Collider col in _colliders) col.enabled = false;

        Quaternion startRot  = transform.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(0f, openAngle, 0f);

        float elapsed = 0f;
        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.localRotation = targetRot;

        foreach (Collider col in _colliders) col.enabled = true;
        _open      = true;
        _animating = false;
    }
}
