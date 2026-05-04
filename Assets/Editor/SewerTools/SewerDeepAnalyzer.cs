using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SewerDeepAnalyzer : EditorWindow
{
    [System.Serializable]
    public class PipeVariant {
        public GameObject prefab;
        public float rotationFix; 
    }

    public List<PipeVariant> straights = new List<PipeVariant>();
    public List<PipeVariant> corners = new List<PipeVariant>();
    public List<PipeVariant> tJunctions = new List<PipeVariant>();
    public List<PipeVariant> crosses = new List<PipeVariant>();
    public List<PipeVariant> deadEnds = new List<PipeVariant>();

    [MenuItem("Tools/Sewer Tools/DEEP ANALYZER & AUTO-BUILDER")]
    public static void ShowWindow() => GetWindow<SewerDeepAnalyzer>("Deep Analyzer");

    void OnGUI()
    {
        GUILayout.Label("1. Deep Object Analysis", EditorStyles.boldLabel);
        if (GUILayout.Button("Analyze FBX & Map Topologies", GUILayout.Height(40))) {
            AnalyzeModels();
        }

        EditorGUILayout.Space();
        GUILayout.Label($"Pool: {straights.Count} Straights | {corners.Count} Corners | {tJunctions.Count} T-Juncs | {crosses.Count} Crosses");

        EditorGUILayout.Space();
        GUILayout.Label("2. Rebake Maze with Variations", EditorStyles.boldLabel);
        if (GUILayout.Button("Auto-Assign & Rebake Maze", GUILayout.Height(50))) {
            RebakeMaze();
        }

        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); // Light red
        if (GUILayout.Button("Undo Rebake (Restore Old Maze)", GUILayout.Height(30))) {
            RestoreOldMaze();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.HelpBox("This tool deeply scans the FBX file. It calculates the exact bounding box, checks vertex proximity to detect physical tunnel exits, normalizes the rotation of every single variant, and randomly distributes them across your existing maze for a highly detailed, professional look.", MessageType.Info);
    }

    void AnalyzeModels()
    {
        straights.Clear(); corners.Clear(); tJunctions.Clear(); crosses.Clear(); deadEnds.Clear();
        
        string fbxPath = "Assets/Models/SewerKit/Sewer/Models/Sewers.fbx";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        if (allAssets == null || allAssets.Length == 0) {
            Debug.LogError("Could not find the Sewers.fbx file. Make sure it's imported at the correct path!");
            return;
        }

        string folderPath = "Assets/Models/SewerKit/ExtractedPipes";
        if (Directory.Exists(folderPath)) {
            Directory.Delete(folderPath, true);
            AssetDatabase.Refresh();
        }
        Directory.CreateDirectory(folderPath);

        // Prevent PINK materials by detecting URP
        Shader sewerShader = Shader.Find("Universal Render Pipeline/Lit");
        if (sewerShader == null) sewerShader = Shader.Find("Standard");

        Material mat = new Material(sewerShader);
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/SewerKit/Sewer/Textures/bricks.jpg");
        mat.color = new Color(0.6f, 0.6f, 0.6f);

        int count = 0;
        foreach (Object asset in allAssets)
        {
            if (asset is Mesh mesh)
            {
                if (mesh.vertexCount < 50) continue; 
                Bounds b = mesh.bounds;

                // Relative thresholds (10% of the bounding box) adapts to any import scale
                float threshX = b.size.x * 0.1f;
                float threshZ = b.size.z * 0.1f;

                // Skip flat planes or impossibly thin geometry
                if (b.size.x < 0.01f || b.size.z < 0.01f) continue;

                bool openN = false, openS = false, openE = false, openW = false;

                foreach (Vector3 v in mesh.vertices)
                {
                    // Check floor level (bottom 20% of the mesh)
                    if (v.y > b.min.y + (b.size.y * 0.2f)) continue; 

                    if (Mathf.Abs(v.z - b.max.z) < threshZ) openN = true;
                    if (Mathf.Abs(v.z - b.min.z) < threshZ) openS = true;
                    if (Mathf.Abs(v.x - b.max.x) < threshX) openE = true;
                    if (Mathf.Abs(v.x - b.min.x) < threshX) openW = true;
                }

                int openCount = (openN?1:0) + (openS?1:0) + (openE?1:0) + (openW?1:0);

                // Filter out non-sewer props (trash bags, doors) unless they are huge
                string meshName = mesh.name.ToLower();
                bool isSewer = meshName.Contains("serwer") || meshName.Contains("sewer");
                if (!isSewer && b.size.magnitude < 3f) continue; 

                string savePath = $"{folderPath}/{mesh.name}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(savePath);
                if (prefab == null) {
                    GameObject go = new GameObject(mesh.name);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                    prefab = PrefabUtility.SaveAsPrefabAsset(go, savePath);
                    DestroyImmediate(go);
                }

                PipeVariant variant = new PipeVariant { prefab = prefab, rotationFix = 0f };

                if (openCount == 4) {
                    crosses.Add(variant);
                    Debug.Log($"[Crossroad] {mesh.name}");
                }
                else if (openCount == 3) {
                    if (!openW) variant.rotationFix = 0;
                    else if (!openN) variant.rotationFix = -90;
                    else if (!openE) variant.rotationFix = -180;
                    else if (!openS) variant.rotationFix = -270;
                    tJunctions.Add(variant);
                    Debug.Log($"[T-Junction] {mesh.name}");
                }
                else if (openCount == 2) {
                    if (openN && openS) { variant.rotationFix = 0; straights.Add(variant); Debug.Log($"[Straight] {mesh.name}"); }
                    else if (openE && openW) { variant.rotationFix = -90; straights.Add(variant); Debug.Log($"[Straight] {mesh.name}"); }
                    else {
                        if (openN && openE) variant.rotationFix = 0;
                        else if (openE && openS) variant.rotationFix = -90;
                        else if (openS && openW) variant.rotationFix = -180;
                        else if (openW && openN) variant.rotationFix = -270;
                        corners.Add(variant);
                        Debug.Log($"[Corner] {mesh.name}");
                    }
                }
                else if (openCount == 1) {
                    if (openN) variant.rotationFix = 0;
                    else if (openE) variant.rotationFix = -90;
                    else if (openS) variant.rotationFix = -180;
                    else if (openW) variant.rotationFix = -270;
                    deadEnds.Add(variant);
                    Debug.Log($"[Dead End] {mesh.name}");
                }
                
                if (openCount > 0) count++;
            }
        }
        Debug.Log($"Deep Analysis Complete! Found {count} usable modular blocks.");
    }

    void RebakeMaze()
    {
        GameObject maze = GameObject.Find("SewerMaze_Master");
        if (maze == null) maze = GameObject.Find("SewerMaze_Hybrid");
        if (maze == null) maze = GameObject.Find("SewerMaze_Rebaked_3D"); // allow rebaking over rebaked
        if (maze == null) { Debug.LogError("No maze found!"); return; }

        Dictionary<Vector2Int, GameObject> floorTiles = new Dictionary<Vector2Int, GameObject>();
        float cellSize = 4f;

        foreach (Transform child in maze.transform) {
            // Find floors by looking for flat objects at Y=0
            if (Mathf.Approximately(child.localScale.y, 0.1f) && child.position.y < 0.5f) {
                int gridX = Mathf.RoundToInt(child.position.x / cellSize);
                int gridZ = Mathf.RoundToInt(child.position.z / cellSize);
                Vector2Int pos = new Vector2Int(gridX, gridZ);
                if (!floorTiles.ContainsKey(pos)) floorTiles.Add(pos, child.gameObject);
            }
        }

        GameObject rebakedRoot = new GameObject("SewerMaze_Ultimate_3D");
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

            PipeVariant pv = GetRandomVariant(mask);
            if (pv != null && pv.prefab != null) {
                float targetRot = GetTargetRotation(mask);
                GameObject spawned = Instantiate(pv.prefab, worldPos, Quaternion.Euler(0, targetRot + pv.rotationFix, 0), rebakedRoot.transform);
                
                // Perfect Scale Fix: Scale the tiny model up to exactly match our maze cell size
                MeshFilter mf = spawned.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) {
                    float currentSize = Mathf.Max(mf.sharedMesh.bounds.size.x, mf.sharedMesh.bounds.size.z);
                    if (currentSize > 0.001f) {
                        float requiredScale = cellSize / currentSize;
                        spawned.transform.localScale = new Vector3(requiredScale, requiredScale, requiredScale);
                    }
                }
                replacedCount++;
            }
        }

        maze.SetActive(false);
        Debug.Log($"SUCCESS: Reverse-engineered and constructed {replacedCount} high-fidelity 3D modules! The layout remains exactly the same.");
    }

    void RestoreOldMaze()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects) {
            // Ensure we are only touching objects in the scene, not project assets
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave || !go.scene.isLoaded) continue;

            if (go.name == "SewerMaze_Master" || go.name == "SewerMaze_Hybrid") {
                go.SetActive(true);
                Debug.Log("Restored your original perfect maze!");
            }
            if (go.name == "SewerMaze_Ultimate_3D" || go.name == "SewerMaze_Rebaked_3D") {
                DestroyImmediate(go);
                Debug.Log("Deleted the 3D Rebaked maze.");
            }
        }
    }

    PipeVariant GetRandomVariant(int mask) {
        if (mask == 1 || mask == 2 || mask == 4 || mask == 8) return GetRandom(deadEnds);
        if (mask == 5 || mask == 10) return GetRandom(straights);
        if (mask == 3 || mask == 6 || mask == 12 || mask == 9) return GetRandom(corners);
        if (mask == 7 || mask == 14 || mask == 13 || mask == 11) return GetRandom(tJunctions);
        if (mask == 15) return GetRandom(crosses);
        return null;
    }

    PipeVariant GetRandom(List<PipeVariant> list) {
        if (list == null || list.Count == 0) return null;
        return list[Random.Range(0, list.Count)];
    }

    float GetTargetRotation(int mask) {
        switch (mask) {
            case 1: return 0;   case 2: return 90;  case 4: return 180; case 8: return 270;
            case 5: return 0;   case 10: return 90;
            case 3: return 0;   case 6: return 90;  case 12: return 180; case 9: return 270;
            case 7: return 0;   case 14: return 90; case 13: return 180; case 11: return 270;
            case 15: return 0;
            default: return 0;
        }
    }
}
