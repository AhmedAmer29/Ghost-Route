using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MaterialBatchAssigner : MonoBehaviour
{
    [Header("Settings")]
    public string materialPath = "Assets/Models/SewerProps/electrical box/source/electrical boxes/Materials/electrical boxes_Base_Color.mat";
    public Material targetMaterial;

    [ContextMenu("Apply Material To Children")]
    public void ApplyMaterialToChildren()
    {
#if UNITY_EDITOR
        if (targetMaterial == null)
        {
            targetMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        }
#endif

        if (targetMaterial == null)
        {
            Debug.LogError($"[MaterialBatchAssigner] Material not found at path: {materialPath}. Please assign it manually in the inspector.");
            return;
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);
        int count = 0;

        foreach (var renderer in renderers)
        {
            // Apply to all material slots
            Material[] mats = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = targetMaterial;
            }
            renderer.sharedMaterials = mats;
            count++;

#if UNITY_EDITOR
            EditorUtility.SetDirty(renderer);
#endif
        }

        Debug.Log($"[MaterialBatchAssigner] Successfully applied material to {count} child objects.");
    }
}
