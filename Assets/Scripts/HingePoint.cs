using UnityEngine;

/// <summary>
/// Attach to a rotating part (e.g. light_1.003_low.001).
/// Shows a yellow dot in the Scene view at the hinge/pivot position,
/// exactly like the door, so you can verify the part rotates around
/// the correct connection point on the device.
///
/// HOW TO USE:
///   1. Attach this script to light_1.003_low.001.
///   2. Select the object in the Scene view — the yellow dot appears.
///   3. Drag the Hinge Local Offset handles until the dot sits exactly
///      at the joint where the part connects to the rest of the device.
///   4. Rotate the object in the Inspector — it will visually pivot
///      around that yellow dot.
/// </summary>
public class HingePoint : MonoBehaviour
{
    [Tooltip("Local-space offset from this object's origin to the actual hinge/joint point.\n" +
             "Drag until the yellow dot sits exactly at the connection point on the device.")]
    public Vector3 hingeLocalOffset = Vector3.zero;

    [Tooltip("Which local axis this part rotates around. Y = spin on vertical axis, X = nod up/down, Z = tilt sideways.")]
    public Vector3 hingeAxis = Vector3.up;

    void OnDrawGizmosSelected()
    {
        Vector3 hingeWorld = transform.position + transform.rotation * hingeLocalOffset;

        // Yellow sphere — same as the door hinge dot
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hingeWorld, 0.04f);

        // Cyan line showing the rotation axis
        Gizmos.color = Color.cyan;
        Vector3 axisWorld = transform.rotation * hingeAxis.normalized;
        Gizmos.DrawRay(hingeWorld - axisWorld * 0.3f, axisWorld * 0.6f);
    }
}
