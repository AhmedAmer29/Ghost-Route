using UnityEngine;
using UnityEditor;

public class SewerBridgeTool : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/1. BRIDGE THE GAP (SEAL HOLES)")]
    public static void Bridge()
    {
        GameObject[] selected = Selection.gameObjects;
        if (selected.Length != 2) {
            Debug.LogError("Select EXACTLY 2 objects: The Entrance Marker and the Chamber.");
            return;
        }

        Vector3 posA = selected[0].transform.position;
        Vector3 posB = selected[1].transform.position;
        Vector3 midPoint = (posA + posB) / 2f;
        float distance = Vector3.Distance(posA, posB);

        GameObject bridge = new GameObject("Sewer_Bridge_Link");
        bridge.transform.position = midPoint;
        bridge.transform.LookAt(posA);

        // Create 4 slabs to "Seal" the rounded gap
        // We make them slightly larger than the arch to ensure they overlap the corners
        CreateSlab("Bridge_Top",    new Vector3(0, 3.5f, 0),  new Vector3(8, 1, distance), bridge.transform);
        CreateSlab("Bridge_Bottom", new Vector3(0, -3.5f, 0), new Vector3(8, 1, distance), bridge.transform);
        CreateSlab("Bridge_Left",   new Vector3(-4f, 0, 0),   new Vector3(1, 8, distance), bridge.transform);
        CreateSlab("Bridge_Right",  new Vector3(4f, 0, 0),    new Vector3(1, 8, distance), bridge.transform);

        Undo.RegisterCreatedObjectUndo(bridge, "Bridge Gap");
        Selection.activeGameObject = bridge;
        Debug.Log("[SewerBridgeTool] Bridge created! Scale the slabs to fit your archway.");
    }

    static void CreateSlab(string name, Vector3 localPos, Vector3 scale, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        
        // Try to find the wet material
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Sewer/Sewer_Wet_Wall.mat");
        if (mat != null) go.GetComponent<Renderer>().material = mat;
    }
}
