using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterSubtitle : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public TextMeshProUGUI subtitleText;

    [Header("Settings")]
    public float typingSpeed = 0.05f;   // Seconds between each letter
    public float holdDuration = 1.5f;   // How long text stays after fully typed

    private Coroutine currentCoroutine;

    // Called by DialogueManager with the line text
    public void PlayLine(string text, System.Action onFinished)
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        currentCoroutine = StartCoroutine(TypeText(text, onFinished));
    }

    private IEnumerator TypeText(string text, System.Action onFinished)
    {
        subtitleText.text = "";

        foreach (char letter in text)
        {
            subtitleText.text += letter;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        // Hold the full text on screen
        yield return new WaitForSecondsRealtime(holdDuration);

        // Clear and notify DialogueManager to resume timeline
        subtitleText.text = "";
        onFinished?.Invoke();
    }

    public void ClearText()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);
        subtitleText.text = "";
    }
}
