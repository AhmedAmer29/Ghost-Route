using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the LadderPrompt GameObject.
///
/// State machine:
///   0 — Examine   → dialogue → unlocks Sewers pickup
///   1 — Place Ladder → dialogue → shows Ladder object
///   2 — Climb     → fade to black → teleport to SpawnPoint2 → fade in
/// </summary>
public class LadderPromptInteraction : MonoBehaviour
{
    [Header("Interaction")]
    public float   interactRadius = 3f;
    public KeyCode interactKey    = KeyCode.E;

    [Header("References")]
    public SewersLadderPickup sewersPickup;   // assign in Inspector or auto-wired
    public GameObject         ladderObject;   // shown when ladder is placed
    public Transform          spawnPoint2;    // teleport destination after climbing

    [Header("State 0 — Examine")]
    public string examinePrompt   = "Examine";
    [TextArea] public string examineDialogue = "I need something to use to get up there...";
    public float examineDialogueDuration     = 3.5f;

    [Header("State 1 — Place Ladder")]
    public string placeLadderPrompt   = "Place Ladder";
    [TextArea] public string placeDialogue = "That should hold. Time to go up.";
    public float placeDialogueDuration     = 3f;

    [Header("State 2 — Climb")]
    public string climbPrompt = "Climb";
    public float  fadeOutDuration = 1.2f;
    public float  holdDuration    = 1f;
    public float  fadeInDuration  = 1f;

    // ─────────────────────────────────────────────────────────────────────
    // 0 = Examine, 1 = Place Ladder, 2 = Climb
    private int       _state = 0;
    private Transform _player;
    private bool      _inRange;
    private bool      _busy;   // prevent double-triggers during dialogue

    void Start()
    {
        if (ladderObject != null)
            ladderObject.SetActive(false);
    }

    void Update()
    {
        if (_busy) return;

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

        // Yield prompt ownership to SewersLadderPickup when player is near it.
        // Both objects share one prompt slot; Sewers takes priority while in range.
        bool sewersOwnsPrompt = sewersPickup != null && sewersPickup.IsInRange;

        if (near && !_inRange)
        {
            _inRange = true;
            if (!sewersOwnsPrompt)
                GetManager().ShowPrompt(interactKey.ToString(), CurrentPrompt());
        }
        else if (near && _inRange)
        {
            // Refresh every frame so state changes are reflected immediately,
            // but only when Sewers isn't already showing its own prompt.
            if (!sewersOwnsPrompt)
                GetManager().ShowPrompt(interactKey.ToString(), CurrentPrompt());
        }
        else if (!near && _inRange)
        {
            _inRange = false;
            if (!sewersOwnsPrompt)
                GetManager().HidePrompt();
        }

        if (_inRange && Input.GetKeyDown(interactKey))
        {
            GetManager().HidePrompt();
            _busy = true;
            HandleState();
        }
    }

    string CurrentPrompt()
    {
        switch (_state)
        {
            case 0:  return examinePrompt;
            case 1:  return placeLadderPrompt;
            default: return climbPrompt;
        }
    }

    void HandleState()
    {
        switch (_state)
        {
            case 0: DoExamine();     break;
            case 1: DoPlaceLadder(); break;
            case 2: DoClimb();       break;
        }
    }

    // ── State 0: Examine ─────────────────────────────────────────────────
    void DoExamine()
    {
        GetManager().ShowNotification(examineDialogue, examineDialogueDuration, onDone: () =>
        {
            // Unlock the sewer pickup prompt
            if (sewersPickup != null)
                sewersPickup.Activate();
            else
                Debug.LogWarning("[LadderPromptInteraction] sewersPickup not assigned!");

            // Don't advance state here — wait for player to grab the ladder
            _busy = false;
            // Refresh prompt for re-approach (still Examine until ladder grabbed)
        });
    }

    // ── Called by SewersLadderPickup when the ladder is grabbed ──────────
    public void AdvanceToPlaceLadder()
    {
        _state = 1;
        _busy  = false;   // ensure prompt is interactive when player returns

        // If the player is already in our range, refresh immediately so the
        // prompt updates to "Place Ladder" as soon as they walk back to us
        // (Sewers is no longer in range at that point, so we can safely show).
        if (_inRange && (sewersPickup == null || !sewersPickup.IsInRange))
            GetManager().ShowPrompt(interactKey.ToString(), CurrentPrompt());
    }

    // ── State 1: Place Ladder ─────────────────────────────────────────────
    void DoPlaceLadder()
    {
        // Show the ladder object
        if (ladderObject != null)
            ladderObject.SetActive(true);

        GetManager().ShowNotification(placeDialogue, placeDialogueDuration, onDone: () =>
        {
            _state = 2;
            _busy  = false;
        });
    }

    // ── State 2: Climb ────────────────────────────────────────────────────
    void DoClimb()
    {
        var pm = _player != null ? _player.GetComponent<PlayerMovement>() : null;
        if (pm != null) pm.enabled = false;

        GetManager().FadeOutThenIn(
            onBlack: () =>
            {
                // Teleport player while screen is black
                if (_player != null && spawnPoint2 != null)
                {
                    var cc = _player.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    _player.position = spawnPoint2.position;
                    if (cc != null) cc.enabled = true;
                }
                if (pm != null) pm.enabled = true;
            },
            fadeOut: fadeOutDuration,
            hold:    holdDuration,
            fadeIn:  fadeInDuration
        );

        // _busy stays true — interaction is fully done after climbing
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }

    static InteractionManager GetManager()
    {
        if (InteractionManager.Instance != null) return InteractionManager.Instance;
        return new GameObject("InteractionManager").AddComponent<InteractionManager>();
    }
}
