using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SewerTexturizer : EditorWindow
{
    public GameObject modDeadEnd;
    public GameObject modStraight;
    public GameObject modCorner;
    public GameObject modTJunction;
    public GameObject modCross;

    [MenuItem("Tools/Sewer Tools/UPGRADE EXISTING MAZE TO 3D PIPES")]
    public static void ShowWindow() => GetWindow<SewerTexturizer>("Upgrade Maze");

    void OnGUI()
    {
        GUILayout.Label("1. Auto-Analyze Extracted 3D Kit", EditorStyles.boldLabel);
        if (GUILayout.Button("Analyze Topology & Assign Models", GUILayout.Height(30))) {
            AutoAnalyzeKit();
        }

        EditorGUILayout.Space();
        GUILayout.Label("Detected Pieces (Adjust manually if needed):", EditorStyles.label);
        modDeadEnd = (GameObject)EditorGUILayout.ObjectField("Dead End (1-Way)", modDeadEnd, typeof(GameObject), false);
        modStraight = (GameObject)EditorGUILayout.ObjectField("Straight (2-Way)", modStraight, typeof(GameObject), false);
        modCorner = (GameObject)EditorGUILayout.ObjectField("Corner (L-Turn)", modCorner, typeof(GameObject), false);
        modTJunction = (GameObject)EditorGUILayout.ObjectField("T-Junction (3-Way)", modTJunction, typeof(GameObject), false);
        modCross = (GameObject)EditorGUILayout.ObjectField("Crossroads (4-Way)", modCross, typeof(GameObject), false);

        EditorGUILayout.Space();
        GUILayout.Label("2. Upgrade Scene", EditorStyles.boldLabel);
        if (GUILayout.Button("Re-Bake Perfect Maze to 3D!", GUILayout.Height(40))) {
            RebakeMaze();
        }
        
        EditorGUILayout.HelpBox("This uses a reverse-engineering algorithm. It scans your existing cube maze, figures out every corner and intersection, and physically replaces the cubes with your new 3D pipes while perfectly preserving your layout!", MessageType.Info);
    }

    void AutoAnalyzeKit()
    {
        string folderPath = "Assets/Models/SewerKit/ExtractedPipes";
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            MeshFilter mf = prefab.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            Mesh mesh = mf.sharedMesh;
            Bounds b = mesh.bounds;
            
            bool openN = false, openS = false, openE = false, openW = false;
            float threshold = 0.6f;

            foreach (Vector3 v in mesh.vertices)
            {
                if (v.y > b.min.y + (b.size.y * 0.35f)) continue; // Only look near the floor

                if (Mathf.Abs(v.z - b.max.z) < threshold) openN = true;
                if (Mathf.Abs(v.z - b.min.z) < threshold) openS = true;
                if (Mathf.Abs(v.x - b.max.x) < threshold) openE = true;
                if (Mathf.Abs(v.x - b.min.x) < threshold) openW = true;
            }

            int count = (openN?1:0) + (openS?1:0) + (openE?1:0) + (openW?1:0);

            if (count == 4 && modCross == null) modCross = prefab;
            else if (count == 3 && modTJunction == null) modTJunction = prefab;
            else if (count == 2) {
                if ((openN && openS) || (openE && openW)) {
                    if (modStraight == null) modStraight = prefab;
                } else {
                    if (modCorner == null) modCorner = prefab;
                }
            }
            else if (count == 1 && modDeadEnd == null) modDeadEnd = prefab;
        }
        
        Debug.Log("Topology Analysis Complete! I have guessed the best models based on their vertex structures.");
    }

    void RebakeMaze()
    {
        GameObject maze = GameObject.Find("SewerMaze_Master");
        if (maze == null) maze = GameObject.Find("SewerMaze_Hybrid");
        if (maze == null) { Debug.LogError("Could not find generated maze!"); return; }

        Dictionary<Vector2Int, GameObject> floorTiles = new Dictionary<Vector2Int, GameObject>();
        float cellSize = 4f;

        // Step 1: Scan layout and build grid
        foreach (Transform child in maze.transform) {
            if (Mathf.Approximately(child.localScale.y, 0.1f) && child.position.y < 0.5f) {
                int gridX = Mathf.RoundToInt(child.position.x / cellSize);
                int gridZ = Mathf.RoundToInt(child.position.z / cellSize);
                Vector2Int pos = new Vector2Int(gridX, gridZ);
                if (!floorTiles.ContainsKey(pos)) floorTiles.Add(pos, child.gameObject);
            }
        }

        // Step 2: Reverse-engineer the bitmask
        GameObject rebakedRoot = new GameObject("SewerMaze_Rebaked_3D");
        int replacedCount = 0;

        foreach (var kvp in floorTiles) {
            Vector2Int pos = kvp.Key;
            Vector3 worldPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            
            bool hasN = floorTiles.ContainsKey(pos + Vector2Int.up);
            bool hasE = floorTiles.ContainsKey(pos + Vector2Int.right);
            bool hasS = floorTiles.ContainsKey(pos + Vector2Int.down);
            bool hasW = floorTiles.ContainsKey(pos + Vector2Int.left);

            int mask = 0;
            if (hasN) mask |= 1;
            if (hasE) mask |= 2;
            if (hasS) mask |= 4;
            if (hasW) mask |= 8;

            GameObject toSpawn = null;
            float rotY = 0f;

            switch (mask) {
                case 1: toSpawn = modDeadEnd; rotY = 0; break;
                case 2: toSpawn = modDeadEnd; rotY = 90; break;
                case 4: toSpawn = modDeadEnd; rotY = 180; break;
                case 8: toSpawn = modDeadEnd; rotY = 270; break;

                case 5: toSpawn = modStraight; rotY = 0; break;
                case 10: toSpawn = modStraight; rotY = 90; break;

                case 3: toSpawn = modCorner; rotY = 0; break;
                case 6: toSpawn = modCorner; rotY = 90; break;
                case 12: toSpawn = modCorner; rotY = 180; break;
                case 9: toSpawn = modCorner; rotY = 270; break;

                case 7: toSpawn = modTJunction; rotY = 0; break;
                case 14: toSpawn = modTJunction; rotY = 90; break;
                case 13: toSpawn = modTJunction; rotY = 180; break;
                case 11: toSpawn = modTJunction; rotY = 270; break;

                case 15: toSpawn = modCross; rotY = 0; break;
            }

            if (toSpawn != null) {
                Instantiate(toSpawn, worldPos, Quaternion.Euler(0, rotY, 0), rebakedRoot.transform);
                replacedCount++;
            }
        }

        // Hide the old cube maze without destroying it
        maze.SetActive(false);
        Debug.Log($"SUCCESS: Reverse-engineered {replacedCount} cube tiles and replaced them with actual 3D models! Your original maze is hidden in the Hierarchy as a backup.");
    }
}
