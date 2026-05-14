using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;

public class PlayerSetup
{
    [MenuItem("Tools/Setup Player Character", false, 0)]
    static void SetupPlayer()
    {
        Setup();

        if (!Application.isPlaying)
            EditorSceneManager.SaveOpenScenes();
    }

    [MenuItem("Tools/Setup Player Character", true)]
    static bool ValidateSetup()
    {
        return !EditorApplication.isCompiling && !EditorApplication.isUpdating;
    }

    static void Setup()
    {
        Debug.Log("[Player Setup] Starting...");

        EnsurePlayerBodyLayer();

        GameObject player = FindOrCreatePlayer();
        if (player == null)
        {
            Debug.LogError("[Player Setup] Could not find or create player GameObject.");
            return;
        }

        AddComponentByName(player, "PlayerMovement");
        AddComponentByName(player, "PlayerAnimation");
        AddComponentByName(player, "PlayerCombat");
        AddComponentByName(player, "FPSBodyVisibility");
        AddComponentByName(player, "StaminaUI");
        PlayerKnockbackReceiver knockback = player.GetComponent<PlayerKnockbackReceiver>();
        if (knockback == null) knockback = player.AddComponent<PlayerKnockbackReceiver>();
        if (player.GetComponent<CharacterController>() == null)
            player.AddComponent<CharacterController>();
        AddComponentByName(player, "PlayerState");

        Animator anim = player.GetComponent<Animator>();
        if (anim == null) anim = player.AddComponent<Animator>();
        if (anim.runtimeAnimatorController == null)
        {
            string[] guids = AssetDatabase.FindAssets("Criminal Animator t:AnimatorController");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
                anim.applyRootMotion = false;
                Debug.Log("[Player Setup] Assigned Criminal Animator controller.");
            }
        }

        SkinnedMeshRenderer mesh = player.GetComponentInChildren<SkinnedMeshRenderer>();
        if (mesh != null)
        {
            Component vis = player.GetComponent("FPSBodyVisibility");
            if (vis != null)
            {
                var field = vis.GetType().GetField("characterRoot");
                if (field != null && field.GetValue(vis) == null)
                    field.SetValue(vis, mesh.transform.root != player.transform ? mesh.transform.parent : mesh.transform);
            }
        }

        Component controls = player.GetComponent("PlayerControls");
        Component combat = player.GetComponent("PlayerCombat");
        knockback.componentsToDisable = new MonoBehaviour[]
        {
            controls as MonoBehaviour,
            combat as MonoBehaviour,
        };

        SetupCamera(player);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[Player Setup] Done! Player: {player.name}");
    }

    static GameObject FindOrCreatePlayer()
    {
        Type controlsType = GetTypeFromAllAssemblies("PlayerControls");
        if (controlsType != null)
        {
            Component existing = UnityEngine.Object.FindObjectOfType(controlsType) as Component;
            if (existing != null) return existing.gameObject;
        }

        Type movementType = GetTypeFromAllAssemblies("PlayerMovement");
        if (movementType != null)
        {
            Component existing = UnityEngine.Object.FindObjectOfType(movementType) as Component;
            if (existing != null) return existing.gameObject;
        }

        string[] guids = AssetDatabase.FindAssets("Character_output t:Model");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene());
                go.name = "Player";
                go.transform.position = GetSpawnPosition();
                Debug.Log($"[Player Setup] Created Player from {path}");
                return go;
            }
        }

        Debug.LogError("[Player Setup] Could not find Criminal model. Import the Criminal folder first.");
        return null;
    }

    static Vector3 GetSpawnPosition()
    {
        Camera cam = Camera.main;
        if (cam != null) return cam.transform.position + cam.transform.forward * 3f;
        return Vector3.zero;
    }

    static void SetupCamera(GameObject player)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[Player Setup] No MainCamera found.");
            return;
        }

        GameObject camGo = cam.gameObject;

        Component pcam = AddComponentByName(camGo, "PlayerCamera");
        var targetField = pcam.GetType().GetField("target");
        if (targetField != null)
            targetField.SetValue(pcam, player.transform);

        cam.transform.SetParent(player.transform);
        cam.transform.localPosition = new Vector3(0f, 1.6f, 0.2f);
        cam.transform.localRotation = Quaternion.identity;

        // Add our new UnderwaterPostProcess effect
        AddComponentByName(camGo, "UnderwaterPostProcess");

        PlayerKnockbackReceiver old = camGo.GetComponent<PlayerKnockbackReceiver>();
        if (old != null)
        {
            UnityEngine.Object.DestroyImmediate(old);
            Debug.Log("[Player Setup] Removed old PlayerKnockbackReceiver from camera.");
        }

        Debug.Log($"[Player Setup] Camera target set to '{player.name}'. First-person ready.");
    }

    static Component AddComponentByName(GameObject go, string typeName)
    {
        Type type = GetTypeFromAllAssemblies(typeName);
        if (type == null)
        {
            Debug.LogWarning($"[Player Setup] Type '{typeName}' not found. Make sure the Criminal scripts are imported.");
            return null;
        }

        Component comp = go.GetComponent(type);
        if (comp == null) comp = go.AddComponent(type);
        return comp;
    }

    static Type GetTypeFromAllAssemblies(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null) return type;
        }
        return null;
    }

    static void EnsurePlayerBodyLayer()
    {
        int layer = LayerMask.NameToLayer("PlayerBody");
        if (layer != -1) return;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i <= 31; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = "PlayerBody";
                tagManager.ApplyModifiedProperties();
                Debug.Log("[Player Setup] Created 'PlayerBody' layer.");
                return;
            }
        }

        Debug.LogWarning("[Player Setup] No free User Layer slot found. Create 'PlayerBody' manually.");
    }
}
