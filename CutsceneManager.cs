using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the intro sequence:
///   1) Player sees the main menu (camera pulled back, mouse parallax active)
///   2) Player clicks Play  -> this runs OnPlayClicked()
///   3) Menu UI hides, camera zooms into first-person POV
///   4) Hands/desk animation plays
///   5) After 8 seconds, phone starts ringing
///
/// HOW TO USE:
///   - Create an empty GameObject called "CutsceneManager" and attach this script.
///   - Drag in the references in the Inspector.
///   - On your Play Button (in the Canvas), add an OnClick()
///     and point it at CutsceneManager -> OnPlayClicked.
/// </summary>
public class CutsceneManager : MonoBehaviour
{
    [Header("Scene References")]
    public CameraController cameraController;
    public GameObject mainMenuUI;          // the root Canvas or a panel inside it
    public Animator handsAnimator;         // the Animator that holds your hand/desk animation
    public AudioSource phoneRingAudio;     // AudioSource with the ringtone clip

    [Header("Timings")]
    [Tooltip("Small delay so the zoom finishes before the animation begins")]
    public float zoomBeforeAnimation = 0.6f;

    [Tooltip("How long after the animation starts until the phone rings")]
    public float phoneRingDelay = 8f;

    [Header("Animator")]
    [Tooltip("The trigger parameter name inside your Animator Controller")]
    public string animationTriggerName = "Play";

    private bool started = false;

    /// <summary>Hook this to the Play button's OnClick().</summary>
    public void OnPlayClicked()
    {
        if (started) return;
        started = true;
        StartCoroutine(RunCutscene());
    }

    private IEnumerator RunCutscene()
    {
        // 1) Hide the menu UI
        if (mainMenuUI != null) mainMenuUI.SetActive(false);

        // 2) Tell the camera to move into the POV position / FOV
        if (cameraController != null) cameraController.SetMenuState(false);

        // 3) Give the zoom a moment to settle
        yield return new WaitForSeconds(zoomBeforeAnimation);

        // 4) Start the hands/desk animation
        if (handsAnimator != null && !string.IsNullOrEmpty(animationTriggerName))
            handsAnimator.SetTrigger(animationTriggerName);

        // 5) Wait 8 seconds, then ring the phone
        yield return new WaitForSeconds(phoneRingDelay);

        if (phoneRingAudio != null) phoneRingAudio.Play();
    }
}
