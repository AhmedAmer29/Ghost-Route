using UnityEngine;
using UnityEditor;

public class RoomGenerator : EditorWindow
{
    public float width = 7f;
    public float length = 7f;
    public float height = 4f;
    public Material wallMaterial;

    [MenuItem("Tools/Sewer Tools/GENERATE NESTING CHAMBER")]
    public static void ShowWindow()
    {
        GetWindow<RoomGenerator>("Room Gen");
    }

    void OnGUI()
    {
        GUILayout.Label("Chamber Dimensions (Meters)", EditorStyles.boldLabel);
        width = EditorGUILayout.FloatField("Width (X)", width);
        length = EditorGUILayout.FloatField("Length (Z)", length);
        height = EditorGUILayout.FloatField("Height (Y)", height);
        wallMaterial = (Material)EditorGUILayout.ObjectField("Material", wallMaterial, typeof(Material), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate 7x7 Chamber"))
        {
            CreateHall();
        }
        
        EditorGUILayout.HelpBox("Note: 1 Unit = 1 Meter. A 7x7 room is about the size of a large bedroom. For a 'Huge Hall', consider 20x20 or larger!", MessageType.Info);
    }

    void CreateHall()
    {
        GameObject hallRoot = new GameObject($"NestingChamber_{width}x{length}");
        
        // Floor
        CreatePlane("Floor", new Vector3(0, -0.05f, 0), new Vector3(width, 0.1f, length), hallRoot.transform);
        // Ceiling
        CreatePlane("Ceiling", new Vector3(0, height + 0.05f, 0), new Vector3(width, 0.1f, length), hallRoot.transform);
        // Walls
        CreatePlane("Wall_North", new Vector3(0, height/2, length/2), new Vector3(width, height, 0.1f), hallRoot.transform);
        CreatePlane("Wall_South", new Vector3(0, height/2, -length/2), new Vector3(width, height, 0.1f), hallRoot.transform);
        CreatePlane("Wall_East", new Vector3(width/2, height/2, 0), new Vector3(0.1f, height, length), hallRoot.transform);
        CreatePlane("Wall_West", new Vector3(-width/2, height/2, 0), new Vector3(0.1f, height, length), hallRoot.transform);

        Selection.activeGameObject = hallRoot;
        Undo.RegisterCreatedObjectUndo(hallRoot, "Generate Chamber");
        Debug.Log($"[RoomGenerator] Created {width}x{length} chamber.");
    }

    void CreatePlane(string name, Vector3 pos, Vector3 scale, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        if (wallMaterial != null) go.GetComponent<Renderer>().material = wallMaterial;
    }
}
