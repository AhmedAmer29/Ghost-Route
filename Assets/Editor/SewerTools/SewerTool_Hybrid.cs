using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SewerTool_Hybrid : EditorWindow
{
    int width = 60;
    int height = 60;
    float cellSize = 4f;
    float wallHeight = 3f;
    
    [Range(0, 1)] float organicBias = 0.4f;
    [Range(0, 1)] float branchProbability = 0.7f;
    int branchAttemptsPerCell = 5;
    int maxBranchLength = 35;

    int cisternCount = 5;
    int floodZoneRadius = 15;
    float trapDensity = 0.15f;
    bool generateCeiling = false;

    // --- TRAPS ---
    GameObject crusherPrefab;
    Vector3 crusherPosOffset = new Vector3(0, 1.5f, 0);
    Vector3 crusherRotOffset = new Vector3(-90, 0, 0); 
    
    GameObject turbinePrefab;
    Vector3 turbinePosOffset = new Vector3(0, 1.5f, 0);
    Vector3 turbineRotOffset = new Vector3(-90, 0, 0);

    [MenuItem("Tools/Sewer Tools/MASTER HYBRID: Ghost-Route")]
    public static void ShowWindow() => GetWindow<SewerTool_Hybrid>("Master Hybrid");

    void OnGUI()
    {
        GUILayout.Label("Ghost-Route Master Hybrid", EditorStyles.boldLabel);
        
        width = EditorGUILayout.IntField("Grid Size", width);
        height = width;
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        
        EditorGUILayout.Space();
        GUILayout.Label("Maze Generation", EditorStyles.label);
        organicBias = EditorGUILayout.Slider("Path Organic Bias", organicBias, 0f, 1f);
        branchProbability = EditorGUILayout.Slider("Branch Probability", branchProbability, 0f, 1f);
        branchAttemptsPerCell = EditorGUILayout.IntSlider("Attempts Per Cell", branchAttemptsPerCell, 1, 10);
        maxBranchLength = EditorGUILayout.IntSlider("Max Branch Length", maxBranchLength, 5, 100);

        EditorGUILayout.Space();
        GUILayout.Label("Friction & Lethality", EditorStyles.label);
        cisternCount = EditorGUILayout.IntSlider("Cistern Count", cisternCount, 0, 15);
        floodZoneRadius = EditorGUILayout.IntSlider("Flood Zone Size", floodZoneRadius, 5, 25);
        trapDensity = EditorGUILayout.Slider("Trap Density", trapDensity, 0f, 1f);
        generateCeiling = EditorGUILayout.Toggle("Generate Ceiling", generateCeiling);

        EditorGUILayout.Space();
        GUILayout.Label("Trap Prefabs & Alignment", EditorStyles.boldLabel);
        crusherPrefab = (GameObject)EditorGUILayout.ObjectField("Crusher Prefab", crusherPrefab, typeof(GameObject), false);
        crusherPosOffset = EditorGUILayout.Vector3Field("Crusher Pos Offset", crusherPosOffset);
        crusherRotOffset = EditorGUILayout.Vector3Field("Crusher Rot Offset", crusherRotOffset);
        
        turbinePrefab = (GameObject)EditorGUILayout.ObjectField("Turbine Prefab", turbinePrefab, typeof(GameObject), false);
        turbinePosOffset = EditorGUILayout.Vector3Field("Turbine Pos Offset", turbinePosOffset);
        turbineRotOffset = EditorGUILayout.Vector3Field("Turbine Rot Offset", turbineRotOffset);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Textured Maze", GUILayout.Height(40))) Generate();
        if (GUILayout.Button("Clear Scene")) ClearScene();
        
        EditorGUILayout.HelpBox("AUTO-TEXTURE MODE: The generator will now automatically find the textures from your downloaded SewerKit and apply them to the procedural geometry. No manual prefab assignment needed for walls!", MessageType.Info);
    }

    void ClearScene()
    {
        var existing = GameObject.Find("SewerMaze_Master");
        if (existing) DestroyImmediate(existing);
    }

    Material GetSewerMaterial(string texName, Color fallbackColor)
    {
        Material mat = new Material(Shader.Find("Standard"));
        string path = "Assets/Models/SewerKit/Sewer/Textures/" + texName;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        
        if (tex != null) {
            mat.mainTexture = tex;
            // Tweak to make it look moody and dark
            mat.color = new Color(0.7f, 0.7f, 0.7f); 
            mat.SetFloat("_Glossiness", 0.1f);
        } else {
            mat.color = fallbackColor;
        }
        return mat;
    }

    void Generate()
    {
        ClearScene();
        GameObject root = new GameObject("SewerMaze_Master");
        int[,] grid = new int[width, height]; 

        for (int i = 0; i < cisternCount; i++) {
            int rx = Random.Range(5, width-6), ry = Random.Range(5, height-6);
            for (int x = rx; x < rx+2; x++) for (int y = ry; y < ry+2; y++) grid[x, y] = 15 | 16 | 128;
        }

        Vector2Int current = new Vector2Int(width / 2, height / 2);
        Vector2Int exit = new Vector2Int(width - 5, height - 5);
        HashSet<Vector2Int> spine = new HashSet<Vector2Int>();
        int safety = 400;
        while (current != exit && safety-- > 0) {
            spine.Add(current); grid[current.x, current.y] |= 16;
            List<Vector2Int> ds = new List<Vector2Int> { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            ds = ds.OrderBy(d => Vector2Int.Distance(current+d, exit)).ToList();
            Vector2Int next = current + ((Random.value < organicBias) ? ds[0] : ds[Random.Range(0, 4)]);
            if (next.x > 0 && next.x < width - 1 && next.y > 0 && next.y < height - 1) {
                OpenWalls(grid, current, next); current = next;
            }
        }

        foreach (Vector2Int s in spine) {
            for (int i = 0; i < branchAttemptsPerCell; i++) {
                if (Random.value < branchProbability) GrowBranch(grid, s);
            }
        }

        int fx = Random.Range(width/4, 3*width/4), fy = Random.Range(height/4, 3*height/4);
        for (int x = fx - floodZoneRadius; x < fx + floodZoneRadius; x++)
            for (int y = fy - floodZoneRadius; y < fy + floodZoneRadius; y++)
                if (x >= 0 && x < width && y >= 0 && y < height && (grid[x, y] & 15) > 0) grid[x, y] |= 256;

        for (int x = 1; x < width - 1; x++) {
            for (int y = 1; y < height - 1; y++) {
                int v = grid[x, y] & 15;
                if (v == 0 || (grid[x, y] & 128) != 0) continue;
                if (Random.value < trapDensity) {
                    if (v == 5 || v == 10) grid[x, y] |= 32;
                    else if (v == 1 || v == 2 || v == 4 || v == 8) grid[x, y] |= 64;
                }
            }
        }

        BuildMasterGeometry(grid, root.transform);
    }

    void GrowBranch(int[,] grid, Vector2Int start) {
        Stack<Vector2Int> stack = new Stack<Vector2Int>(); stack.Push(start);
        int maxL = Random.Range(maxBranchLength/2, maxBranchLength), curL = 0;
        while (stack.Count > 0 && curL < maxL) {
            Vector2Int c = stack.Peek();
            var nbs = new List<Vector2Int>{c+Vector2Int.up, c+Vector2Int.right, c+Vector2Int.down, c+Vector2Int.left}
                .Where(n => n.x > 0 && n.x < width-1 && n.y > 0 && n.y < height-1 && (grid[n.x, n.y] & 16) == 0).ToList();
            if (nbs.Count > 0) {
                Vector2Int next = nbs[Random.Range(0, nbs.Count)];
                grid[next.x, next.y] |= 16; OpenWalls(grid, c, next);
                stack.Push(next); curL++;
            } else stack.Pop();
        }
    }

    void OpenWalls(int[,] grid, Vector2Int a, Vector2Int b) {
        if (b.y > a.y) { grid[a.x, a.y] |= 1; grid[b.x, b.y] |= 4; }
        if (b.x > a.x) { grid[a.x, a.y] |= 2; grid[b.x, b.y] |= 8; }
        if (b.y < a.y) { grid[a.x, a.y] |= 4; grid[b.x, b.y] |= 1; }
        if (b.x < a.x) { grid[a.x, a.y] |= 8; grid[b.x, b.y] |= 2; }
    }

    void BuildMasterGeometry(int[,] grid, Transform parent) {
        
        // AUTO-FETCH TEXTURES FROM THE IMPORTED SEWER KIT
        Material wallM = GetSewerMaterial("bricks.jpg", new Color(0.1f, 0.1f, 0.1f));
        Material floorM = GetSewerMaterial("concrete_dirty.jpg", new Color(0.2f, 0.2f, 0.2f));
        Material cisM = GetSewerMaterial("soil_mud.jpg", new Color(0.1f, 0.3f, 0.3f));
        Material floodM = GetSewerMaterial("concrete_base.png", new Color(0.1f, 0.2f, 0.5f));
        
        // Tint the flood zone blue
        floodM.color = new Color(0.2f, 0.3f, 0.6f);

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int raw = grid[x, y]; int mask = raw & 15;
                if (mask == 0) continue;
                Vector3 p = new Vector3(x * cellSize, 0, y * cellSize);

                BuildBlock(p, mask, raw, parent, cisM, floodM, floorM, wallM);

                // Spawning Traps
                if ((raw & 32) != 0) { // Crusher
                    if (crusherPrefab) {
                        Quaternion baseRot = (mask == 5) ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
                        Instantiate(crusherPrefab, p + crusherPosOffset, baseRot * Quaternion.Euler(crusherRotOffset), parent);
                    } else CreateMarker(p, Color.red, parent);
                } else if ((raw & 64) != 0) { // Turbine
                    if (turbinePrefab) {
                        float rY = (mask == 1) ? 0 : (mask == 2) ? 90 : (mask == 4) ? 180 : 270;
                        Instantiate(turbinePrefab, p + turbinePosOffset, Quaternion.Euler(0, rY, 0) * Quaternion.Euler(turbineRotOffset), parent);
                    } else CreateMarker(p, Color.blue, parent);
                }
            }
        }
    }

    void BuildBlock(Vector3 p, int mask, int raw, Transform parent, Material cisM, Material floodM, Material floorM, Material wallM) {
        GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        f.transform.position = p; f.transform.localScale = new Vector3(cellSize, 0.1f, cellSize); f.transform.parent = parent;
        f.GetComponent<Renderer>().sharedMaterial = (raw & 128) != 0 ? cisM : (raw & 256) != 0 ? floodM : floorM;
        
        if ((mask & 1) == 0) CreatePart(p + new Vector3(0, wallHeight/2f, cellSize/2), new Vector3(cellSize, wallHeight, 0.1f), parent, wallM);
        if ((mask & 2) == 0) CreatePart(p + new Vector3(cellSize/2, wallHeight/2f, 0), new Vector3(0.1f, wallHeight, cellSize), parent, wallM);
        if ((mask & 4) == 0) CreatePart(p + new Vector3(0, wallHeight/2f, -cellSize/2), new Vector3(cellSize, wallHeight, 0.1f), parent, wallM);
        if ((mask & 8) == 0) CreatePart(p + new Vector3(-cellSize/2, wallHeight/2f, 0), new Vector3(0.1f, wallHeight, cellSize), parent, wallM);
        
        if (generateCeiling) {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            c.transform.position = p + Vector3.up * wallHeight; c.transform.localScale = new Vector3(cellSize, 0.1f, cellSize); c.transform.parent = parent; c.GetComponent<Renderer>().sharedMaterial = wallM;
        }
    }

    void CreatePart(Vector3 p, Vector3 s, Transform par, Material m) {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.transform.position = p; g.transform.localScale = s; g.transform.parent = par; g.GetComponent<Renderer>().sharedMaterial = m;
    }
    void CreateMarker(Vector3 p, Color c, Transform par) {
        GameObject m = GameObject.CreatePrimitive(PrimitiveType.Sphere); m.transform.position = p+Vector3.up; m.transform.parent = par; m.GetComponent<Renderer>().sharedMaterial.color = c;
    }
}
