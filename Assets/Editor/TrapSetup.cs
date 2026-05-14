using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class TrapSetup
{
    [MenuItem("Tools/Setup All Traps", false, 1)]
    static void SetupAllTraps()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[TrapSetup] Wait for Unity to finish compiling.");
            return;
        }

        Debug.Log("[TrapSetup] Clearing old traps...");

        DestroyOldTrap("SiltTrap");
        DestroyOldTrap("FalseLadder");
        DestroyOldTrap("CeilingCollapse");
        DestroyOldTrap("RatSwarm");

        GameObject trapsRoot = CreateTrapsRoot();

        SetupSiltTrap(trapsRoot);
        SetupRisingWater();
        SetupFalseLadder(trapsRoot);
        SetupCeilingCollapse(trapsRoot);
        SetupRatSwarm(trapsRoot);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[TrapSetup] All traps created! Position them in the scene.");
    }

    static void DestroyOldTrap(string name)
    {
        GameObject go = FindChildRecursive(name);
        if (go != null)
        {
            GameObject.DestroyImmediate(go);
            Debug.Log($"[TrapSetup] Removed old '{name}'.");
        }
    }

    static GameObject CreateTrapsRoot()
    {
        GameObject existing = GameObject.Find("Traps");
        if (existing != null) return existing;

        GameObject root = new GameObject("Traps");
        root.transform.position = Vector3.zero;
        return root;
    }

    static void SetupSiltTrap(GameObject parent)
    {
        GameObject go = new GameObject("SiltTrap");
        go.transform.SetParent(parent.transform);

        GameObject debris = FindChildRecursive("debris");
        if (debris != null) go.transform.position = debris.transform.position + new Vector3(0, 0.5f, 0);
        else go.transform.position = new Vector3(5f, -1f, 5f);

        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3f, 0.5f, 3f);

        go.AddComponent<SiltTrap>();

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Plane);
        visual.name = "MudVisual";
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = new Vector3(0f, -0.25f, 0f);
        visual.transform.localScale = Vector3.one * 1.5f;
        Renderer r = visual.GetComponent<Renderer>();
        r.material = new Material(Shader.Find("Standard"));
        r.material.color = new Color(0.1f, 0.08f, 0.04f);
        r.material.SetFloat("_Glossiness", 0f);

        Debug.Log("[TrapSetup] Created SiltTrap - position it over muddy water.");
    }

    static void SetupRisingWater()
    {
        GameObject water = GameObject.Find("RealisticSewerWater");

        if (water == null)
        {
            water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.name = "RealisticSewerWater";
            water.transform.position = new Vector3(0f, -0.5f, 0f);
            water.transform.localScale = new Vector3(8f, 0.3f, 8f);

            Renderer r = water.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Custom/RealisticDirtyWater"));
            if (mat.shader == null)
            {
                mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.08f, 0.05f, 0.02f, 0.7f);
                mat.SetFloat("_Mode", 3);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            r.material = mat;

            Debug.Log("[TrapSetup] Created water volume (RealisticSewerWater not found in scene).");
        }
        else
        {
            water.transform.localScale = new Vector3(8f, 0.3f, 8f);
        }

        BoxCollider col = water.GetComponent<BoxCollider>();
        if (col == null) col = water.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1f, 10f, 1f);

        if (water.GetComponent<RisingWater>() == null)
            water.AddComponent<RisingWater>();

        Debug.Log("[TrapSetup] Added RisingWater to RealisticSewerWater.");
    }

    static void SetupFalseLadder(GameObject parent)
    {
        // Find the real ladder inside the Sewers model
        GameObject realLadder = FindChildRecursive("Fake_Ladder");
        if (realLadder == null)
            realLadder = FindChildRecursive("ladder");

        GameObject go = new GameObject("FalseLadder");
        go.transform.SetParent(parent.transform);
        go.transform.position = realLadder != null ? realLadder.transform.position : new Vector3(-3f, 0f, 0f);

        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2f, 5f, 2f);
        col.center = new Vector3(0f, 2.5f, 0f);

        FalseLadder ladder = go.AddComponent<FalseLadder>();
        if (realLadder != null)
            ladder.ladderModel = realLadder.transform;

        GameObject climbStart = new GameObject("ClimbStart");
        climbStart.transform.SetParent(go.transform);
        climbStart.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        ladder.climbStart = climbStart.transform;

        GameObject breakPoint = new GameObject("BreakPoint");
        breakPoint.transform.SetParent(go.transform);
        breakPoint.transform.localPosition = new Vector3(0f, 4f, 0f);
        ladder.breakPoint = breakPoint.transform;

        GameObject fallDest = new GameObject("FallDestination");
        fallDest.transform.SetParent(go.transform);
        fallDest.transform.localPosition = new Vector3(0f, -5f, 0f);
        ladder.fallDestination = fallDest.transform;

        GameObject lightGo = new GameObject("ExitLight");
        Light li = lightGo.AddComponent<Light>();
        li.type = LightType.Point;
        li.intensity = 2f;
        li.range = 5f;
        li.color = new Color(1f, 0.8f, 0.4f);
        lightGo.transform.SetParent(go.transform);
        lightGo.transform.localPosition = new Vector3(0f, 6f, 0f);
        ladder.exitLight = li;

        Debug.Log(realLadder != null
            ? $"[TrapSetup] Created FalseLadder linked to '{realLadder.name}'."
            : "[TrapSetup] Created FalseLadder - could not find Fake_Ladder in scene.");
    }

    static GameObject FindChildRecursive(string name)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            var result = FindInChildren(root.transform, name);
            if (result != null) return result;
        }
        return null;
    }

    static GameObject FindInChildren(Transform t, string name)
    {
        if (t.name == name) return t.gameObject;
        for (int i = 0; i < t.childCount; i++)
        {
            var result = FindInChildren(t.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    static void SetupCeilingCollapse(GameObject parent)
    {
        GameObject go = new GameObject("CeilingCollapse");
        go.transform.SetParent(parent.transform);

        GameObject pipe = FindChildRecursive("Pipe_001");
        if (pipe != null) go.transform.position = pipe.transform.position - new Vector3(0, 2f, 0);
        else go.transform.position = new Vector3(0f, 3f, 0f);

        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(5f, 1f, 5f);

        CeilingCollapse cc = go.AddComponent<CeilingCollapse>();

        // Find debris prefab
        string[] debrisGuids = AssetDatabase.FindAssets("debris t:Prefab");
        if (debrisGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(debrisGuids[0]);
            GameObject debrisPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            cc.debrisPrefabs = new GameObject[] { debrisPrefab };
            Debug.Log($"[TrapSetup] Assigned debris prefab: {path}");
        }
        else
        {
            Debug.Log("[TrapSetup] No debris prefab found - CeilingCollapse will use primitive cubes.");
        }

        Debug.Log("[TrapSetup] Created CeilingCollapse - position it under a weak ceiling.");
    }

    static void SetupRatSwarm(GameObject parent)
    {
        GameObject go = new GameObject("RatSwarm");
        go.transform.SetParent(parent.transform);

        GameObject trash = FindChildRecursive("Trash_M");
        if (trash != null) go.transform.position = trash.transform.position + new Vector3(0, 0.5f, 0);
        else go.transform.position = new Vector3(-5f, 0f, 3f);

        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 4f;

        go.AddComponent<RatSwarm>();

        Debug.Log("[TrapSetup] Created RatSwarm - position it in a filthy area.");
    }
}
