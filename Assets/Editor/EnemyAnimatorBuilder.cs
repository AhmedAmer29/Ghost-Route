using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class EnemyAnimatorBuilder : EditorWindow
{
    [MenuItem("Tools/Build Enemy Animator")]
    public static void BuildAnimator()
    {
        string path = "Assets/EnemyController.controller";
        
        // 1. Create the Animator Controller
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        // 2. Add Parameters
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("StartRunning", AnimatorControllerParameterType.Trigger);

        // 3. Load Animation Clips
        // Note: Paths are based on your description (Assets/people/)
        AnimationClip sittingIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/people/Sitting Idle.fbx");
        AnimationClip standingUp = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/people/Standing Up.fbx");
        AnimationClip running = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/people/Running (1).fbx");
        AnimationClip flyingKnee = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/people/Flying knee kick punch.fbx");
        AnimationClip fightingIdle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/people/Fighting Idle.fbx");

        // Fallback search if specific FBX paths fail
        if (sittingIdle == null) sittingIdle = FindClip("Sitting Idle");
        if (standingUp == null) standingUp = FindClip("Standing Up");
        if (running == null) running = FindClip("Running (1)");
        if (flyingKnee == null) flyingKnee = FindClip("Flying knee kick punch");
        if (fightingIdle == null) fightingIdle = FindClip("Fighting Idle");

        // 4. Create States
        var rootStateMachine = controller.layers[0].stateMachine;

        var stateSitting = rootStateMachine.AddState("Sitting Idle");
        stateSitting.motion = sittingIdle;

        var stateStanding = rootStateMachine.AddState("Standing Up");
        stateStanding.motion = standingUp;

        var stateRunning = rootStateMachine.AddState("Running");
        stateRunning.motion = running;

        var stateAttack = rootStateMachine.AddState("Flying Knee Kick");
        stateAttack.motion = flyingKnee;

        var stateFightingIdle = rootStateMachine.AddState("Fighting Idle");
        stateFightingIdle.motion = fightingIdle;

        // 5. Set Default State
        rootStateMachine.defaultState = stateSitting;

        // 6. Create Transitions
        
        // Sitting Idle -> Standing Up
        var t1 = stateSitting.AddTransition(stateStanding);
        t1.hasExitTime = false;
        t1.AddCondition(AnimatorConditionMode.If, 0, "StartRunning");

        // Standing Up -> Running
        var t2 = stateStanding.AddTransition(stateRunning);
        t2.hasExitTime = false;
        t2.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

        // Running -> Flying Knee Kick
        var t3 = stateRunning.AddTransition(stateAttack);
        t3.hasExitTime = false;
        t3.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        // Flying Knee Kick -> Fighting Idle
        var t4 = stateAttack.AddTransition(stateFightingIdle);
        t4.hasExitTime = true;
        t4.exitTime = 0.9f; // Auto-transition near the end

        AssetDatabase.SaveAssets();
        Debug.Log("EnemyController created successfully at " + path);
        Selection.activeObject = controller;
    }

    private static AnimationClip FindClip(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:AnimationClip");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }
        return null;
    }
}
