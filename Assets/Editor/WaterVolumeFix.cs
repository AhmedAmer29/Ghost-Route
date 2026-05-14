using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class WaterVolumeFix
{
    [MenuItem("Tools/Fix Water Volume", false, 2)]
    static void FixWater()
    {
        GameObject water = GameObject.Find("RealisticSewerWater");
        if (water == null)
        {
            Debug.LogError("[WaterFix] No RealisticSewerWater found in scene.");
            return;
        }

        Debug.Log($"[WaterFix] Found '{water.name}' — replacing mesh with Cube...");

        MeshFilter mf = water.GetComponent<MeshFilter>();
        if (mf != null)
        {
            mf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        }

        water.transform.localScale = new Vector3(8f, 0.3f, 8f);

        Renderer r = water.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Custom/RealisticDirtyWater"));
            if (mat.shader == null)
            {
                Debug.LogWarning("[WaterFix] RealisticDirtyWater shader not found, falling back to Standard.");
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
        }

        BoxCollider col = water.GetComponent<BoxCollider>();
        if (col == null) col = water.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1f, 10f, 1f);

        if (water.GetComponent<RisingWater>() == null)
            water.AddComponent<RisingWater>();

        RisingWater rw = water.GetComponent<RisingWater>();
        if (rw != null)
        {
            SerializedObject so = new SerializedObject(rw);
            so.FindProperty("riseDuration").floatValue = 600f;
            so.FindProperty("maxHeight").floatValue = 20f;
            so.ApplyModifiedProperties();
            Debug.Log("[WaterFix] Set riseDuration=600s, maxHeight=20m.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[WaterFix] Done! Water will flood the sewer over 10 minutes.");
    }

    [MenuItem("Tools/Fix Water Duration", false, 3)]
    static void FixDuration()
    {
        RisingWater rw = Object.FindObjectOfType<RisingWater>();
        if (rw == null)
        {
            Debug.LogError("[WaterFix] No RisingWater component found in scene.");
            return;
        }

        SerializedObject so = new SerializedObject(rw);
        so.FindProperty("riseDuration").floatValue = 600f;
        so.FindProperty("maxHeight").floatValue = 20f;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[WaterFix] Set riseDuration=600s, maxHeight=20m (fills sewer in 10 min).");
    }
}
