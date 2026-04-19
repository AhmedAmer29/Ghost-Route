using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    [Header("Scene References")]
    public CameraController cameraController;
    public GameObject       mainMenuUI;
    public Animation        handsAnimation;

    [Header("Animation")]
    public string animationClipName = "Hands Movement";

    [Header("Phone Audio")]
    public AudioSource phoneRingAudio;
    public AudioClip   phonePickupSound;
    public AudioClip   phoneHangupSound;

    [Header("Dialogue Audio Clips")]
    [Tooltip("16 slots — one per dialogue line, in order. Drag caller_01 to caller_12, then detective_01 to detective_04.")]
    public AudioClip[] dialogueClips = new AudioClip[16];

    [Header("Timings")]
    public float phoneRingDelay = 8f;
    public float ringDuration   = 2.5f;
    public float linePause      = 0.4f;
    public float lastLinePause  = 1.5f;

    [Header("Subtitles")]
    [Tooltip("Must be on a SEPARATE canvas from the main menu — see setup notes")]
    public TextMeshProUGUI subtitleText;
    [Tooltip("How fast letters appear (seconds per character)")]
    public float typewriterSpeed = 0.03f;

    [Header("Fade to Black")]
    [Tooltip("Must be on the same separate HUD canvas as the subtitle")]
    public Image fadeOverlay;
    public float fadeDuration = 2f;

    [Header("Next Scene")]
    public string nextSceneName = "";

    // ── Dialogue ──────────────────────────────────────────────────────────────
    [System.Serializable]
    public class DialogueLine
    {
        public int    speaker;   // 0 = caller, 1 = detective (you)
        public string line;
        public float  duration;  // how long the full line stays before moving on
    }

    private DialogueLine[] dialogue;
    private Coroutine typewriterCoroutine;

    // ── private ───────────────────────────────────────────────────────────────
    private bool started = false;

    void Awake()
    {
        // Lines match the Fish Audio recordings exactly (tags stripped, text only)
        // Slots: caller_01 to caller_12 = indices 0-11, detective_01 to detective_04 = indices 12-15
        dialogue = new DialogueLine[]
        {
            new DialogueLine { speaker = 0, line = "Detective.",                                                                    duration = 2.0f },
            new DialogueLine { speaker = 0, line = "Bank of Meridian. Last night. 2 AM.",                                           duration = 3.2f },
            new DialogueLine { speaker = 0, line = "No forced entry. No alarms. No prints.",                                        duration = 3.2f },
            new DialogueLine { speaker = 0, line = "Whoever did this... walked through that building like he owned it.",            duration = 4.2f },
            new DialogueLine { speaker = 0, line = "We've been through the footage. Nothing.",                                      duration = 3.0f },
            new DialogueLine { speaker = 0, line = "Not a blur. Not a shadow. Not a single frame.",                                 duration = 3.8f },
            new DialogueLine { speaker = 0, line = "As far as we can tell... no one was ever there.",                               duration = 4.0f },
            new DialogueLine { speaker = 0, line = "The bank robbed itself.",                                                       duration = 3.5f },
            new DialogueLine { speaker = 0, line = "But someone walked out of there with everything.",                              duration = 3.8f },
            new DialogueLine { speaker = 0, line = "No face. No name. No trail.",                                                   duration = 3.2f },
            new DialogueLine { speaker = 0, line = "He's out there right now... and we have absolutely nothing.",                   duration = 4.0f },
            new DialogueLine { speaker = 0, line = "Find his route. Find his exits. Find how a ghost moves.",                      duration = 4.2f },
            new DialogueLine { speaker = 1, line = "I'll find him.",                                                                duration = 2.5f },
            new DialogueLine { speaker = 2, line = "Doesn't exist on any camera...",                                                duration = 3.0f },
            new DialogueLine { speaker = 2, line = "Then I need to stop looking for him.",                                          duration = 3.2f },
            new DialogueLine { speaker = 2, line = "I need to be him.",                                                             duration = 3.5f },
        };

        if (fadeOverlay != null)
        {
            Color c = fadeOverlay.color;
            c.a = 0f;
            fadeOverlay.color = c;
            fadeOverlay.gameObject.SetActive(true);
        }

        if (subtitleText != null)
            subtitleText.text = "";
    }

    public void OnPlayClicked()
    {
        if (started) return;
        started = true;
        StartCoroutine(RunCutscene());
    }

    private IEnumerator RunCutscene()
    {
        // 1) Hide menu, zoom in
        if (mainMenuUI != null) mainMenuUI.SetActive(false);
        if (cameraController != null) cameraController.SetMenuState(false);

        // 2) Loop hands animation
        if (handsAnimation != null)
        {
            handsAnimation.wrapMode = WrapMode.Loop;
            handsAnimation.Play(animationClipName);
        }

        // 3) Wait, then ring
        yield return new WaitForSeconds(phoneRingDelay);

        if (phoneRingAudio != null)
        {
            phoneRingAudio.loop = true;
            phoneRingAudio.Play();
        }

        // 4) Pick up after a moment
        yield return new WaitForSeconds(ringDuration);
        if (phoneRingAudio != null) phoneRingAudio.Stop();
        PlayOneShot(phonePickupSound);

        // Breather — let the pickup sound settle before anyone speaks
        yield return new WaitForSeconds(2.0f);

        // 5) Dialogue with typewriter + audio
        // Index 12 = detective's in-call line ("I'll find him.") — hang up after this
        int hangupAfterIndex = 12;

        for (int i = 0; i < dialogue.Length; i++)
        {
            DialogueLine d = dialogue[i];

            // Play the voice clip for this line (if assigned)
            AudioClip clip = (dialogueClips != null && i < dialogueClips.Length) ? dialogueClips[i] : null;
            if (clip != null) phoneRingAudio.PlayOneShot(clip);

            // Show subtitle with typewriter
            yield return StartCoroutine(TypewriterLine(d.speaker, d.line));

            // Wait for the clip to finish — subtract time already spent on typewriter
            float waitTime = (clip != null)
                ? Mathf.Max(clip.length - (d.line.Length * typewriterSpeed), 0f)
                : d.duration;
            yield return new WaitForSeconds(waitTime + 0.15f);

            HideSubtitle();
            yield return new WaitForSeconds(linePause);

            // Hang up after detective's in-call response
            if (i == hangupAfterIndex)
            {
                yield return new WaitForSeconds(0.4f);
                PlayOneShot(phoneHangupSound);
                yield return new WaitForSeconds(1.8f);
            }
        }

        // 6) Final pause before fade
        yield return new WaitForSeconds(lastLinePause);

        // 7) Fade to black
        yield return StartCoroutine(FadeToBlack());

        // 8) Load next scene
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
    }

    // ── Typewriter ────────────────────────────────────────────────────────────

    IEnumerator TypewriterLine(int speaker, string line)
    {
        if (subtitleText == null) yield break;

        // Speaker label
        string label, labelHex, textHex;
        if (speaker == 0)
        {
            label    = "[CALLER]";
            labelHex = "D4A84B";   // amber
            textHex  = "E8E8E8";   // near-white
        }
        else if (speaker == 1)
        {
            label    = "[YOU]";
            labelHex = "7EC8E3";   // cool blue
            textHex  = "FFFFFF";   // white
        }
        else
        {
            // Speaker 2 — detective muttering to himself, no label, dimmer italic
            label    = "";
            labelHex = "AAAAAA";
            textHex  = "AAAAAA";   // dim grey, feels internal/quiet
        }

        string prefix = label.Length > 0
            ? "<color=#" + labelHex + "><b>" + label + "</b></color>  "
            : "";

        // Type out the line character by character
        bool italic = (speaker == 2);
        string openTag  = italic ? "<i>" : "";
        string closeTag = italic ? "</i>" : "";

        string displayed = "";
        for (int i = 0; i <= line.Length; i++)
        {
            displayed = line.Substring(0, i);
            subtitleText.text = prefix + "<color=#" + textHex + ">" + openTag + displayed + closeTag + "</color>";
            yield return new WaitForSeconds(typewriterSpeed);
        }
    }

    void HideSubtitle()
    {
        if (subtitleText != null) subtitleText.text = "";
    }

    void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;
        if (phoneRingAudio != null)
            phoneRingAudio.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position);
    }

    IEnumerator FadeToBlack()
    {
        if (fadeOverlay == null) yield break;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = fadeOverlay.color;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            fadeOverlay.color = c;
            yield return null;
        }
    }
}
