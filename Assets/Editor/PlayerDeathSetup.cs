using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class PlayerDeathSetup : EditorWindow
{
    [MenuItem("Ghost Route/Setup Player Death Animation")]
    public static void SetupDeath()
    {
        string animPath = "Assets/Criminal/Falling Down.fbx";
        string controllerPath = "Assets/Criminal/Criminal Animator.controller";

        // 1. Fix Rig to Humanoid
        ModelImporter importer = AssetImporter.GetAtPath(animPath) as ModelImporter;
        if (importer != null)
        {
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.SaveAndReimport();
                Debug.Log("[Setup] 'Falling Down' set to Humanoid.");
            }
        }
        else
        {
            Debug.LogError("[Setup] Could not find 'Falling Down.fbx' at " + animPath);
            return;
        }

        // 2. Setup Animator
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            // Add Trigger
            bool triggerExists = false;
            foreach (var param in controller.parameters)
            {
                if (param.name == "RatDeath") { triggerExists = true; break; }
            }
            if (!triggerExists) controller.AddParameter("RatDeath", AnimatorControllerParameterType.Trigger);

            // Add State
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            
            // Check if state already exists
            ChildAnimatorState deathStateWrapper = new ChildAnimatorState();
            bool stateExists = false;
            foreach (var s in rootStateMachine.states)
            {
                if (s.state.name == "RatDeath_State") { stateExists = true; deathStateWrapper = s; break; }
            }

            if (!stateExists)
            {
                var state = rootStateMachine.AddState("RatDeath_State");
                var motion = AssetDatabase.LoadAssetAtPath<Motion>(animPath);
                
                // Mixamo animations are often nested, let's find the actual clip
                if (motion == null)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(animPath);
                    foreach (var asset in assets)
                    {
                        if (asset is AnimationClip && !asset.name.Contains("__preview__"))
                        {
                            state.motion = (AnimationClip)asset;
                            break;
                        }
                    }
                }
                else
                {
                    state.motion = motion;
                }

                // Add Transition from Any State
                var transition = rootStateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.If, 0, "RatDeath");
                transition.duration = 0.25f;
                transition.canTransitionToSelf = false;

                Debug.Log("[Setup] Animator configured with 'RatDeath' state and transition.");
            }
            else
            {
                Debug.Log("[Setup] Animator already has 'RatDeath_State'.");
            }
        }
        else
        {
            Debug.LogError("[Setup] Could not find 'Criminal Animator.controller' at " + controllerPath);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Setup Complete", "Player Death Animation is now configured!", "Great!");
    }
}
