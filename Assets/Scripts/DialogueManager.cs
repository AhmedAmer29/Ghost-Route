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

    [Header("Audio Clips (assign when you have voice recordings)")]
    public AudioClip line1Audio;
    public AudioClip line2Audio;
    public AudioClip line3Audio;
    public AudioClip line4Audio;
    public AudioClip line5Audio;
    public AudioClip line6Audio;

    [Header("Dialogue Texts")]
    [TextArea] public string line1Text = "Another sleepless night. Another dead end on the 409 job.";
    [TextArea] public string line2Text = "Someone cracked First National. Clean. No alarms. No cameras. No sensors triggered. Nothing.";
    [TextArea] public string line3Text = "They\u2019re calling him The Ghost. Fitting... because there\u2019s nothing here. It\u2019s like he was never there at all.";
    [TextArea] public string line4Text = "But ghosts don\u2019t exist. Somebody did this. And they made a mistake somewhere. They always do.";
    [TextArea] public string line5Text = "No prints. No footage. No mistakes. 409 like it never happened.";
    [TextArea] public string line6Text = "But I have one thing they don\u2019t. Close my eyes... and I can see it all.";

    // Called via Signal Emitters in Timeline
    public void PlayLine1() { PlayDialogue(line1Text, line1Audio); }
    public void PlayLine2() { PlayDialogue(line2Text, line2Audio); }
    public void PlayLine3() { PlayDialogue(line3Text, line3Audio); }
    public void PlayLine4() { PlayDialogue(line4Text, line4Audio); }
    public void PlayLine5() { PlayDialogue(line5Text, line5Audio); }
    public void PlayLine6() { PlayDialogue(line6Text, line6Audio); }

    private void PlayDialogue(string text, AudioClip audio)
    {
        if (timelineDirector == null) { Debug.LogError("DialogueManager: No timeline assigned!"); return; }
        if (typewriter == null) { Debug.LogError("DialogueManager: No typewriter assigned!"); return; }

        Debug.Log("DialogueManager: Playing - " + text);
        timelineDirector.Pause();

        if (audio != null && audioSource != null)
        {
            audioSource.clip = audio;
            audioSource.Play();
        }

        typewriter.PlayLine(text, () =>
        {
            Debug.Log("DialogueManager: Line finished, resuming timeline");
            timelineDirector.Resume();
        });
    }
}
