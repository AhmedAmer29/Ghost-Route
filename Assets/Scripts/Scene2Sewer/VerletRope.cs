using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VerletRope.cs
/// Creates a dynamic rope using Verlet Integration physics.
/// Attach this to an empty GameObject with a LineRenderer component.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // INSPECTOR SETTINGS
    // ─────────────────────────────────────────────────────────────
    [Header("Rope Configuration")]
    [Tooltip("Total number of simulation nodes in the rope.")]
    public int nodeCount = 24;

    [Tooltip("Resting length between each adjacent node (meters).")]
    public float segmentLength = 0.25f;

    [Tooltip("How many constraint-solving passes per physics step. Higher = stiffer rope.")]
    [Range(5, 40)]
    public int constraintIterations = 20;

    [Header("Physics")]
    [Tooltip("Gravity acceleration applied to each node per step.")]
    public Vector3 gravity = new Vector3(0f, -9.81f, 0f);

    [Tooltip("Velocity damping factor (0 = no damping, 1 = full stop).")]
    [Range(0f, 1f)]
    public float damping = 0.02f;

    [Header("Visuals")]
    [Tooltip("Width of the LineRenderer at the start of the rope.")]
    public float ropeStartWidth = 0.06f;

    [Tooltip("Width of the LineRenderer at the end of the rope.")]
    public float ropeEndWidth = 0.04f;

    // ─────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────

    /// <summary>Current world-space positions of each Verlet node.</summary>
    private List<Vector3> _positions = new List<Vector3>();

    /// <summary>Previous positions used to compute implicit velocity.</summary>
    private List<Vector3> _prevPositions = new List<Vector3>();

    /// <summary>Which nodes are pinned (cannot move).</summary>
    private List<bool> _pinned = new List<bool>();

    private LineRenderer _lineRenderer;
    private bool _isDeployed = false;

    // ─────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.startWidth = ropeStartWidth;
        _lineRenderer.endWidth = ropeEndWidth;
        _lineRenderer.positionCount = nodeCount;

        InitialiseNodes(transform.position);
    }

    void FixedUpdate()
    {
        if (!_isDeployed) return;

        Simulate();
        for (int i = 0; i < constraintIterations; i++)
            ApplyConstraints();

        // Push current positions to the LineRenderer
        for (int i = 0; i < nodeCount; i++)
            _lineRenderer.SetPosition(i, _positions[i]);
    }

    // ─────────────────────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Lay all nodes out in a straight vertical line from <paramref name="origin"/>.
    /// </summary>
    // Made internal so DeployRope can call it when invoked from Editor scripts
    internal void InitialiseNodes(Vector3 origin)
    {
        _positions.Clear();
        _prevPositions.Clear();
        _pinned.Clear();

        for (int i = 0; i < nodeCount; i++)
        {
            Vector3 pos = origin + Vector3.down * (i * segmentLength);
            _positions.Add(pos);
            _prevPositions.Add(pos);
            _pinned.Add(false);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Instantly deploy the rope: pins node[0] at <paramref name="start"/> and
    /// teleports node[last] to <paramref name="impactPoint"/>, then enables physics.
    /// </summary>
    public void DeployRope(Vector3 start, Vector3 impactPoint)
    {
        // Guard: if called from an Editor script, Awake() hasn't run yet.
        // Re-initialise the lists and grab the LineRenderer defensively.
        if (_positions.Count != nodeCount)
        {
            InitialiseNodes(start);
        }
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.startWidth = ropeStartWidth;
            _lineRenderer.endWidth   = ropeEndWidth;
            _lineRenderer.positionCount = nodeCount;
        }

        // Distribute nodes in a curve (initial sag) between start and impactPoint
        float sagIntensity = 0.5f; // Initial visual sag
        for (int i = 0; i < nodeCount; i++)
        {
            float t = (float)i / (nodeCount - 1);
            Vector3 pos = Vector3.Lerp(start, impactPoint, t);
            
            // Add initial sag so it doesn't look like a plank
            float sag = Mathf.Sin(t * Mathf.PI) * sagIntensity;
            pos.y -= sag;

            _positions[i]     = pos;
            _prevPositions[i] = pos;
            _pinned[i]        = false;
        }

        // Pin both ends so the rope hangs between them
        _pinned[0]              = true;
        _pinned[nodeCount - 1]  = true;

        _isDeployed = true;
        _lineRenderer.enabled = true;
    }

    /// <summary>
    /// Returns the interpolated world position along the rope corresponding to
    /// <paramref name="progressPercent"/> (0 = start, 100 = end).
    /// </summary>
    public Vector3 GetPositionAtProgress(float progressPercent)
    {
        progressPercent = Mathf.Clamp(progressPercent, 0f, 100f);
        float normalized = progressPercent / 100f;           // 0..1
        float floatIndex = normalized * (nodeCount - 1);     // e.g. 12.4
        int lower = Mathf.FloorToInt(floatIndex);
        int upper = Mathf.Min(lower + 1, nodeCount - 1);
        float frac = floatIndex - lower;

        return Vector3.Lerp(_positions[lower], _positions[upper], frac);
    }

    /// <summary>Manually override the position of a specific node (e.g. player anchor).</summary>
    public void SetNodePosition(int index, Vector3 worldPos)
    {
        if (index < 0 || index >= nodeCount) return;
        _positions[index] = worldPos;
        _prevPositions[index] = worldPos;
    }

    /// <summary>Pin or unpin a specific node.</summary>
    public void SetNodePinned(int index, bool pinned)
    {
        if (index < 0 || index >= nodeCount) return;
        _pinned[index] = pinned;
    }

    // ─────────────────────────────────────────────────────────────
    // VERLET SIMULATION
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verlet integration step: moves each free node based on its implicit
    /// velocity (current - previous position) and applies gravity.
    /// </summary>
    private void Simulate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 gravityStep = gravity * dt * dt;
        float dampFactor = 1f - damping;

        for (int i = 0; i < nodeCount; i++)
        {
            if (_pinned[i]) continue;

            Vector3 current = _positions[i];
            Vector3 prev    = _prevPositions[i];

            // velocity = current - prev (implicit in Verlet)
            Vector3 velocity = (current - prev) * dampFactor;

            _prevPositions[i] = current;
            _positions[i]     = current + velocity + gravityStep;
        }
    }

    /// <summary>
    /// Distance constraint pass: iterates every adjacent pair of nodes and
    /// pushes them apart/together until they honour <see cref="segmentLength"/>.
    /// Pinned nodes absorb the entire correction.
    /// </summary>
    private void ApplyConstraints()
    {
        for (int i = 0; i < nodeCount - 1; i++)
        {
            Vector3 a = _positions[i];
            Vector3 b = _positions[i + 1];

            float dist  = Vector3.Distance(a, b);
            if (dist < 0.0001f) continue;          // avoid divide-by-zero

            float error = (dist - segmentLength) / dist;
            Vector3 correction = (b - a) * (error * 0.5f);

            bool aPinned = _pinned[i];
            bool bPinned = _pinned[i + 1];

            if (!aPinned && !bPinned)
            {
                _positions[i]     += correction;
                _positions[i + 1] -= correction;
            }
            else if (!aPinned)
            {
                _positions[i] += correction * 2f;   // b is pinned, a absorbs all
            }
            else if (!bPinned)
            {
                _positions[i + 1] -= correction * 2f; // a is pinned, b absorbs all
            }
            // If both are pinned, do nothing.
        }
    }
}
