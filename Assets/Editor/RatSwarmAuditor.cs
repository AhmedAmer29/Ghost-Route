using UnityEngine;
using UnityEditor;

public class RatSwarmAuditor : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/AUDIT RAT SWARM POSITION")]
    public static void Audit()
    {
        RatSwarm swarm = GameObject.FindObjectOfType<RatSwarm>();
        if (swarm == null)
        {
            Debug.LogError("[Auditor] RatSwarm script NOT FOUND in scene!");
            return;
        }

        GameObject player = GameObject.Find("Player"); // Adjust if name is different
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        Debug.Log("--- RAT SWARM AUDIT ---");
        Debug.Log($"Object: {swarm.gameObject.name}");
        Debug.Log($"Position: {swarm.transform.position}");
        
        if (player != null)
        {
            float dist = Vector3.Distance(swarm.transform.position, player.transform.position);
            Debug.Log($"Distance to Player: {dist}m");
            Debug.Log($"Player Position: {player.transform.position}");
        }

        BoxCollider box = swarm.GetComponent<BoxCollider>();
        if (box == null)
        {
            Debug.LogError("[Auditor] No BoxCollider found on RatSwarm!");
        }
        else
        {
            Debug.Log($"Box Size: {box.size}");
            Debug.Log($"Is Trigger: {box.isTrigger}");
            
            // Check if player is inside the bounds mathematically
            if (player != null)
            {
                Bounds b = box.bounds;
                if (b.Contains(player.transform.position))
                    Debug.Log("<color=green>[Auditor] Player is INSIDE the trigger bounds!</color>");
                else
                    Debug.Log("<color=red>[Auditor] Player is OUTSIDE the trigger bounds.</color>");
            }
        }

        Debug.Log($"Layer: {LayerMask.LayerToName(swarm.gameObject.layer)}");
        Debug.Log("-----------------------");
    }
}
