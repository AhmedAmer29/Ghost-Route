using UnityEngine;

public class LeverDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("<color=yellow>[Lever-Scan] Starting Diagnostic on " + gameObject.name + "...</color>");
        
        // 1. Find the Handle
        Transform handle = transform.Find("Handle");
        if (handle == null)
        {
            Debug.LogError("[Lever-Scan] ERROR: No child named 'Handle' found! The script needs a child named exactly 'Handle'.");
            return;
        }

        // 2. Check Static Flag
        if (handle.gameObject.isStatic)
        {
            Debug.LogError("[Lever-Scan] ERROR: The Handle is marked as STATIC. It will never rotate. Uncheck 'Static' in the top-right of the Inspector!");
        }
        else
        {
            Debug.Log("<color=green>[Lever-Scan] SUCCESS: Handle is NOT static. Good.</color>");
        }

        // 3. Check for Animators
        Animator anim = handle.GetComponent<Animator>();
        if (anim == null) anim = GetComponentInParent<Animator>();
        if (anim != null && anim.enabled)
        {
            Debug.LogWarning("[Lever-Scan] WARNING: Found an Animator component. Animators usually override script rotations. If the lever doesn't move, you may need to disable the Animator or use an Animation Trigger instead.");
        }

        // 4. Check Skinned Mesh / Bones
        SkinnedMeshRenderer smr = handle.GetComponent<SkinnedMeshRenderer>();
        if (smr != null)
        {
            Debug.Log("[Lever-Scan] NOTE: Handle uses a Skinned Mesh. If rotating the Handle doesn't work, we might need to rotate the 'Root Bone' instead (" + (smr.rootBone != null ? smr.rootBone.name : "None") + ").");
        }
        
        Debug.Log("<color=yellow>[Lever-Scan] Diagnostic Complete.</color>");
    }
}
