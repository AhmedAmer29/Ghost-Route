using System.Collections;
using UnityEngine;

/// <summary>
/// RopeCrossingController.cs
/// Core state machine + QTE logic for the rope crossing challenge.
/// Attach to any GameObject in the scene. Wire up the UI callbacks via the Inspector.
/// </summary>
public class RopeCrossingController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    // STATE MACHINE
    // ─────────────────────────────────────────────────────────────

    public enum CrossingState { Idle, Starting, Crossing, Falling, Success }

    /// <summary>Current state of the rope-crossing sequence.</summary>
    [Header("State (Read-Only in Inspector)")]
    public CrossingState currentState = CrossingState.Idle;

    // ─────────────────────────────────────────────────────────────
    // GAME VARIABLES
    // ─────────────────────────────────────────────────────────────

    [Header("Progress")]
    [Tooltip("0 = start of rope, 100 = safely across.")]
    [Range(0f, 100f)]
    public float progress = 0f;

    [Tooltip("How much progress is gained per successful QTE press.")]
    public float progressPerSuccess = 5f;

    [Header("Balance")]
    [Tooltip("Number of QTE failures allowed before the player falls.")]
    public int maxBalanceLosses = 3;

    /// <summary>Current number of balance losses this run.</summary>
    [HideInInspector] public int balanceLosses = 0;

    // ─────────────────────────────────────────────────────────────
    // QTE STATE (Exposed so PlayerRopeMovement can read them)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Whether a QTE prompt is currently on screen and awaiting input.</summary>
    [HideInInspector] public bool awaitingInput  = false;

    /// <summary>The key the player must press for the current QTE.</summary>
    [HideInInspector] public KeyCode requiredKey = KeyCode.A;

    /// <summary>0..1 fill — used by UI to draw the shrinking timer circle.</summary>
    [HideInInspector] public float timerFill = 1f;

    // ─────────────────────────────────────────────────────────────
    // EVENTS  (subscribe from UI / AudioManager etc.)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Fired when a new QTE prompt appears.  Arg: "A" or "D".</summary>
    public System.Action<string> OnPromptShown;

    /// <summary>Fired when the QTE resolves.  Arg: true = success, false = failure.</summary>
    public System.Action<bool>   OnPromptResolved;

    /// <summary>Fired when the player successfully crosses.</summary>
    public System.Action OnSuccess;

    /// <summary>Fired when the player falls.</summary>
    public System.Action OnFall;

    // ─────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────

    private Coroutine _crossingCoroutine;

    // ─────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────

    /// <summary>Call this to begin the rope crossing (e.g. from your trigger or cutscene).</summary>
    public void StartCrossing()
    {
        if (currentState != CrossingState.Idle) return;

        // Reset all state
        progress       = 0f;
        balanceLosses  = 0;
        awaitingInput  = false;
        timerFill      = 1f;

        currentState   = CrossingState.Starting;
        _crossingCoroutine = StartCoroutine(CrossingLoop());
    }

    /// <summary>Hard-reset back to Idle (use for respawn).</summary>
    public void ResetCrossing()
    {
        if (_crossingCoroutine != null) StopCoroutine(_crossingCoroutine);
        currentState  = CrossingState.Idle;
        progress      = 0f;
        balanceLosses = 0;
        awaitingInput = false;
    }

    // ─────────────────────────────────────────────────────────────
    // MAIN COROUTINE
    // ─────────────────────────────────────────────────────────────

    private IEnumerator CrossingLoop()
    {
        currentState = CrossingState.Crossing;

        while (progress < 100f)
        {
            // ── Determine difficulty tier for current progress ──────────
            float windowDuration;
            float pauseBetweenPrompts;

            if (progress < 25f)
            {
                // Section 1 — Safe
                windowDuration      = 1.5f;
                pauseBetweenPrompts = Random.Range(1.0f, 2.0f);
            }
            else if (progress < 50f)
            {
                // Section 2 — Moderate
                windowDuration      = 1.2f;
                pauseBetweenPrompts = Random.Range(0.8f, 1.5f);
            }
            else if (progress < 75f)
            {
                // Section 3 — Hard
                windowDuration      = 1.0f;
                pauseBetweenPrompts = Random.Range(0.5f, 1.0f);
            }
            else
            {
                // Section 4 — Rapid Succession
                windowDuration      = 1.0f;
                pauseBetweenPrompts = 0.1f;   // exactly 0.1 s between prompts
            }

            // Brief pause before the next prompt appears
            yield return new WaitForSeconds(pauseBetweenPrompts);

            // Safety check: state might have changed during the wait
            if (currentState != CrossingState.Crossing) yield break;

            // ── Show a new QTE prompt ───────────────────────────────────
            requiredKey   = (Random.value > 0.5f) ? KeyCode.A : KeyCode.D;
            awaitingInput = true;
            timerFill     = 1f;

            string keyLabel = (requiredKey == KeyCode.A) ? "A" : "D";
            OnPromptShown?.Invoke(keyLabel);

            // ── Run the countdown coroutine and wait for it ─────────────
            bool pressedCorrectly = false;
            yield return StartCoroutine(WaitForInput(windowDuration, result => pressedCorrectly = result));

            awaitingInput = false;

            if (currentState != CrossingState.Crossing) yield break;

            // ── Resolve result ──────────────────────────────────────────
            if (pressedCorrectly)
            {
                progress += progressPerSuccess;
                progress  = Mathf.Min(progress, 100f);
                OnPromptResolved?.Invoke(true);
            }
            else
            {
                balanceLosses++;
                OnPromptResolved?.Invoke(false);

                if (balanceLosses >= maxBalanceLosses)
                {
                    TriggerFall();
                    yield break;
                }
            }
        }

        // ── Reached 100 % ───────────────────────────────────────────────
        if (currentState == CrossingState.Crossing)
            TriggerSuccess();
    }

    // ─────────────────────────────────────────────────────────────
    // QTE WINDOW COROUTINE
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Waits up to <paramref name="duration"/> seconds for the player to press
    /// <see cref="requiredKey"/>.  Calls <paramref name="result"/> with true on
    /// a correct press, false on a wrong press OR timeout.
    /// </summary>
    private IEnumerator WaitForInput(float duration, System.Action<bool> result)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed  += Time.deltaTime;
            timerFill = 1f - (elapsed / duration);   // Shrinks from 1 → 0

            // ── Correct key ───────────────────────────────────────
            if (Input.GetKeyDown(requiredKey))
            {
                result(true);
                yield break;
            }

            // ── Wrong key pressed (either A or D that isn't required) ──
            KeyCode other = (requiredKey == KeyCode.A) ? KeyCode.D : KeyCode.A;
            if (Input.GetKeyDown(other))
            {
                result(false);
                yield break;
            }

            yield return null;
        }

        // Timeout → failure
        result(false);
    }

    // ─────────────────────────────────────────────────────────────
    // STATE TRANSITIONS
    // ─────────────────────────────────────────────────────────────

    private void TriggerFall()
    {
        currentState  = CrossingState.Falling;
        awaitingInput = false;
        Debug.Log("[RopeCrossing] Player FELL — balance losses: " + balanceLosses);
        OnFall?.Invoke();
    }

    private void TriggerSuccess()
    {
        currentState  = CrossingState.Success;
        awaitingInput = false;
        Debug.Log("[RopeCrossing] Player CROSSED SUCCESSFULLY!");
        OnSuccess?.Invoke();
    }
}
