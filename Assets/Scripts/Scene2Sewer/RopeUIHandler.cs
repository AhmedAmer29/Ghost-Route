using UnityEngine;
using TMPro;

public class RopeUIHandler : MonoBehaviour
{
    public RopeCrossingController controller;
    public GameObject uiPanel;
    public TextMeshProUGUI promptText;

    void Start()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
        
        // Find controller if not assigned
        if (controller == null) controller = FindFirstObjectByType<RopeCrossingController>();

        if (controller != null)
        {
            controller.OnPromptShown += ShowPrompt;
            controller.OnPromptResolved += (success) => HidePrompt();
            controller.OnSuccess += () => HidePrompt();
            controller.OnFall += () => HidePrompt();
        }
    }

    void ShowPrompt(string key)
    {
        if (uiPanel != null) uiPanel.SetActive(true);
        if (promptText != null) promptText.text = key;
    }

    void HidePrompt()
    {
        if (uiPanel != null) uiPanel.SetActive(false);
    }
}
