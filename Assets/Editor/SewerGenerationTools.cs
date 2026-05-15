using UnityEngine;
using UnityEditor;

public class SewerGenerationTools : EditorWindow
{
    // --- FULL HALLWAY ---
    [MenuItem("Tools/Ghost-Route/Create Full Sewer Hallway")]
    public static void CreateHallway()
    {
        // 1. Get/Create Materials
        Material wallMat = GetOrCreateMaterial("Assets/Materials/Sewer/SewerWall_Mat.mat", "Assets/Materials/Sewer/SewerWall_Albedo.png");
        Material floorMat = GetOrCreateMaterial("Assets/Materials/Sewer/SewerFloor_Mat.mat", "Assets/Materials/Sewer/SewerFloor_Albedo.png");
        Material ceilingMat = GetOrCreateMaterial("Assets/Materials/Sewer/SewerCeiling_Mat.mat", "Assets/Materials/Sewer/SewerCeiling_Albedo.png");

        GameObject hallRoot = new GameObject("Sewer_Hallway_Section");
        Undo.RegisterCreatedObjectUndo(hallRoot, "Create Sewer Hallway");

        float width = 4f;
        float height = 3f;
        float length = 4f;

        // Apply distinct materials
        CreatePart("Floor", hallRoot.transform, new Vector3(0, 0, 0), new Vector3(width, 0.2f, length), floorMat);
        CreatePart("Ceiling", hallRoot.transform, new Vector3(0, height, 0), new Vector3(width, 0.2f, length), ceilingMat);
        
        CreateWallWithTrims("Wall_Left", hallRoot.transform, new Vector3(-width/2, height/2, 0), true, wallMat);
        CreateWallWithTrims("Wall_Right", hallRoot.transform, new Vector3(width/2, height/2, 0), false, wallMat);

        Selection.activeGameObject = hallRoot;
    }

    [MenuItem("Tools/Ghost-Route/Create Sewer Wall Segment")]
    public static void CreateWall()
    {
        Material wallMat = GetOrCreateMaterial("Assets/Materials/Sewer/SewerWall_Mat.mat", "Assets/Materials/Sewer/SewerWall_Albedo.png");
        GameObject wallGroup = new GameObject("Sewer_Wall_Segment");
        Undo.RegisterCreatedObjectUndo(wallGroup, "Create Sewer Wall");
        CreateWallWithTrims("Main_Wall", wallGroup.transform, Vector3.zero, true, wallMat);
        Selection.activeGameObject = wallGroup;
    }

    // --- ANIMATED ROPE ---
    [MenuItem("Tools/Ghost-Route/Create Animated Rope")]
    public static void CreateRope()
    {
        GameObject ropeObj = new GameObject("Animated_Rope");
        Undo.RegisterCreatedObjectUndo(ropeObj, "Create Animated Rope");

        LineRenderer lr = ropeObj.AddComponent<LineRenderer>();
        ProceduralRope rope = ropeObj.AddComponent<ProceduralRope>();

        // Create Default Points
        GameObject start = new GameObject("Rope_Start");
        start.transform.SetParent(ropeObj.transform);
        start.transform.position = new Vector3(0, 2, 0);

        GameObject end = new GameObject("Rope_End");
        end.transform.SetParent(ropeObj.transform);
        end.transform.position = new Vector3(0, 2, 6);

        rope.startPoint = start.transform;
        rope.endPoint = end.transform;

        Selection.activeGameObject = ropeObj;
    }

    // --- HELPER METHODS ---
    static Material GetOrCreateMaterial(string matPath, string texPath)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.mainTexture = tex;
                mat.SetFloat("_Glossiness", 0.6f);
                mat.SetFloat("_Metallic", 0.1f);
                
                if (!AssetDatabase.IsValidFolder("Assets/Materials/Sewer"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
                    AssetDatabase.CreateFolder("Assets/Materials", "Sewer");
                }
                AssetDatabase.CreateAsset(mat, matPath);
            }
            else
            {
                mat = new Material(Shader.Find("Standard"));
            }
        }
        return mat;
    }

    static void CreateWallWithTrims(string name, Transform parent, Vector3 pos, bool isLeft, Material mat)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        group.transform.localPosition = pos;

        GameObject main = GameObject.CreatePrimitive(PrimitiveType.Cube);
        main.name = "Main_Wall_Box";
        main.transform.SetParent(group.transform);
        main.transform.localPosition = Vector3.zero;
        main.transform.localScale = new Vector3(0.2f, 3f, 4f);
        main.GetComponent<Renderer>().material = mat;

        float xOffset = isLeft ? 0.25f : -0.25f;
        CreatePart("Trim_Top", group.transform, new Vector3(xOffset, 1.2f, 0), new Vector3(0.4f, 0.4f, 4f), mat);
        CreatePart("Trim_Bottom", group.transform, new Vector3(xOffset, -1.2f, 0), new Vector3(0.4f, 0.4f, 4f), mat);
    }

    static void CreatePart(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material = mat;
    }
}
