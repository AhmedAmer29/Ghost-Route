using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PressurePlateRevealSetup
{
    static PressurePlateRevealSetup()
    {
        EditorSceneManager.sceneOpened += (_, __) => Run();
        EditorApplication.delayCall   += Run;
    }

    [MenuItem("Tools/Setup Pressure Plate Reveals")]
    public static void Run()
    {
        int added = 0;

        foreach (GameObject go in Object.FindObjectsOfType<GameObject>())
        {
            if (!go.name.Contains("Pressure Plate")) continue;

            // Disable any leftover lights
            foreach (Light l in go.GetComponentsInChildren<Light>(true))
            {
                if (!l.enabled) continue;
                Undo.RecordObject(l, "Disable plate light");
                l.enabled = false;
            }

            if (go.GetComponent<PressurePlateReveal>() != null) continue;

            Undo.AddComponent<PressurePlateReveal>(go);
            added++;
        }

        if (added > 0)
        {
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[PressurePlateRevealSetup] Added PressurePlateReveal to {added} plates.");
        }
    }
}
