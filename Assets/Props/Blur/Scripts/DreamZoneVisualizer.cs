using UnityEngine;

/// <summary>
/// Renders the dream zone as an always-visible, fully-opaque dark cube,
/// surrounded by a dense particle "cloud" so it reads as a cloud wall.
/// Right-click → "Create Visual Box" to (re)generate the cube.
/// </summary>
[ExecuteAlways]
public class DreamZoneVisualizer : MonoBehaviour
{
    [Tooltip("Resize this to set your zone size. Collider and clouds update automatically.")]
    public Vector3 zoneSize = new Vector3(10f, 4f, 10f);

    [Tooltip("Solid color of the cloud wall (alpha is forced to 1).")]
    public Color cloudColor = new Color(0.03f, 0.03f, 0.04f, 1f);

    [Header("Particle cloud (always-on)")]
    [Tooltip("Particles emitted per second to keep the cloud thick.")]
    public float emissionRate = 25f;

    [Tooltip("Max simultaneous particles. Higher = denser fog, more cost.")]
    public int maxParticles = 400;

    [Tooltip("Per-particle start size.")]
    public float particleSize = 5f;

    [Tooltip("Per-particle alpha. Many overlapping particles compound to fully opaque.")]
    [Range(0f, 1f)]
    public float particleAlpha = 0.55f;

    [Tooltip("Particle spawn volume relative to zoneSize. >1 lets the haze extend beyond the solid wall for a soft gradient approach.")]
    public float particleSpread = 1.6f;

    private GameObject _visualBox;

    [ContextMenu("Create Visual Box")]
    public void CreateVisualBox()
    {
        var old = transform.Find("_ZoneVisual");
        if (old != null) DestroyImmediate(old.gameObject);

        _visualBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _visualBox.name = "_ZoneVisual";
        _visualBox.transform.SetParent(transform);
        _visualBox.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        _visualBox.transform.localRotation = Quaternion.identity;
        _visualBox.transform.localScale    = zoneSize;

        DestroyImmediate(_visualBox.GetComponent<Collider>());

        AssignOpaqueMaterial(_visualBox.GetComponent<Renderer>());

        SyncCollider();

        Debug.Log("Dream visual box (re)created.");
    }

    void Update()
    {
        var box = transform.Find("_ZoneVisual");
        if (box != null)
        {
            box.localScale    = zoneSize;
            box.localPosition = new Vector3(0f, zoneSize.y * 0.5f, 0f);

            var rend = box.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.enabled = true;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows    = false;
                AssignOpaqueMaterial(rend);
            }
        }
        SyncCollider();
        SyncClouds();
    }

    private const string WallShaderName = "Custom/DreamWall";

    private void AssignOpaqueMaterial(Renderer rend)
    {
        if (rend == null) return;

        // Force the wall to use the custom double-sided opaque shader so the player
        // can't see "through" the volume even when the camera is inside the cube.
        var sm = rend.sharedMaterial;
        bool needsNewMat = sm == null
                           || sm.shader == null
                           || sm.shader.name != WallShaderName;

        if (needsNewMat)
        {
            var shader = Shader.Find(WallShaderName);
            if (shader == null) return; // shader hasn't compiled yet
            var mat = new Material(shader) { name = "DreamWall" };
            mat.color = OpaqueCloudColor();
            rend.sharedMaterial = mat;
        }
        else if (rend.sharedMaterial.color != OpaqueCloudColor())
        {
            rend.sharedMaterial.color = OpaqueCloudColor();
        }
    }

    private Color OpaqueCloudColor()
    {
        var c = cloudColor;
        c.a = 1f;
        return c;
    }

    void SyncCollider()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;
        col.isTrigger = true;
        col.size      = zoneSize;
        col.center    = new Vector3(0f, zoneSize.y * 0.5f, 0f);
    }

    void SyncClouds()
    {
        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps == null) return;

        // Particles span a larger volume than the solid wall so the player
        // walks through thinning haze before hitting the opaque core.
        var shape = ps.shape;
        shape.scale = zoneSize * Mathf.Max(1f, particleSpread);

        var main = ps.main;
        main.maxParticles = maxParticles;
        main.startSize    = particleSize;
        main.playOnAwake  = true;
        main.startColor   = new Color(cloudColor.r, cloudColor.g, cloudColor.b, particleAlpha);

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;

        if (!ps.isPlaying) ps.Play();
    }
}
