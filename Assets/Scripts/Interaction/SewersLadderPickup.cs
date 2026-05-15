using UnityEngine;

/// <summary>
/// Attach to the Sewers GameObject.
/// Prompt is invisible until Activate() is called by LadderPromptInteraction.
/// Once active: shows "Grab Ladder". On interact: gives player the ladder,
/// then tells LadderPromptInteraction to advance to "Place Ladder".
/// After grabbing, shows a funny response if the player tries again.
/// </summary>
public class SewersLadderPickup : MonoBehaviour
{
    [Header("Interaction")]
    public float   interactRadius = 3f;
    public KeyCode interactKey    = KeyCode.E;
    public string  promptText     = "Grab Ladder";

    [Header("Dialogue — Grab")]
    [TextArea] public string grabLine     = "Found it. Now let's get that ladder up.";
    public float             grabDuration = 3f;

    [Header("Dialogue — Already Grabbed (funny)")]
    [TextArea] public string alreadyGrabbedLine =
        "I already took it. What am I gonna do, take it again?";
    public float alreadyGrabbedDuration = 3.5f;

    [HideInInspector] public LadderPromptInteraction ladderPrompt;

    // ─────────────────────────────────────────────────────────────────────
    private Transform _player;
    private bool      _active;
    private bool      _inRange;
    private bool      _grabbed;

    /// Exposed so LadderPromptInteraction can yield the prompt when player is here.
    public bool IsInRange => _inRange && _active;

    /// Called by LadderPromptInteraction after the Examine interaction.
    public void Activate()
    {
        _active  = true;
        _grabbed = false;
    }

    void Update()
    {
        if (!_active) return;

        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            _player = go.transform;
        }

        float dist = Vector3.Distance(
            new Vector3(transform.position.x, _player.position.y, transform.position.z),
            _player.position);
        bool near = dist <= interactRadius;

        if (near && !_inRange)
        {
            _inRange = true;
            GetManager().ShowPrompt(interactKey.ToString(), promptText);
        }
        else if (near && _inRange)
        {
            // Keep prompt refreshed (handles state-label changes if any)
            GetManager().ShowPrompt(interactKey.ToString(), promptText);
        }
        else if (!near && _inRange)
        {
            _inRange = false;
            GetManager().HidePrompt();
        }

        if (_inRange && Input.GetKeyDown(interactKey))
        {
            if (!_grabbed)
            {
                // First interaction — grab the ladder
                _grabbed = true;
                PlayerInventory.HasLadder = true;

                GetManager().ShowNotification(grabLine, grabDuration);

                if (ladderPrompt != null)
                    ladderPrompt.AdvanceToPlaceLadder();
                else
                    Debug.LogWarning("[SewersLadderPickup] ladderPrompt reference is null — " +
                                     "wire it in the Inspector or via execute_code.");
            }
            else
            {
                // Repeat interaction — funny response, no re-grab
                GetManager().ShowNotification(alreadyGrabbedLine, alreadyGrabbedDuration);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.4f, 0.1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    static InteractionManager GetManager()
    {
        if (InteractionManager.Instance != null) return InteractionManager.Instance;
        return new GameObject("InteractionManager").AddComponent<InteractionManager>();
    }
}
