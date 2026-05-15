using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the VentDoor GameObject.
/// All dialogue lines are editable in the Inspector — add, remove, reorder freely.
/// After the last line the screen fades to black and the next scene loads.
/// </summary>
public class VentDoorInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float   interactRadius = 3.5f;
    public KeyCode interactKey    = KeyCode.E;
    public string  promptText     = "Go through vent";

    [Header("Dialogue — edit freely in Inspector")]
    [Tooltip("Lines play in order, one after another, with typewriter effect.")]
    public List<DialogueLine> lines = new List<DialogueLine>
    {
        new DialogueLine { text = "Only way inside the bank... is through the vents.", duration = 3.8f },
        new DialogueLine { text = "The Ghost took this route.",                        duration = 3f   },
        new DialogueLine { text = "I shall too.",                                      duration = 2.2f },
    };

    [Header("Scene Transition")]
    public string nextScene      = "Scene4";
    public float  pauseAfterLast = 0.4f;
    public float  fadeDuration   = 2f;

    [Header("Player")]
    [Tooltip("Disable player movement during the cutscene.")]
    public bool freezePlayerDuringDialogue = true;

    // ─────────────────────────────────────────────────────────────────────

    private Transform     _player;
    private PlayerMovement _movement;
    private bool          _inRange;
    private bool          _triggered;

    void Update()
    {
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            _player   = go.transform;
            _movement = go.GetComponent<PlayerMovement>();
        }

        if (_triggered) return;

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
            _inRange = false;
            GetManager().HidePrompt();
        }

        if (_inRange && Input.GetKeyDown(interactKey))
        {
            _triggered = true;
            GetManager().HidePrompt();

            if (freezePlayerDuringDialogue && _movement != null)
                _movement.enabled = false;

            PlaySequence();
        }
    }

    void PlaySequence()
    {
        if (lines == null || lines.Count == 0)
        {
            GetManager().FadeToScene(nextScene, pauseAfterLast, fadeDuration);
            return;
        }

        string[] texts = new string[lines.Count];
        float[]  durs  = new float [lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            texts[i] = lines[i].text;
            durs [i] = lines[i].duration;
        }

        GetManager().ShowDialogue(texts, durs, onDone: () =>
        {
            GetManager().FadeToScene(nextScene, pauseAfterLast, fadeDuration);
        });
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    static InteractionManager GetManager()
    {
        if (InteractionManager.Instance != null) return InteractionManager.Instance;
        var go = new GameObject("InteractionManager");
        return go.AddComponent<InteractionManager>();
    }
}
