using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Kino;

public class GlitchEffect : MonoBehaviour
{
    [Header("KinoGlitch Components (on Main Camera)")]
    public AnalogGlitch analogGlitch;
    public DigitalGlitch digitalGlitch;

    [Header("Glitch Settings")]
    public float glitchDuration = 1f;
    public float analogIntensity = 0.8f;    // How strong the analog glitch is
    public float digitalIntensity = 0.5f;   // How strong the digital glitch is

    [Header("Binary Code")]
    public GameObject binaryOverlay;
    public TextMeshProUGUI[] binaryTexts;
    public int binaryLength = 40;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip staticSound;

    [Header("On Glitch Complete")]
    public UnityEngine.Events.UnityEvent onGlitchComplete;

    private bool isGlitching = false;

    void Start()
    {
        // Make sure glitch starts off
        if (analogGlitch != null)
        {
            analogGlitch.scanLineJitter = 0f;
            analogGlitch.verticalJump = 0f;
            analogGlitch.horizontalShake = 0f;
            analogGlitch.colorDrift = 0f;
        }

        if (digitalGlitch != null)
            digitalGlitch.intensity = 0f;
    }

    public void TriggerGlitch()
    {
        if (isGlitching) return;
        StartCoroutine(PlayGlitch());
    }

    private IEnumerator PlayGlitch()
    {
        isGlitching = true;

        // Play static sound
        if (staticSound != null && audioSource != null)
            audioSource.PlayOneShot(staticSound);

        // Show binary overlay
        if (binaryOverlay != null)
            binaryOverlay.SetActive(true);

        float elapsed = 0f;
        float updateInterval = 0.05f; // How often it randomizes

        while (elapsed < glitchDuration)
        {
            float t = elapsed / glitchDuration;

            // Randomize glitch intensity each frame for flickering feel
            if (analogGlitch != null)
            {
                analogGlitch.scanLineJitter = Random.Range(0f, analogIntensity);
                analogGlitch.horizontalShake = Random.Range(0f, analogIntensity * 0.5f);
                analogGlitch.colorDrift = Random.Range(0f, analogIntensity * 0.3f);
                analogGlitch.verticalJump = Random.Range(0f, analogIntensity * 0.1f);
            }

            if (digitalGlitch != null)
                digitalGlitch.intensity = Random.Range(0f, digitalIntensity);

            // Randomize binary text
            foreach (var txt in binaryTexts)
                if (txt != null)
                    txt.text = GenerateBinary(binaryLength);

            elapsed += updateInterval;
            yield return new WaitForSecondsRealtime(updateInterval);
        }

        // Reset everything to zero
        if (analogGlitch != null)
        {
            analogGlitch.scanLineJitter = 0f;
            analogGlitch.verticalJump = 0f;
            analogGlitch.horizontalShake = 0f;
            analogGlitch.colorDrift = 0f;
        }

        if (digitalGlitch != null)
            digitalGlitch.intensity = 0f;

        if (binaryOverlay != null)
            binaryOverlay.SetActive(false);

        isGlitching = false;
        onGlitchComplete?.Invoke();

        Debug.Log("GlitchEffect: Complete");
    }

    private string GenerateBinary(int length)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < length; i++)
        {
            sb.Append(Random.value > 0.5f ? "1" : "0");
            if ((i + 1) % 8 == 0) sb.Append(" ");
        }
        return sb.ToString();
    }
}
