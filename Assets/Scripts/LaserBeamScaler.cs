using UnityEngine;

/// <summary>
/// Scales the laser beam along one axis only, leaving its parent (the metal device) untouched.
/// Attach this to the laser beam child GameObject.
/// </summary>
public class LaserBeamScaler : MonoBehaviour
{
    public enum ScaleAxis { X, Y, Z }

    [Tooltip("Axis the laser beam runs along in local space.")]
    public ScaleAxis axis = ScaleAxis.Z;

    [Tooltip("Length of the laser beam in world units.")]
    [Min(0f)]
    public float length = 1f;

    void OnValidate() => Apply();
    void Start()     => Apply();

    void Apply()
    {
        Vector3 s = transform.localScale;
        switch (axis)
        {
            case ScaleAxis.X: s.x = length; break;
            case ScaleAxis.Y: s.y = length; break;
            case ScaleAxis.Z: s.z = length; break;
        }
        transform.localScale = s;
    }
}
