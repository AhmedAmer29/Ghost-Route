using UnityEngine;
using System.Collections;

public class ElectricalBoxDoor : MonoBehaviour
{
    [Header("Animation Settings")]
    public Vector3 openRotation = new Vector3(0, -110, 0); // Angles to open
    public float openSpeed = 2.5f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Quaternion _closedRot;
    private Quaternion _openRot;
    private bool _isOpening;

    void Start()
    {
        _closedRot = transform.localRotation;
        _openRot = _closedRot * Quaternion.Euler(openRotation);
    }

    [ContextMenu("Open Door")]
    public void Open()
    {
        if (_isOpening) return;
        StartCoroutine(OpenSequence());
    }

    IEnumerator OpenSequence()
    {
        _isOpening = true;
        float elapsed = 0f;

        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime * openSpeed;
            float t = openCurve.Evaluate(elapsed);
            transform.localRotation = Quaternion.Slerp(_closedRot, _openRot, t);
            yield return null;
        }

        Debug.Log($"[ElectricalBoxDoor] {gameObject.name} is now OPEN.");
    }
}
