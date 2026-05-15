using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections.Generic;

public class SewerToolsMenu : EditorWindow
{
    private const string TargetMaterialPath = "Assets/Models/SewerProps/electrical box/source/electrical boxes/Materials/electrical boxes_Base_Color.mat";

    [MenuItem("Tools/Sewer Tools/Fix Electrical Box Materials")]
    public static void ApplyElectricalMaterials()
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(TargetMaterialPath);
        if (mat == null) return;

        int totalUpdated = 0;
        foreach (GameObject parent in allObjects)
        {
            if (parent.name.ToLower().Contains("electrical boxes") || parent.name.ToLower().Contains("electricbox"))
            {
                MeshRenderer[] renderers = parent.GetComponentsInChildren<MeshRenderer>(true);
                Undo.RecordObjects(renderers, "Apply Electrical Box Materials");
                foreach (var renderer in renderers)
                {
                    Material[] newMats = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < newMats.Length; i++) newMats[i] = mat;
                    renderer.sharedMaterials = newMats;
                    EditorUtility.SetDirty(renderer);
                    totalUpdated++;
                }
            }
        }
        Debug.Log($"[SewerTools] Updated {totalUpdated} slots.");
    }

    [MenuItem("Tools/Sewer Tools/Auto-Setup Power Puzzle")]
    public static void SetupPowerPuzzle()
    {
        // 1. Master Brain
        GameObject brain = GameObject.Find("Master Power Brain");
        if (brain == null) brain = new GameObject("Master Power Brain");
        var master = brain.GetComponent<MasterPowerSystem>();
        if (master == null) master = brain.AddComponent<MasterPowerSystem>();

        // 2. Lever
        GameObject lever = GameObject.Find("Lever Switch");
        if (lever == null) { Debug.LogError("Could not find 'Lever Switch'!"); return; }
        var leverInt = lever.GetComponent<LeverInteraction>();
        if (leverInt == null) leverInt = lever.AddComponent<LeverInteraction>();
        Transform handle = lever.transform.Find("Handle");
        if (handle != null) leverInt.leverHandle = handle;
        
        while (master.onAllFixed.GetPersistentEventCount() > 0)
        {
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(master.onAllFixed, 0);
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(master.onAllFixed, leverInt.Unlock);

        // 3. Setup Specific Boxes
        SetupSpecificBox(master, "electrical boxes (1)", "modular-box-group-04.000", true);
        SetupSpecificBox(master, "electrical boxes (2)", "modular-box-group-01.003", true);
        SetupSpecificBox(master, "ElectricBox (3)", "Box003", true);
        
        SetupSpecificBox(master, "electrical boxes", "modular-box-group-02.003", false);
        SetupSpecificBox(master, "ElectricBox", "Box003", false);
        SetupSpecificBox(master, "electrical boxes (2)", "modular-box-01", false);

        Debug.Log("<color=green>[SewerTools] Custom Power Puzzle setup complete with 3 Real and 3 Fake boxes!</color>");
    }

    [MenuItem("Tools/Sewer Tools/Deep Test Lever Animation")]
    public static void DeepTestLeverAnimation()
    {
        GameObject lever = GameObject.Find("Lever Switch");
        if (lever == null)
        {
            Debug.LogError("[Lever Test] Could not find 'Lever Switch' in the scene.");
            return;
        }

        Transform handle = lever.transform.Find("Handle");
        if (handle == null)
        {
            Debug.LogError("[Lever Test] 'Lever Switch' has no child named 'Handle'.");
            return;
        }

        Transform targetToRotate = handle;
        SkinnedMeshRenderer smr = handle.GetComponent<SkinnedMeshRenderer>();
        if (smr != null && smr.rootBone != null)
        {
            targetToRotate = smr.rootBone;
            Debug.Log($"<color=cyan>[Lever Test] Detected SkinnedMesh. Target to rotate is Bone: {targetToRotate.name}</color>", targetToRotate.gameObject);
        }
        else
        {
            Debug.Log($"<color=cyan>[Lever Test] No SkinnedMesh/Bone found. Target to rotate is Handle: {targetToRotate.name}</color>", targetToRotate.gameObject);
        }

        // 1. Check for Animators
        Animator anim = targetToRotate.GetComponentInParent<Animator>();
        if (anim != null)
        {
            Debug.LogWarning($"<color=orange>[Lever Test] WARNING: Animator found on {anim.gameObject.name}. Animators override script rotations! Disable the Animator component if you want the script to rotate it.</color>", anim.gameObject);
        }

        Animation legacyAnim = targetToRotate.GetComponentInParent<Animation>();
        if (legacyAnim != null)
        {
            Debug.LogWarning($"<color=orange>[Lever Test] WARNING: Legacy Animation found on {legacyAnim.gameObject.name}. This can lock rotation.</color>", legacyAnim.gameObject);
        }

        // 2. Check Static
        if (targetToRotate.gameObject.isStatic)
        {
            Debug.LogError($"<color=red>[Lever Test] ERROR: {targetToRotate.name} is STATIC. It cannot move!</color>", targetToRotate.gameObject);
        }

        // 3. Test Rotation Application
        Quaternion originalRot = targetToRotate.localRotation;
        targetToRotate.localRotation = originalRot * Quaternion.Euler(60, 0, 0);
        
        if (targetToRotate.localRotation == originalRot)
        {
            Debug.LogError($"<color=red>[Lever Test] ERROR: Tried to rotate {targetToRotate.name}, but it snapped back! Something is locking its rotation (likely an Animator).</color>", targetToRotate.gameObject);
        }
        else
        {
            Debug.Log($"<color=green>[Lever Test] SUCCESS: Script successfully rotated {targetToRotate.name} by 60 degrees. Reverting back to original rotation.</color>", targetToRotate.gameObject);
            targetToRotate.localRotation = originalRot;
        }
        
        Debug.Log("<color=yellow>[Lever Test] Scan Complete. If SUCCESS, check your 'Pull Rotation' values in the LeverInteraction script (try 0,60,0 or 0,0,60 instead of 60,0,0).</color>");
    }

    [MenuItem("Tools/Sewer Tools/Auto-Setup Objective Board")]
    public static void SetupObjectiveBoard()
    {
        GameObject boardObj = GameObject.Find("Meshy_AI_Rugged_Industrial_Con_0515180139_texture");
        if (boardObj == null) { Debug.LogError("Could not find the Meshy_AI object!"); return; }

        // 1. Add Script
        var board = boardObj.GetComponent<ObjectiveBoard>();
        if (board == null) board = boardObj.AddComponent<ObjectiveBoard>();
        board.masterSystem = GameObject.FindObjectOfType<MasterPowerSystem>();

        // 2. Create Canvas
        Canvas canvas = boardObj.GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject cObj = new GameObject("ObjectiveCanvas");
            cObj.transform.SetParent(boardObj.transform, false);
            canvas = cObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            cObj.AddComponent<CanvasScaler>();
            
            // Position it slightly in front of the board
            RectTransform rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, 2);
            rt.localPosition = new Vector3(0, 0, 0.1f); 
        }

        // 3. Create Text Elements if missing
        if (board.circuitText == null)
        {
            GameObject t1 = new GameObject("CircuitText");
            t1.transform.SetParent(canvas.transform, false);
            Text txt = t1.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.text = "✖ ELECTRICAL COMPONENTS: 0/3";
            t1.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0.2f);
            board.circuitText = txt;
        }

        if (board.keyText == null)
        {
            GameObject t2 = new GameObject("KeyText");
            t2.transform.SetParent(canvas.transform, false);
            Text txt = t2.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.text = "✖ KEY: NEEDED";
            t2.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -0.2f);
            board.keyText = txt;
        }

        Debug.Log("<color=green>[SewerTools] Objective Board setup complete!</color>");
    }

    [MenuItem("Tools/Sewer Tools/Verify Lever Setup")]
    public static void VerifyLevers()
    {
        LeverInteraction[] levers = GameObject.FindObjectsOfType<LeverInteraction>();
        Debug.Log($"<color=yellow>[SewerTools] Scanning {levers.Length} levers...</color>");

        foreach (var lever in levers)
        {
            Transform h = lever.leverHandle;
            // Fallback if not assigned
            if (h == null) h = lever.transform.Find("Handle");

            if (h == null)
            {
                Debug.LogError($"[Lever Error] {lever.name}: No Handle found! Assign it in the Inspector.", lever);
                continue;
            }

            if (h.gameObject.isStatic)
            {
                Debug.LogError($"[Lever Error] {lever.name}: Handle '{h.name}' is set to STATIC. Click this message, then uncheck 'Static' in the top-right of the Inspector!", h.gameObject);
            }
            else
            {
                Debug.Log($"<color=green>[Lever OK] {lever.name}: Handle is ready to move.</color>", h.gameObject);
            }
            
            if (h.GetComponent<SkinnedMeshRenderer>() != null)
            {
                Debug.Log($"<color=cyan>[Lever Note] {lever.name}: Uses SkinnedMesh. If it doesn't rotate, check if we need to rotate its 'Bone' child instead.</color>", h.gameObject);
            }
        }
    }

    [MenuItem("Tools/Sewer Tools/Setup Rat Colliders")]
    public static void SetupRatColliders()
    {
        // ── 1. Tag the Player ────────────────────────────────────────────────
        // Look for the player by common names / existing tag
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null) player = GameObject.Find("FPSController");
        if (player == null) player = GameObject.Find("PlayerController");

        if (player != null)
        {
            Undo.RecordObject(player, "Tag Player");
            player.tag = "Player";
            EditorUtility.SetDirty(player);
            Debug.Log($"<color=green>[SewerTools] Tagged '{player.name}' as Player.</color>", player);
        }
        else
        {
            Debug.LogWarning("[SewerTools] Could not find the Player object. Tag it manually as 'Player'.");
        }

        // ── 2. Fix every rat ─────────────────────────────────────────────────
        SwarmRatHallRunner[] rats = GameObject.FindObjectsOfType<SwarmRatHallRunner>();
        if (rats.Length == 0)
        {
            Debug.LogWarning("[SewerTools] No rats found in scene (SwarmRatHallRunner).");
            return;
        }

        int fixed_count = 0;
        int damage_enabled = 0;

        foreach (SwarmRatHallRunner rat in rats)
        {
            // Only enable damage if the name contains "black"
            bool isBlackRat = rat.name.ToLower().Contains("black");
            rat.isAttackingRat = isBlackRat;
            if (isBlackRat) damage_enabled++;

            // Check if it already has any trigger collider
            Collider[] cols = rat.GetComponents<Collider>();
            bool hasTrigger = false;
            foreach (Collider c in cols)
            {
                if (c.isTrigger) { hasTrigger = true; break; }
            }

            if (!hasTrigger && isBlackRat) // Only add colliders to the dangerous rats
            {
                CapsuleCollider cap = rat.gameObject.AddComponent<CapsuleCollider>();
                cap.isTrigger = true;
                cap.radius    = 0.25f;
                cap.height    = 0.3f;
                cap.center    = new Vector3(0f, 0.15f, 0f);
                Undo.RegisterCreatedObjectUndo(cap, "Add Rat Trigger");
                fixed_count++;
            }
            
            EditorUtility.SetDirty(rat.gameObject);
        }

        Debug.Log($"<color=green>[SewerTools] Done! Scanned {rats.Length} rats. Enabled damage for {damage_enabled} Black Rats.</color>");
    }

    private static void SetupSpecificBox(MasterPowerSystem master, string parentName, string childName, bool isReal)
    {
        GameObject parent = GameObject.Find(parentName);
        if (parent == null) { Debug.LogWarning($"Could not find parent: {parentName}"); return; }
        
        Transform child = parent.transform.Find(childName);
        if (child == null) { Debug.LogWarning($"Could not find child: {childName} in {parentName}"); return; }

        var sub = child.gameObject.GetComponent<SubPowerBox>();
        if (sub == null) sub = child.gameObject.AddComponent<SubPowerBox>();
        
        sub.masterSystem = master;
        sub.isReal = isReal;
        
        Undo.RecordObject(sub, "Setup SubPowerBox");
        EditorUtility.SetDirty(sub);
    }
}

