using UnityEditor;
using UnityEngine;

public static class FixNYCWallColliders
{
    [MenuItem("Tools/Fix NYC Wall Colliders")]
    public static void Fix()
    {
        int wallsFixed = 0;
        int collidersDisabled = 0;
        int meshCollidersDisabled = 0;

        var roots = GameObject.FindObjectsOfType<Transform>(true);
        foreach (var t in roots)
        {
            if (!t.name.StartsWith("nyc_1960_red_brick_wall_only")) continue;
            wallsFixed++;

            var parentMC = t.GetComponent<MeshCollider>();
            if (parentMC != null) { parentMC.enabled = false; meshCollidersDisabled++; }

            foreach (var child in t.GetComponentsInChildren<Collider>(true))
            {
                if (child.transform == t) continue;
                if (child.enabled) { child.enabled = false; collidersDisabled++; }
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

        Debug.Log($"[FixNYCWallColliders] Walls processed: {wallsFixed}, child colliders disabled: {collidersDisabled}, parent MeshColliders disabled: {meshCollidersDisabled}");
    }
}
