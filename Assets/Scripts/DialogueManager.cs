using UnityEngine;
using UnityEngine.Playables;

public class DialogueManager : MonoBehaviour
{
    [Header("Timeline Director")]
    public PlayableDirector timelineDirector;

    [Header("Subtitle System")]
    public TypewriterSubtitle typewriter;

    [Header("Audio Source (optional - assign when you have voice lines)")]
    public AudioSource audioSource;

    [Header("Dialogue Lines")]
    public AudioClip line1Audio; // Optional for now
    public AudioClip line2Audio;
    public AudioClip line3Audio;
    public AudioClip line4Audio;

    [Header("Dialogue Texts")]
    public string line1Text = "Another night... another dead end.";
    public string line2Text = "This Ghost... he\u2019s not just good. He\u2019s a goddamn phantom.";
    public string line3Text = "No prints. No footage. No mistakes.";
    public string line4Text = "But every system has a hole. I just need to find it.";

    // Called via Signal Emitters in Timeline
    public void PlayLine1() { PlayDialogue(line1Text, line1Audio); }
    public void PlayLine2() { PlayDialogue(line2Text, line2Audio); }
    public void PlayLine3() { PlayDialogue(line3Text, line3Audio); }
    public void PlayLine4() { PlayDialogue(line4Text, line4Audio); }

    private void PlayDialogue(string text, AudioClip audio)
    {
        if (timelineDirector == null) { Debug.LogError("DialogueManager: No timeline assigned!"); return; }
        if (typewriter == null) { Debug.LogError("DialogueManager: No typewriter assigned!"); return; }

        Debug.Log("DialogueManager: Playing - " + text);
        timelineDirector.Pause();

        // Play voice audio if assigned
        if (audio != null && audioSource != null)
        {
            audioSource.clip = audio;
            audioSource.Play();
        }

        // Start typewriter — resumes timeline when done
        typewriter.PlayLine(text, () =>
        {
            Debug.Log("DialogueManager: Line finished, resuming timeline");
            timelineDirector.Resume();
        });
    }
}
