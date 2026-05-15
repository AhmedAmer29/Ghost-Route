using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the Door GameObject.
/// Fully editable from the Inspector — add/remove/reorder lines freely.
/// </summary>
public class LockedDoor : MonoBehaviour
{
    [Header("Interaction")]
    public float  interactRadius = 3f;
    public KeyCode interactKey   = KeyCode.E;
    public string  promptText    = "Open door";

    [Header("Dialogue — edit freely in Inspector")]
    [Tooltip("Lines play in order. Each line has its own on-screen duration.")]
    public List<DialogueLine> lines = new List<DialogueLine>
    {
        new DialogueLine { text = "Locked.",                                             duration = 1.6f },
        new DialogueLine { text = "Of course it's locked... why would it be open?",     duration = 4f   },
    };

    // ─────────────────────────────────────────────────────────────────────

    private Transform _player;
    private bool      _inRange;
    private bool      _interacted;

    void Update()
    {
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
            else return;
        }

        // XZ-only distance (door may be elevated on a wall)
        Vector3 flat = new Vector3(transform.position.x, _player.position.y, transform.position.z);
        float dist   = Vector3.Distance(flat, _player.position);
        bool  near   = dist <= interactRadius;

        if (near && !_inRange)
        {
            _inRange = true;
            GetManager().ShowPrompt(interactKey.ToString(), promptText);
        }
        else if (!near && _inRange)
        {
            _inRange    = false;
            _interacted = false;
            GetManager().HidePrompt();
        }

        if (_inRange && !_interacted && Input.GetKeyDown(interactKey))
        {
            _interacted = true;
            GetManager().HidePrompt();
            PlayLines();
        }
    }

    void PlayLines()
    {
        if (lines == null || lines.Count == 0) return;

        string[] texts = new string[lines.Count];
        float[]  durs  = new float [lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            texts[i] = lines[i].text;
            durs [i] = lines[i].duration;
        }

        GetManager().ShowDialogue(texts, durs, onDone: () => _interacted = false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    static InteractionManager GetManager()
    {
        if (InteractionManager.Instance != null) return InteractionManager.Instance;
        var go = new GameObject("InteractionManager");
        return go.AddComponent<InteractionManager>();
    }
}

// ── Shared dialogue line type ─────────────────────────────────────────────
[System.Serializable]
public class DialogueLine
{
    [TextArea(1, 4)] public string text     = "";
    [Tooltip("How long this line stays on screen (seconds).")]
    public float duration = 3f;
}
