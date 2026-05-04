using UnityEngine;
using UnityEditor;

public class SceneRotationScanner : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/DEBUG: Scan Scene Rotations")]
    public static void Scan()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        Debug.Log($"--- SCANNING {allObjects.Length} OBJECTS ---");
        
        foreach (GameObject go in allObjects)
        {
            if (go.transform.parent != null && go.transform.parent.name.Contains("SewerMaze"))
            {
                Debug.Log($"Object: {go.name} | Local Rotation: {go.transform.localEulerAngles} | World Rotation: {go.transform.eulerAngles}");
            }
        }
        Debug.Log("--- SCAN COMPLETE ---");
    }
}
