using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class MazeSewerConverter : EditorWindow
{
    [MenuItem("Tools/Sewer Tools/LLM MAZE→SEWER CONVERTER")]
    public static void ShowWindow() => GetWindow<MazeSewerConverter>("Maze→Sewer");

    void OnGUI()
    {
        GUILayout.Label("SEWER MAZE CONVERSION", EditorStyles.boldLabel);
        GUILayout.Label("LLM-Driven Direct Conversion", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("CONVERT: Replace Maze Cubes with Sewer Models", GUILayout.Height(60))) {
            ConvertMazeToSewer();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "This tool analyzes your 605-cube maze from Game.unity and generates a sewer-themed version in sewer_maze.unity using intelligent sewer model placement. " +
            "It detects maze topology (straights, corners, junctions) and assigns appropriate sewer prefab variants for visual diversity.",
            MessageType.Info
        );
    }

    void ConvertMazeToSewer()
    {
        // Load the Game scene to read maze positions
        EditorSceneManager.OpenScene("Assets/Scenes/Game.unity", OpenSceneMode.Single);
        
        // Find all cubes (maze blocks)
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<MazeBlock> mazeBlocks = new List<MazeBlock>();
        
        foreach (GameObject go in allObjects)
        {
            if (go.name == "Cube" && go.GetComponent<BoxCollider>() != null && go.GetComponent<MeshFilter>() != null)
            {
                mazeBlocks.Add(new MazeBlock { gameObject = go, position = go.transform.position });
            }
        }

        if (mazeBlocks.Count == 0)
        {
            Debug.LogError("No maze cubes found!");
            return;
        }

        Debug.Log($"Found {mazeBlocks.Count} maze blocks. Creating sewer conversion...");

        // Analyze maze topology
        Dictionary<Vector2Int, List<MazeBlock>> floorGrid = new Dictionary<Vector2Int, List<MazeBlock>>();
        float cellSize = 4f;

        foreach (var block in mazeBlocks)
        {
            if (Mathf.Approximately(block.gameObject.transform.localScale.y, 0.1f))
            {
                // Floor tile
                int gridX = Mathf.RoundToInt(block.position.x / cellSize);
                int gridZ = Mathf.RoundToInt(block.position.z / cellSize);
                Vector2Int gridPos = new Vector2Int(gridX, gridZ);
                
                if (!floorGrid.ContainsKey(gridPos)) floorGrid.Add(gridPos, new List<MazeBlock>());
                floorGrid[gridPos].Add(block);
            }
        }

        // Load sewer prefabs
        List<GameObject> sewers = LoadSewerPrefabs();
        
        // Open sewer_maze scene
        EditorSceneManager.OpenScene("Assets/Scenes/sewer_maze.unity", OpenSceneMode.Single);

        // Create conversion container
        GameObject sewerMaze = new GameObject("SewerMaze_LLMConverted");
        sewerMaze.transform.position = Vector3.zero;

        // Place sewer models
        int placedCount = 0;
        foreach (var gridPos in floorGrid.Keys)
        {
            if (sewers.Count == 0) break;
            
            Vector3 worldPos = new Vector3(gridPos.x * cellSize, 0.2f, gridPos.y * cellSize);
            
            // Detect neighbors to pick appropriate sewer variant
            bool hasN = floorGrid.ContainsKey(gridPos + Vector2Int.up);
            bool hasE = floorGrid.ContainsKey(gridPos + Vector2Int.right);
            bool hasS = floorGrid.ContainsKey(gridPos + Vector2Int.down);
            bool hasW = floorGrid.ContainsKey(gridPos + Vector2Int.left);
            
            int neighbors = (hasN ? 1 : 0) + (hasE ? 1 : 0) + (hasS ? 1 : 0) + (hasW ? 1 : 0);
            
            // Pick sewer type based on topology
            GameObject sewerPrefab = PickSewerVariant(sewers, neighbors, hasN, hasE, hasS, hasW);
            
            if (sewerPrefab != null)
            {
                GameObject sewerInstance = PrefabUtility.InstantiatePrefab(sewerPrefab, sewerMaze.transform) as GameObject;
                sewerInstance.name = $"Sewer_{placedCount}";
                sewerInstance.transform.position = worldPos;
                
                // Randomize rotation for visual variety
                sewerInstance.transform.Rotate(0, Random.Range(0, 4) * 90f, 0);
                placedCount++;
            }
        }

        Debug.Log($"✓ Sewer conversion complete! Placed {placedCount} sewer models in sewer_maze.unity");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    GameObject PickSewerVariant(List<GameObject> sewers, int neighbors, bool n, bool e, bool s, bool w)
    {
        // Intelligent prefab selection based on topology
        List<GameObject> candidates = new List<GameObject>();

        if (neighbors == 2 && ((n && s) || (e && w)))
        {
            // Straight section
            candidates = sewers.Where(p => p.name.Contains("Serwers02") || p.name.Contains("Straight")).ToList();
        }
        else if (neighbors == 2)
        {
            // Corner
            candidates = sewers.Where(p => p.name.Contains("_015") || p.name.Contains("Corner")).ToList();
        }
        else if (neighbors == 3)
        {
            // T-junction
            candidates = sewers.Where(p => p.name.Contains("01_004") || p.name.Contains("01_007")).ToList();
        }
        else if (neighbors == 4)
        {
            // Cross junction
            candidates = sewers.Where(p => p.name.Contains("_002") || p.name.Contains("Cross")).ToList();
        }
        else if (neighbors == 1)
        {
            // Dead-end
            candidates = sewers.Where(p => p.name.Contains("SerwersP") && !p.name.Contains("_")).ToList();
        }

        if (candidates.Count == 0) candidates = sewers; // Fallback to any sewer

        return candidates[Random.Range(0, candidates.Count)];
    }

    List<GameObject> LoadSewerPrefabs()
    {
        List<GameObject> prefabs = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Models/SewerKit/ExtractedPipes" });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.name != "debris") // Skip debris
            {
                prefabs.Add(prefab);
            }
        }

        Debug.Log($"Loaded {prefabs.Count} sewer prefabs");
        return prefabs;
    }

    struct MazeBlock
    {
        public GameObject gameObject;
        public Vector3 position;
    }
}
