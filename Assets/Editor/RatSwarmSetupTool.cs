using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class RatSwarmSetupTool : EditorWindow
{
    private const string AnimationFolder = "Assets/Animations";
    private const string BlackControllerPath = "Assets/Animations/BlackRat_Controller.controller";
    private const string SwarmControllerPath = "Assets/Animations/SwarmRat_Controller.controller";
    private const string BlackRatFbxPath = "Assets/Models/SewerProps/black-rat-free-download/source/blackrat.fbx";
    private const string SwarmRatFbxPath = "Assets/Models/SewerProps/rat-animated_2/source/RAT.fbx";

    [MenuItem("Tools/Sewer Tools/MASS ASSIGN RAT ANIMATORS")]
    public static void AssignAnimators()
    {
        if (!Directory.Exists(AnimationFolder))
        {
            Directory.CreateDirectory(AnimationFolder);
        }

        string[] blackIdleStates;
        string[] blackStates;
        string swarmRunState;
        AnimatorController blackController = GetOrCreateController(BlackControllerPath, BlackRatFbxPath, true, out blackIdleStates, out blackStates);
        AnimatorController swarmController = GetOrCreateController(SwarmControllerPath, SwarmRatFbxPath, false, out string[] swarmStates);
        Avatar blackAvatar = FindAvatar(BlackRatFbxPath);
        Avatar swarmAvatar = FindAvatar(SwarmRatFbxPath);
        string blackAttackState = ChooseStateByKeywords(blackStates, "run", "walk", "attack", "jump", "move", "scene");
        swarmRunState = ChooseStateByKeywords(swarmStates, "run", "walk", "crawl", "scene");

        if (blackController == null || swarmController == null)
        {
            Debug.LogError("[RatSetup] Could not build both rat controllers. Check the FBX import paths and animation clips.");
            return;
        }

        int blackCount = 0;
        int swarmCount = 0;
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject go in allObjects)
        {
            if (!IsTopLevelRat(go.transform, "blackrat") && !IsTopLevelRat(go.transform, "RAT"))
            {
                continue;
            }

            Animator anim = go.GetComponent<Animator>();
            if (anim == null)
            {
                anim = go.AddComponent<Animator>();
            }

            string fullPath = GetGameObjectPath(go);
            if (IsTopLevelRat(go.transform, "blackrat"))
            {
                anim.runtimeAnimatorController = blackController;
                anim.avatar = blackAvatar;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                RemoveOldProceduralAnimator(go);
                ConfigureBlackRat(go, blackIdleStates, blackAttackState);
                blackCount++;
                Debug.Log($"[RatSetup] Linked Black Rat: {fullPath}");
            }
            else
            {
                anim.runtimeAnimatorController = swarmController;
                anim.avatar = swarmAvatar;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                RemoveOldProceduralAnimator(go);
                ConfigureSwarmRat(go, swarmRunState);
                swarmCount++;
                Debug.Log($"[RatSetup] Linked Swarm Rat: {fullPath}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log($"[RatSetup] CLEAN SUCCESS! Assigned {blackCount} Black Rats and {swarmCount} Swarm Rats.");
        Debug.Log($"[RatSetup] Black controller states: {blackStates.Length}, attack state: {blackAttackState}, avatar: {(blackAvatar != null ? blackAvatar.name : "None")}");
        Debug.Log($"[RatSetup] Black state names: {string.Join(", ", blackStates)}");
        Debug.Log($"[RatSetup] Swarm controller states: {swarmStates.Length}, run state: {swarmRunState}, avatar: {(swarmAvatar != null ? swarmAvatar.name : "None")}");
    }

    static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }

    static AnimatorController GetOrCreateController(string path, string fbxPath, bool preferIdleClips, out string[] selectedStateNames, out string[] allStateNames)
    {
        selectedStateNames = new string[0];
        allStateNames = new string[0];
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        List<AnimationClip> clips = FindAnimationClips(fbxPath);
        if (clips.Count == 0)
        {
            Debug.LogWarning($"[RatSetup] No animation clips found in {fbxPath}.");
            return controller;
        }

        List<string> stateNameList = EnsureControllerHasClipStates(controller, clips);
        allStateNames = stateNameList.ToArray();
        selectedStateNames = preferIdleClips
            ? PickStatesByKeywords(stateNameList, "idle", "stand", "sniff", "eat", "pick", "nib", "groom", "look")
            : allStateNames;

        return controller;
    }

    static AnimatorController GetOrCreateController(string path, string fbxPath, bool preferIdleClips, out string[] selectedStateNames)
    {
        return GetOrCreateController(path, fbxPath, preferIdleClips, out selectedStateNames, out _);
    }

    static void ConfigureBlackRat(GameObject rat, string[] stateNames, string attackStateName)
    {
        BlackRatAnimationPlayer player = rat.GetComponent<BlackRatAnimationPlayer>();
        if (player == null)
        {
            player = rat.AddComponent<BlackRatAnimationPlayer>();
        }

        player.Configure(stateNames, attackStateName);
        EditorUtility.SetDirty(rat);
    }

    static void ConfigureSwarmRat(GameObject rat, string runStateName)
    {
        SwarmRatHallRunner runner = rat.GetComponent<SwarmRatHallRunner>();
        if (runner == null)
        {
            runner = rat.AddComponent<SwarmRatHallRunner>();
        }

        runner.Configure(runStateName);
        EditorUtility.SetDirty(rat);
    }

    static void RemoveOldProceduralAnimator(GameObject rat)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(rat);

        MonoBehaviour[] components = rat.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component != null && component.GetType().Name == "RatProceduralAnimator")
            {
                Object.DestroyImmediate(component);
            }
        }
    }

    static List<AnimationClip> FindAnimationClips(string fbxPath)
    {
        var clips = new List<AnimationClip>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in assets)
        {
            AnimationClip clip = asset as AnimationClip;
            if (clip == null)
            {
                continue;
            }

            if (clip.name.Contains("__preview__"))
            {
                continue;
            }

            clips.Add(clip);
        }

        return clips;
    }

    static Avatar FindAvatar(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object asset in assets)
        {
            Avatar avatar = asset as Avatar;
            if (avatar != null)
            {
                return avatar;
            }
        }

        Debug.LogWarning($"[RatSetup] No avatar found in {fbxPath}. The controller may assign, but rig animation may not play.");
        return null;
    }

    static List<string> EnsureControllerHasClipStates(AnimatorController controller, List<AnimationClip> clips)
    {
        var stateNames = new List<string>();
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;

        for (int i = 0; i < clips.Count; i++)
        {
            AnimationClip clip = clips[i];
            string stateName = MakeStateName(clip, i);
            AnimatorState state = FindState(stateMachine, stateName);
            if (state == null)
            {
                state = stateMachine.AddState(stateName);
            }

            state.motion = clip;
            state.writeDefaultValues = true;
            stateNames.Add(stateName);

            if (stateMachine.defaultState == null || i == 0)
            {
                stateMachine.defaultState = state;
            }
        }

        EditorUtility.SetDirty(controller);
        return stateNames;
    }

    static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            if (childState.state.name == stateName)
            {
                return childState.state;
            }
        }

        return null;
    }

    static string MakeStateName(AnimationClip clip, int index)
    {
        string rawName = string.IsNullOrWhiteSpace(clip.name) ? $"Clip_{index}" : clip.name;
        char[] chars = rawName.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return $"Rat_{index}_{new string(chars)}";
    }

    static string[] PickStatesByKeywords(List<string> stateNames, params string[] keywords)
    {
        var selected = new List<string>();
        foreach (string stateName in stateNames)
        {
            string lower = stateName.ToLowerInvariant();
            foreach (string keyword in keywords)
            {
                if (lower.Contains(keyword))
                {
                    selected.Add(stateName);
                    break;
                }
            }
        }

        return selected.Count > 0 ? selected.ToArray() : stateNames.ToArray();
    }

    static string ChooseStateByKeywords(string[] stateNames, params string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            foreach (string stateName in stateNames)
            {
                if (stateName.ToLowerInvariant().Contains(keyword))
                {
                    return stateName;
                }
            }
        }

        return stateNames.Length > 0 ? stateNames[0] : string.Empty;
    }

    static bool IsTopLevelRat(Transform current, string ratBaseName)
    {
        string objectName = current.name.Trim();
        if (!objectName.Equals(ratBaseName, System.StringComparison.OrdinalIgnoreCase) &&
            !objectName.StartsWith(ratBaseName + " (", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Transform parent = current.parent;
        if (parent == null)
        {
            return true;
        }

        string parentName = parent.name.Trim();
        return !parentName.Equals(ratBaseName, System.StringComparison.OrdinalIgnoreCase) &&
               !parentName.StartsWith(ratBaseName + " (", System.StringComparison.OrdinalIgnoreCase);
    }
}
