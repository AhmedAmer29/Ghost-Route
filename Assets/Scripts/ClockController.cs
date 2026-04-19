using System.Collections;
using UnityEngine;

public class ClockController : MonoBehaviour
{
    [Header("Clock Hand Rotators")]
    [Tooltip("Drag the SecondsRotator empty GameObject here — NOT the mesh itself")]
    public Transform secondsRotator;
    [Tooltip("Drag the HoursRotator empty GameObject here — NOT the mesh itself")]
    public Transform hoursRotator;

    [Header("Calibration")]
    [Tooltip("Z angle of the Hours rotator when clock shows 12:00. Adjust until 12 looks correct.")]
    public float hourAngleAt12   = 90f;
    [Tooltip("Z angle of the Seconds rotator when clock shows :00. Adjust if needed.")]
    public float secondAngleAt00 = 180f;

    [Header("Tick Audio")]
    public AudioSource tickAudioSource;
    public AudioClip   tickSound;

    [Header("Alarm Audio")]
    public AudioSource alarmAudioSource;
    public AudioClip   alarmSound;
    public AudioClip   alarmStopSound;

    // Time tracked as seconds offset from midnight. 11:55 PM = -300.
    private int secondsFromMidnight = -(5 * 60);

    private Coroutine tickCoroutine;

    void Start()
    {
        if (secondsRotator == null) { Debug.LogError("ClockController: Seconds Rotator not assigned!"); return; }
        if (hoursRotator   == null) { Debug.LogError("ClockController: Hours Rotator not assigned!");   return; }

        UpdateClockVisuals();
    }

    public void StartTicking()
    {
        if (tickCoroutine != null) StopCoroutine(tickCoroutine);
        tickCoroutine = StartCoroutine(TickLoop());
    }

    public void SnapToMidnight()
    {
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
            secondsFromMidnight++;
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
        if (secondsRotator == null || hoursRotator == null) return;

        float secPos    = ((secondsFromMidnight % 60) + 60) % 60;
        float secAngle  = secondAngleAt00 + (secPos * 6f);
        float hourAngle = hourAngleAt12   + (secondsFromMidnight * (0.5f / 60f));

        // Rotators are pure Z — X and Y are always 0, no Euler distortion possible
        secondsRotator.localEulerAngles = new Vector3(0f, 0f, secAngle);
        hoursRotator.localEulerAngles   = new Vector3(0f, 0f, hourAngle);
    }
}
