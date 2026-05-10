using UnityEditor;
using UnityEngine;

public static class SewerEnemySetupTool
{
    private const string ControllerPath = "Assets/people/EnemyController.controller";

    [MenuItem("Tools/Sewer Tools/SET UP SEWER ENEMY FROM SELECTION")]
    public static void SetupSelectedEnemy()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("Sewer Enemy Setup", "Select the enemy root GameObject in the Hierarchy first.", "OK");
            return;
        }

        Animator animator = selectedObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = selectedObject.AddComponent<Animator>();
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Sewer Enemy Setup", $"Could not find {ControllerPath}. Make sure EnemyController.controller exists.", "OK");
            return;
        }

        animator.runtimeAnimatorController = controller;

        SewerEnemyAI ai = selectedObject.GetComponent<SewerEnemyAI>();
        if (ai == null)
        {
            ai = selectedObject.AddComponent<SewerEnemyAI>();
        }

        if (Camera.main != null)
        {
            ai.player = Camera.main.transform;
        }

        ai.triggerDistance = 10f;
        ai.attackDistance = 1.8f;
        ai.turnSpeed = 5f;

        Selection.activeGameObject = selectedObject;
        EditorUtility.SetDirty(selectedObject);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(ai);

        Debug.Log($"Sewer enemy setup applied to '{selectedObject.name}'. Animator controller assigned and AI added.");
    }
}