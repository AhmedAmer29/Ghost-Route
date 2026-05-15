using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SewerLLMAssign : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/LLM ONE-CLICK REBAKE")]
    public static void ShowWindow() => GetWindow<SewerLLMAssign>("LLM Rebake");

    void OnGUI()
    {
        GUILayout.Label("LLM Hardcoded Rebake", EditorStyles.boldLabel);
        
        if (GUILayout.Button("1-Click: Turn Perfect Maze to 3D", GUILayout.Height(50))) {
            RebakeMaze();
        }
        
        EditorGUILayout.Space();
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("Undo (Restore Cube Maze)", GUILayout.Height(30))) {
            RestoreOldMaze();
        }
        GUI.backgroundColor = Color.white;
    }

    void RebakeMaze()
    {
        // 1. I am hardcoding the exact names of the meshes I read from your logs.
        string fbxPath = "Assets/Models/SewerKit/Sewer/Models/Sewers.fbx";
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        
        if (allAssets == null || allAssets.Length == 0) {
            Debug.LogError("Could not find Sewers.fbx! Did you move it?");
            return;
        }

        Mesh meshStraight = null;
        Mesh meshCorner = null;
        Mesh meshT = null;
        Mesh meshCross = null;

        foreach (Object asset in allAssets) {
            if (asset is Mesh m) {
                // I hand-picked these specific meshes from your FBX for the maze!
                if (m.name == "Serwers02") meshStraight = m;
                if (m.name == "Serwers_015") meshCorner = m;
                if (m.name == "Serwers01_004") meshT = m;
                if (m.name == "Serwers_002") meshCross = m;
            }
        }

        if (meshStraight == null) { Debug.LogError("Failed to find 'Serwers02' in the FBX!"); return; }

        // 2. Fix the PINK Material bug by using the correct URP Shader properties
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        
        Material mat = new Material(shader);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/SewerKit/Sewer/Textures/bricks.jpg");
        
        if (shader.name.Contains("Universal")) {
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", new Color(0.6f, 0.6f, 0.6f));
        } else {
            mat.mainTexture = tex;
            mat.color = new Color(0.6f, 0.6f, 0.6f);
        }

        // 3. Find your hidden perfect maze
        GameObject maze = null;
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects) {
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave || !go.scene.isLoaded) continue;
            if (go.name == "SewerMaze_Master" || go.name == "SewerMaze_Hybrid") { maze = go; break; }
        }

        if (maze == null) { Debug.LogError("No original perfect maze found to upgrade!"); return; }

        Dictionary<Vector2Int, GameObject> floorTiles = new Dictionary<Vector2Int, GameObject>();
        float cellSize = 4f;

        // Scan layout
        foreach (Transform child in maze.transform) {
            if (Mathf.Approximately(child.localScale.y, 0.1f) && child.position.y < 0.5f) {
                int gridX = Mathf.RoundToInt(child.position.x / cellSize);
                int gridZ = Mathf.RoundToInt(child.position.z / cellSize);
                Vector2Int pos = new Vector2Int(gridX, gridZ);
                if (!floorTiles.ContainsKey(pos)) floorTiles.Add(pos, child.gameObject);
            }
        }

        GameObject rebakedRoot = new GameObject("SewerMaze_LLM_3D");

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

            Mesh toSpawn = null;
            float rotY = 0f;

            switch (mask) {
                case 1: toSpawn = meshStraight; rotY = 0; break;
                case 2: toSpawn = meshStraight; rotY = 90; break;
                case 4: toSpawn = meshStraight; rotY = 0; break;
                case 8: toSpawn = meshStraight; rotY = 90; break;

                case 5: toSpawn = meshStraight; rotY = 0; break;
                case 10: toSpawn = meshStraight; rotY = 90; break;

                case 3: toSpawn = meshCorner; rotY = 0; break;
                case 6: toSpawn = meshCorner; rotY = 90; break;
                case 12: toSpawn = meshCorner; rotY = 180; break;
                case 9: toSpawn = meshCorner; rotY = 270; break;

                case 7: toSpawn = meshT; rotY = 0; break;
                case 14: toSpawn = meshT; rotY = 90; break;
                case 13: toSpawn = meshT; rotY = 180; break;
                case 11: toSpawn = meshT; rotY = 270; break;

                case 15: toSpawn = meshCross; rotY = 0; break;
            }

            if (toSpawn != null) {
                GameObject go = new GameObject("PipeModule");
                go.transform.position = worldPos;
                go.transform.rotation = Quaternion.Euler(0, rotY, 0);
                go.transform.parent = rebakedRoot.transform;

                MeshFilter mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = toSpawn;
                MeshRenderer mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;

                // Perfect Scale Fix
                float currentSize = Mathf.Max(toSpawn.bounds.size.x, toSpawn.bounds.size.z);
                if (currentSize > 0.001f) {
                    float requiredScale = cellSize / currentSize;
                    go.transform.localScale = new Vector3(requiredScale, requiredScale, requiredScale);
                }
            }
        }

        maze.SetActive(false);
        Debug.Log("LLM Hardcoded Rebake Complete! Your maze is now 3D, fully scaled, and correctly textured.");
    }

    void RestoreOldMaze()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject go in allObjects) {
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave || !go.scene.isLoaded) continue;
            if (go.name == "SewerMaze_Master" || go.name == "SewerMaze_Hybrid") go.SetActive(true);
            if (go.name == "SewerMaze_LLM_3D" || go.name == "SewerMaze_Ultimate_3D") DestroyImmediate(go);
        }
    }
}
