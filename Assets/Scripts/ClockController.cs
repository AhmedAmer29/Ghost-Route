using System.Collections;
using UnityEngine;

public class ClockController : MonoBehaviour
{
    [Header("Clock Hands  (drag the actual Seconds and Hours objects)")]
    public Transform secondsHand;
    public Transform hoursHand;

    [Header("Calibration  (adjust until 12 o'clock looks correct)")]
    [Tooltip("Z angle that visually places the Hours hand at 12:00")]
    public float hourAngleAt12   = 0f;
    [Tooltip("Z angle that visually places the Seconds hand at :00")]
    public float secondAngleAt00 = -30f;

    [Header("Tick Audio")]
    public AudioSource tickAudioSource;
    public AudioClip   tickSound;

    [Header("Alarm Audio")]
    public AudioSource alarmAudioSource;
    public AudioClip   alarmSound;
    public AudioClip   alarmStopSound;

    private int secondsFromMidnight = -(5 * 60); // 11:55 PM

    // Original rotations stored as Quaternions — never touched again after Start.
    // Position is never changed — only localRotation.
    private Quaternion secondsOrigRot;
    private Quaternion hoursOrigRot;

    private Coroutine tickCoroutine;

    // During the sleep phase each real-time tick advances the clock by 1 minute
    // so the hands visibly sweep 11:55 → 12:00 over 5 ticks.
    // SnapToMidnight() flips this to false so after midnight the clock runs
    // at normal speed (1 second per tick).
    private bool sleepMode = true;

    void Start()
    {
        if (secondsHand == null) { Debug.LogError("ClockController: Seconds Hand not assigned!"); return; }
        if (hoursHand   == null) { Debug.LogError("ClockController: Hours Hand not assigned!");   return; }

        // Capture the model's original rotation once — position is untouched forever
        secondsOrigRot = secondsHand.localRotation;
        hoursOrigRot   = hoursHand.localRotation;

        UpdateClockVisuals();
    }

    public void StartTicking()
    {
        sleepMode = true;
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        tickCoroutine = StartCoroutine(TickLoop());
    }

    public void SnapToMidnight()
    {
        sleepMode = false;          // switch to real-time speed from here on
        secondsFromMidnight = 0;
        UpdateClockVisuals();
    }

    public void PlayAlarm()
    {
        if (alarmAudioSource && alarmSound)
        {
            alarmAudioSource.clip = alarmSound;
            alarmAudioSource.loop = true;
            alarmAudioSource.Play();
        }
    }

    public void StopAlarm()
    {
        if (alarmAudioSource)
        {
            alarmAudioSource.Stop();
            alarmAudioSource.loop = false;
            if (alarmStopSound)
                alarmAudioSource.PlayOneShot(alarmStopSound);
        }
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    IEnumerator TickLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            // Sleep mode: each real second = 1 clock minute, so 5 ticks sweeps
            // the hands from 11:55 to 12:00 visibly.
            // Normal mode: each real second = 1 clock second (after midnight).
            secondsFromMidnight += sleepMode ? 60 : 1;

            UpdateClockVisuals();
            PlayOneTick();
        }
    }

    void PlayOneTick()
    {
        if (tickAudioSource && tickSound)
            tickAudioSource.PlayOneShot(tickSound);
    }

    void UpdateClockVisuals()
    {
        if (secondsHand == null || hoursHand == null) return;

        float secPos    = ((secondsFromMidnight % 60) + 60) % 60;
        float secAngle  = secondAngleAt00 + (secPos * 6f);
        float hourAngle = hourAngleAt12   + (secondsFromMidnight * (0.5f / 60f));

        // Rotate around the parent's Z axis by the calculated angle,
        // applied on top of the model's original rotation.
        // Position is never touched — only localRotation changes.
        secondsHand.localRotation = Quaternion.AngleAxis(secAngle, Vector3.forward) * secondsOrigRot;
        hoursHand.localRotation   = Quaternion.AngleAxis(hourAngle, Vector3.forward) * hoursOrigRot;
    }
}
