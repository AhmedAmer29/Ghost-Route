using UnityEngine;
using UnityEditor;

public class CreateDirtyWaterPrefab : EditorWindow
{
    [MenuItem("Sewer Tools/Create Dirty Water Prefab")]
    public static void CreateWater()
    {
        // Create the Plane
        GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "DirtySewerWater";
        
        // Remove the collider
        DestroyImmediate(waterPlane.GetComponent<MeshCollider>());

        // Create the Material
        Material dirtyWaterMat = new Material(Shader.Find("Standard"));
        
        // Setup Dirty Water Look (Brownish green)
        dirtyWaterMat.color = new Color(0.3f, 0.4f, 0.1f, 0.8f);
        dirtyWaterMat.SetFloat("_Mode", 3); // Transparent mode
        
        // Apply Transparency settings
        dirtyWaterMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        dirtyWaterMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        dirtyWaterMat.SetInt("_ZWrite", 0);
        dirtyWaterMat.DisableKeyword("_ALPHATEST_ON");
        dirtyWaterMat.DisableKeyword("_ALPHABLEND_ON");
        dirtyWaterMat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        dirtyWaterMat.renderQueue = 3000;

        // Smoothness
        dirtyWaterMat.SetFloat("_Glossiness", 0.85f);

        // Save the material to assets
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }
        
        AssetDatabase.CreateAsset(dirtyWaterMat, "Assets/Materials/DirtyWaterMat.mat");

        // Apply material to plane
        waterPlane.GetComponent<Renderer>().sharedMaterial = dirtyWaterMat;

        // Add WaterScroll script (if it exists)
        if (System.Type.GetType("WaterScroll, Assembly-CSharp") != null)
        {
            waterPlane.AddComponent(System.Type.GetType("WaterScroll, Assembly-CSharp"));
        }

        // Parent it to a useful scale
        waterPlane.transform.localScale = new Vector3(10, 1, 10);
        
        Debug.Log("Dirty Water object created in the scene! You can drag it into your Project window to make it a Prefab.");
    }
}
