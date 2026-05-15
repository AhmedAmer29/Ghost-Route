using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ProceduralRope : MonoBehaviour
{
    [Header("Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public int segments = 20;
    public float thickness = 0.1f;
    public float sagAmount = 0.5f;

    [Header("Animation")]
    public float swaySpeed = 1.0f;
    public float swayAmount = 0.2f;

    private LineRenderer _line;

    void Start()
    {
        _line = GetComponent<LineRenderer>();
        _line.positionCount = segments + 1;
        _line.startWidth = thickness;
        _line.endWidth = thickness;

        // Apply Rope Material
        string matPath = "Assets/Materials/Sewer/Rope_Mat.mat";
        Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Sewer/Rope_Albedo.png");
            if (tex != null) mat.mainTexture = tex;
            UnityEditor.AssetDatabase.CreateAsset(mat, matPath);
        }
        _line.material = mat;
    }

    void Update()
    {
        if (startPoint == null || endPoint == null) return;

        UpdateRopePositions();
    }

    void UpdateRopePositions()
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 pos = Vector3.Lerp(startPoint.position, endPoint.position, t);

            // Add Sag (Catenary-like curve)
            float sag = Mathf.Sin(t * Mathf.PI) * sagAmount;
            pos.y -= sag;

            // Add Animated Sway
            float sway = Mathf.Sin(Time.time * swaySpeed + t * 2f) * swayAmount;
            pos.x += sway;

            _line.SetPosition(i, pos);
        }
    }
}
