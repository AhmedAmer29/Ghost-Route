using UnityEngine;
using UnityEditor;

public class SceneCleanup : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/REVERT SCENE TO ORIGINAL")]
    public static void ShowWindow() => GetWindow<SceneCleanup>("Scene Cleanup");

    void OnGUI()
    {
        GUILayout.Label("Scene Cleanup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This will:\n" +
            "1. DELETE all generated 3D mazes (LLM_3D, ULTIMATE_3D)\n" +
            "2. RESTORE your original SewerMaze_Hybrid cube maze\n" +
            "3. Leave everything else (Camera, Lights, etc.) untouched.",
            MessageType.Warning);

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("REVERT TO ORIGINAL CUBE MAZE", GUILayout.Height(50)))
        {
            RevertScene();
        }
        GUI.backgroundColor = Color.white;
    }

    static void RevertScene()
    {
        string[] namesToDelete = new string[] {
            "SewerMaze_LLM_3D",
            "SewerMaze_Ultimate_3D",
            "SewerMaze_ULTIMATE_3D",
            "SewerMaze_Rebaked_3D",
            "SewerMaze_Ultimate_3D(Clone)",
            "Sewers"
        };

        string[] namesToRestore = new string[] {
            "SewerMaze_Master",
            "SewerMaze_Hybrid",
            "SewerMaze_AI2",
            "SewerMaze_AI1",
        };

        // Use FindObjectsOfTypeAll so we can find INACTIVE objects too
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        int deleted = 0;
        int restored = 0;

        foreach (GameObject go in allObjects)
        {
            // Skip project assets, only touch scene objects
            if (!go.scene.isLoaded) continue;
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave) continue;

            foreach (string name in namesToDelete)
            {
                if (go.name == name)
                {
                    Debug.Log($"Deleting: {go.name}");
                    Undo.DestroyObjectImmediate(go);
                    deleted++;
                    break;
                }
            }
        }

        // Re-scan after deletes
        allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects)
        {
            if (!go.scene.isLoaded) continue;
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave) continue;

            foreach (string name in namesToRestore)
            {
                if (go.name == name)
                {
                    go.SetActive(true);
                    Debug.Log($"Restored: {go.name}");
                    restored++;
                    break;
                }
            }
        }

        Debug.Log($"DONE: Deleted {deleted} generated objects, Restored {restored} original maze object(s). Press Ctrl+Z if anything looks wrong!");
    }
}
