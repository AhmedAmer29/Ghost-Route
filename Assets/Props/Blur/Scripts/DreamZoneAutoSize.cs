using UnityEngine;

/// <summary>
/// Attach to DreamZone. The BoxCollider and clouds will always match
/// the GameObject's scale. Just use the Scale tool (R) to resize the zone visually.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(BoxCollider))]
public class DreamZoneAutoSize : MonoBehaviour
{
    void Update()
    {
        var col  = GetComponent<BoxCollider>();
        col.size   = Vector3.one;
        col.center = new Vector3(0f, 0.5f, 0f);

        // Sync particle system shape to match
        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            var shape = ps.shape;
            shape.scale = transform.localScale;
        }
    }
}
