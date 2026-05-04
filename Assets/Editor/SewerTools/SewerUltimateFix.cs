using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SewerUltimateFix : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/ULTIMATE 3D GENERATOR")]
    public static void ShowWindow() => GetWindow<SewerUltimateFix>("Ultimate 3D");

    void OnGUI()
    {
        GUILayout.Label("The Ultimate 3D Generator", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1. Deep Scan & Fix Assets", GUILayout.Height(40))) {
            AnalyzeAndFix();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("2. Rebake Perfect Maze", GUILayout.Height(50))) {
            Rebake();
        }
        
        EditorGUILayout.Space();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Undo / Restore Old Maze")) {
            RestoreOldMaze();
        }
        GUI.backgroundColor = Color.white;
    }

    class PipeVar {
        public GameObject prefab;
        public float rotFix;
    }
    List<PipeVar> straights = new List<PipeVar>();
    List<PipeVar> corners = new List<PipeVar>();
    List<PipeVar> ts = new List<PipeVar>();
    List<PipeVar> crosses = new List<PipeVar>();

    void AnalyzeAndFix()
    {
        straights.Clear(); corners.Clear(); ts.Clear(); crosses.Clear();
        
        string fbxPath = "Assets/Models/SewerKit/Sewer/Models/Sewers.fbx";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        // Use an Unlit shader temporarily to ensure you can actually see the textures even without lights!
        Material mat = new Material(Shader.Find("Unlit/Texture"));
        mat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/SewerKit/Sewer/Textures/bricks.jpg");

        int count = 0;
        foreach (Object asset in allAssets) {
            if (asset is Mesh mesh) {
                if (mesh.vertexCount < 50) continue; 
                Bounds b = mesh.bounds;
                if (b.size.x < 0.01f || b.size.z < 0.01f) continue;

                float threshX = b.size.x * 0.1f;
                float threshZ = b.size.z * 0.1f;
                bool openN = false, openS = false, openE = false, openW = false;

                foreach (Vector3 v in mesh.vertices) {
                    if (v.y > b.min.y + (b.size.y * 0.2f)) continue; 
                    if (Mathf.Abs(v.z - b.max.z) < threshZ) openN = true;
                    if (Mathf.Abs(v.z - b.min.z) < threshZ) openS = true;
                    if (Mathf.Abs(v.x - b.max.x) < threshX) openE = true;
                    if (Mathf.Abs(v.x - b.min.x) < threshX) openW = true;
                }

                int openCount = (openN?1:0) + (openS?1:0) + (openE?1:0) + (openW?1:0);
                string nameL = mesh.name.ToLower();
                if (!nameL.Contains("sewer") && !nameL.Contains("serwer") && b.size.magnitude < 3f) continue;

                GameObject temp = new GameObject(mesh.name);
                temp.AddComponent<MeshFilter>().sharedMesh = mesh;
                temp.AddComponent<MeshRenderer>().sharedMaterial = mat;
                // Don't save to disk to save time, just keep in memory for this session
                temp.hideFlags = HideFlags.HideAndDontSave;
                temp.SetActive(false);

                PipeVar pv = new PipeVar { prefab = temp, rotFix = 0 };

                if (openCount == 4) { crosses.Add(pv); }
                else if (openCount == 3) {
                    if (!openW) pv.rotFix = 0;
                    else if (!openN) pv.rotFix = -90;
                    else if (!openE) pv.rotFix = -180;
                    else if (!openS) pv.rotFix = -270;
                    ts.Add(pv);
                }
                else if (openCount == 2) {
                    if (openN && openS) { pv.rotFix = 0; straights.Add(pv); }
                    else if (openE && openW) { pv.rotFix = -90; straights.Add(pv); }
                    else {
                        if (openN && openE) pv.rotFix = 0;
                        else if (openE && openS) pv.rotFix = -90;
                        else if (openS && openW) pv.rotFix = -180;
                        else if (openW && openN) pv.rotFix = -270;
                        corners.Add(pv);
                    }
                }
                if (openCount > 1) count++;
            }
        }
        Debug.Log($"Deep Scan Complete! Found {count} usable pieces.");
    }

    void Rebake()
    {
        if (straights.Count == 0) { Debug.LogError("You must run Step 1 first!"); return; }

        GameObject maze = null;
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave || !go.scene.isLoaded) continue;
            if (go.name == "SewerMaze_Master" || go.name == "SewerMaze_Hybrid") { maze = go; break; }
        }

        if (maze == null) { Debug.LogError("No original maze found!"); return; }

        Dictionary<Vector2Int, GameObject> floorTiles = new Dictionary<Vector2Int, GameObject>();
        float cellSize = 4f;

        foreach (Transform child in maze.transform) {
            if (Mathf.Approximately(child.localScale.y, 0.1f) && child.position.y < 0.5f) {
                int gridX = Mathf.RoundToInt(child.position.x / cellSize);
                int gridZ = Mathf.RoundToInt(child.position.z / cellSize);
                Vector2Int pos = new Vector2Int(gridX, gridZ);
                if (!floorTiles.ContainsKey(pos)) floorTiles.Add(pos, child.gameObject);
            }
        }

        GameObject rebakedRoot = new GameObject("SewerMaze_ULTIMATE_3D");

        foreach (var kvp in floorTiles) {
            Vector2Int pos = kvp.Key;
            Vector3 worldPos = new Vector3(pos.x * cellSize, 0, pos.y * cellSize);
            
            bool hasN = floorTiles.ContainsKey(pos + Vector2Int.up);
            bool hasE = floorTiles.ContainsKey(pos + Vector2Int.right);
            bool hasS = floorTiles.ContainsKey(pos + Vector2Int.down);
            bool hasW = floorTiles.ContainsKey(pos + Vector2Int.left);

            int mask = 0;
            if (hasN) mask |= 1; if (hasE) mask |= 2; if (hasS) mask |= 4; if (hasW) mask |= 8;

            PipeVar pv = null;
            float rotY = 0f;

            switch (mask) {
                case 1: case 2: case 4: case 8: pv = straights[Random.Range(0, straights.Count)]; rotY = (mask==1||mask==4)?0:90; break;
                case 5: pv = straights[Random.Range(0, straights.Count)]; rotY = 0; break;
                case 10: pv = straights[Random.Range(0, straights.Count)]; rotY = 90; break;

                case 3: pv = corners[Random.Range(0, corners.Count)]; rotY = 0; break;
                case 6: pv = corners[Random.Range(0, corners.Count)]; rotY = 90; break;
                case 12: pv = corners[Random.Range(0, corners.Count)]; rotY = 180; break;
                case 9: pv = corners[Random.Range(0, corners.Count)]; rotY = 270; break;

                case 7: pv = ts[Random.Range(0, ts.Count)]; rotY = 0; break;
                case 14: pv = ts[Random.Range(0, ts.Count)]; rotY = 90; break;
                case 13: pv = ts[Random.Range(0, ts.Count)]; rotY = 180; break;
                case 11: pv = ts[Random.Range(0, ts.Count)]; rotY = 270; break;

                case 15: pv = crosses[Random.Range(0, crosses.Count)]; rotY = 0; break;
            }

            if (pv != null) {
                // Create a parent anchor to perfectly control the pivot
                GameObject anchor = new GameObject("GridCell");
                anchor.transform.position = worldPos;
                anchor.transform.rotation = Quaternion.Euler(0, rotY + pv.rotFix, 0);
                anchor.transform.parent = rebakedRoot.transform;

                // Instantiate the visual mesh as a child
                GameObject visual = Instantiate(pv.prefab, anchor.transform);
                visual.SetActive(true);
                
                Mesh mesh = visual.GetComponent<MeshFilter>().sharedMesh;
                Bounds b = mesh.bounds;

                // 1. Center the pivot perfectly by offsetting the child
                visual.transform.localPosition = -b.center;

                // 2. Stretch exactly to the cell size (4x4) to close ALL gaps
                float scaleX = cellSize / b.size.x;
                float scaleZ = cellSize / b.size.z;
                // keep Y scale proportional to X to avoid flat ceilings
                visual.transform.localScale = new Vector3(scaleX, scaleX, scaleZ);
            }
        }

        maze.SetActive(false);
        Debug.Log("ULTIMATE REBAKE COMPLETE! Perfect grids, perfect pivots, no gaps, bright textures.");
    }

    void RestoreOldMaze()
    {
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave || !go.scene.isLoaded) continue;
            if (go.name == "SewerMaze_Master" || go.name == "SewerMaze_Hybrid") go.SetActive(true);
            if (go.name == "SewerMaze_ULTIMATE_3D" || go.name == "SewerMaze_LLM_3D") DestroyImmediate(go);
        }
    }
}
