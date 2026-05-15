using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FixPlayerStanding
{
    [MenuItem("Tools/Fix Player Standing", false, 0)]
    static void Fix()
    {
        // 1. Add MeshColliders to all sewer pieces in the scene
        int colliderCount = 0;
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            colliderCount += AddMeshCollidersToChildren(root.transform);
        }
        Debug.Log($"[FixStanding] Added MeshColliders to {colliderCount} sewer pieces.");

        // 2. Find or create Player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[FixStanding] No Player found. Run Tools > Setup Player Character first.");
            return;
        }

        // 3. Position player safely above the sewer floor
        player.transform.position = new Vector3(-40f, 7.5f, 155f);

        // 4. Destroy old safety floor if it exists
        GameObject oldFloor = GameObject.Find("SafetyFloor");
        if (oldFloor != null)
            GameObject.DestroyImmediate(oldFloor);

        // 5. Add a safety floor platform under the player so they always have ground
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "SafetyFloor";
        floor.transform.position = new Vector3(-40f, 6f, 155f);
        floor.transform.localScale = new Vector3(20f, 0.2f, 20f);

        // Make the floor invisible but keep collider
        Renderer floorRend = floor.GetComponent<Renderer>();
        if (floorRend != null)
        {
            floorRend.material = new Material(Shader.Find("Standard"));
            floorRend.material.color = new Color(1f, 1f, 1f, 0f);
            floorRend.enabled = false;
        }

        BoxCollider floorCol = floor.GetComponent<BoxCollider>();
        if (floorCol != null)
        {
            floorCol.enabled = true;
            floorCol.isTrigger = false;
        }

        Debug.Log("[FixStanding] Added SafetyFloor under player.");

        // 6. Create StandingUp reference under Player
        Transform existing = player.transform.Find("StandingUp");
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing.gameObject);
        }

        GameObject standing = new GameObject("StandingUp");
        standing.transform.SetParent(player.transform);
        standing.transform.localPosition = Vector3.zero;
        standing.transform.localRotation = Quaternion.identity;

        Debug.Log("[FixStanding] Created StandingUp under Player.");

        // 7. Log final state
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            Debug.Log($"[FixStanding] CharacterController height={cc.height}, radius={cc.radius}, center={cc.center}");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FixStanding] Done! SafetyFloor added — player can walk, sprint, and jump.");
    }

    static int AddMeshCollidersToChildren(Transform t)
    {
        int count = 0;
        if (t.name.ToLower().Contains("serwer") || t.name.ToLower().Contains("sewer"))
        {
            MeshFilter mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && t.GetComponent<Collider>() == null)
            {
                MeshCollider mc = t.gameObject.AddComponent<MeshCollider>();
                mc.convex = false;
                count++;
            }
        }
        for (int i = 0; i < t.childCount; i++)
        {
            count += AddMeshCollidersToChildren(t.GetChild(i));
        }
        return count;
    }
}
