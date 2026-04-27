using UnityEngine;

/// <summary>
/// Attach to the criminal character root.
/// Puts the single character mesh on the "PlayerBody" layer so PlayerCamera's
/// dual-camera system can clip away the torso interior when looking down,
/// while still showing legs and arms further out.
///
/// SETUP:
///  1. Edit > Project Settings > Tags and Layers > add "PlayerBody" to any free User Layer
///  2. Add this script to the criminal root GameObject
///  3. Assign characterRoot → the Transform that parents the character mesh
/// </summary>
public class FPSBodyVisibility : MonoBehaviour
{
    [Tooltip("The root of the character mesh (the single mesh with the whole body).")]
    public Transform characterRoot;

    void Awake()
    {
        int bodyLayer = LayerMask.NameToLayer("PlayerBody");
        if (bodyLayer == -1)
        {
            Debug.LogWarning("[FPSBodyVisibility] 'PlayerBody' layer missing. " +
                             "Add it in Edit > Project Settings > Tags and Layers.");
            return;
        }

        Transform root = characterRoot != null ? characterRoot : transform;

        // Move the whole mesh to PlayerBody layer
        SetLayerRecursive(root, bodyLayer);

        // Re-enable the renderer — PlayerCamera handles hiding the interior
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.enabled = true;
    }

    static void SetLayerRecursive(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        foreach (Transform child in t)
            SetLayerRecursive(child, layer);
    }
}
