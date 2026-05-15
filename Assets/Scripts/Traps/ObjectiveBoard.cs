using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the Meshy_AI board object.
/// Tracks two objectives:
///   1. Overload 3 real electrical boxes  (reads from MasterPowerSystem)
///   2. Pick up the KEY01 object          (detects when it disappears from the scene)
/// When BOTH are done, the lever is unlocked.
/// </summary>
public class ObjectiveBoard : MonoBehaviour
{
    [Header("References")]
    public Text              circuitText;
    public Text              keyText;
    public MasterPowerSystem masterSystem;
    public LeverInteraction  lever;          // Drag Lever Switch here (or auto-found)

    // KEY01 is detected by watching if the GameObject disappears from the scene
    private GameObject _keyObj;
    private bool       _hasKey      = false;
    private bool       _circuitsDone = false;
    private bool       _leverUnlocked = false;

    void Start()
    {
        // Auto-find references if not set in inspector
        if (masterSystem == null)
            masterSystem = FindObjectOfType<MasterPowerSystem>();

        if (lever == null)
            lever = FindObjectOfType<LeverInteraction>();

        // Find KEY01 — we watch for it to disappear (picked up / destroyed)
        _keyObj = GameObject.Find("KEY01");
        if (_keyObj == null)
            Debug.LogWarning("[ObjectiveBoard] Could not find 'KEY01' in scene. Key objective will not track.");

        RefreshUI();
    }

    void Update()
    {
        CheckCircuits();
        CheckKey();
        TryUnlockLever();
        RefreshUI();
    }

    // ── Circuits ──────────────────────────────────────────────────────────────
    void CheckCircuits()
    {
        if (_circuitsDone) return;
        if (masterSystem == null) return;
        if (masterSystem.fixedCount >= masterSystem.targetCount)
            _circuitsDone = true;
    }

    // ── Key ───────────────────────────────────────────────────────────────────
    void CheckKey()
    {
        if (_hasKey) return;

        // KEY01 was found at start — mark collected when it disappears
        if (_keyObj != null && !_keyObj.activeInHierarchy)
        {
            _hasKey = true;
            return;
        }

        // KEY01 was already gone at start (already picked up before board loaded)
        if (_keyObj == null)
            _hasKey = true;
    }

    // Public call — your key pickup script can call this directly
    public void SetKeyFound() => _hasKey = true;

    // ── Lever Gate ────────────────────────────────────────────────────────────
    void TryUnlockLever()
    {
        if (_leverUnlocked) return;
        if (!_circuitsDone || !_hasKey) return;

        _leverUnlocked = true;
        if (lever != null)
        {
            lever.Unlock();
            Debug.Log("<color=green>[ObjectiveBoard] All objectives complete! Lever unlocked.</color>");
        }
    }

    // ── UI ────────────────────────────────────────────────────────────────────
    void RefreshUI()
    {
        if (circuitText != null)
        {
            int count = masterSystem != null ? masterSystem.fixedCount  : 0;
            int total = masterSystem != null ? masterSystem.targetCount : 3;

            if (_circuitsDone)
            {
                circuitText.text  = "✔ ELECTRICAL COMPONENTS: DONE";
                circuitText.color = Color.green;
            }
            else
            {
                circuitText.text  = $"✖ ELECTRICAL COMPONENTS: {count}/{total}";
                circuitText.color = Color.white;
            }
        }

        if (keyText != null)
        {
            if (_hasKey)
            {
                keyText.text  = "✔ KEY: ACQUIRED";
                keyText.color = Color.green;
            }
            else
            {
                keyText.text  = "✖ KEY: NEEDED";
                keyText.color = new Color(1f, 0.3f, 0.3f); // red
            }
        }
    }
}
