using UnityEngine;

/// <summary>
/// Attach to the DreamClouds Particle System child of DreamZone.
/// Right-click this component → "Setup Dream Clouds" to auto-configure everything.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class DreamCloudSetup : MonoBehaviour
{
    [Tooltip("Assign the DreamCloud material (Custom/DreamCloud shader).")]
    public Material cloudMaterial;

    [Tooltip("Size of the dream zone — automatically applied to parent BoxCollider too.")]
    public Vector3 zoneSize = new Vector3(10f, 4f, 10f);

    [ContextMenu("Setup Dream Clouds")]
    public void Setup()
    {
        var ps   = GetComponent<ParticleSystem>();
        var rend = GetComponent<ParticleSystemRenderer>();

        // ── Auto-size parent BoxCollider ──────────────────────────────────────
        if (transform.parent != null)
        {
            var col = transform.parent.GetComponent<BoxCollider>();
            if (col != null)
            {
                col.size      = zoneSize;
                col.center    = new Vector3(0f, zoneSize.y * 0.5f, 0f);
                col.isTrigger = true;
                Debug.Log("BoxCollider auto-sized to " + zoneSize);
            }
        }

        // ── Main ─────────────────────────────────────────────────────────────
        var main = ps.main;
        main.loop            = true;
        main.playOnAwake     = true;   // always visible inside the zone
        main.duration        = 8f;
        main.maxParticles    = 40;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed    = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startSize     = new ParticleSystem.MinMaxCurve(3f, 7f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.78f, 0.88f, 1.00f, 0.55f),
            new Color(0.95f, 0.97f, 1.00f, 0.35f));

        // ── Emission ─────────────────────────────────────────────────────────
        var em = ps.emission;
        em.enabled      = true;
        em.rateOverTime = 2.5f;

        // ── Shape — box matching zone size ────────────────────────────────────
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = zoneSize;

        // ── Velocity over Lifetime — slow dreamy drift ────────────────────────
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.Local;
        vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        vel.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.02f);
        vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        // ── Size over Lifetime — grow then shrink ─────────────────────────────
        var sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve bell = new AnimationCurve(
            new Keyframe(0f,   0f,  0f,  2f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.8f, 1f),
            new Keyframe(1f,   0f, -2f,  0f));
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, bell);

        // ── Color over Lifetime — fade in / out ───────────────────────────────
        var colorOL = ps.colorOverLifetime;
        colorOL.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.82f, 0.90f, 1f), 0f),
                new GradientColorKey(new Color(0.95f, 0.97f, 1f), 0.5f),
                new GradientColorKey(new Color(0.82f, 0.90f, 1f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f,    0f),
                new GradientAlphaKey(0.55f, 0.2f),
                new GradientAlphaKey(0.55f, 0.8f),
                new GradientAlphaKey(0f,    1f)
            });
        colorOL.color = new ParticleSystem.MinMaxGradient(grad);

        // ── Renderer ──────────────────────────────────────────────────────────
        rend.renderMode      = ParticleSystemRenderMode.Billboard;
        rend.sortMode        = ParticleSystemSortMode.Distance;
        rend.maxParticleSize = 0.5f;
        if (cloudMaterial != null)
            rend.material = cloudMaterial;
        else
            Debug.LogWarning("No Cloud Material assigned — assign DreamCloud material in the Inspector.");

        Debug.Log("DreamClouds fully configured!");
    }
}
