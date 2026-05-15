using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RopeCrossingUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject promptRoot;
    public TextMeshProUGUI keyText;
    public Image timerFill;
    public CanvasGroup canvasGroup;

    [Header("Animations")]
    public float fadeSpeed = 5f;
    public Color successColor = Color.green;
    public Color failureColor = Color.red;
    public Color normalColor = Color.white;

    private float _maxTime;
    private float _currentTime;
    private bool _isShowing;

    void Start()
    {
        if (promptRoot != null) promptRoot.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0;
    }

    void Update()
    {
        if (!_isShowing)
        {
            if (canvasGroup != null) canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0, Time.deltaTime * fadeSpeed);
            return;
        }

        if (canvasGroup != null) canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1, Time.deltaTime * fadeSpeed);

        if (_currentTime > 0)
        {
            _currentTime -= Time.deltaTime;
            if (timerFill != null)
                timerFill.fillAmount = _currentTime / _maxTime;
            
            // Pulse scale
            float pulse = 1f + Mathf.Sin(Time.time * 15f) * 0.05f;
            promptRoot.transform.localScale = Vector3.one * pulse;
        }
    }

    public void ShowPrompt(string key, float duration)
    {
        _isShowing = true;
        _maxTime = duration;
        _currentTime = duration;
        
        if (promptRoot != null) promptRoot.SetActive(true);
        if (keyText != null) 
        {
            keyText.text = key;
            keyText.color = normalColor;
        }
        if (timerFill != null) timerFill.color = normalColor;
    }

    public void HidePrompt(bool success)
    {
        _isShowing = false;
        
        if (keyText != null) keyText.color = success ? successColor : failureColor;
        if (timerFill != null) timerFill.color = success ? successColor : failureColor;

        // Briefly keep visible to show success/failure color
        Invoke("DisableRoot", 0.2f);
    }

    private void DisableRoot()
    {
        if (!_isShowing && promptRoot != null) promptRoot.SetActive(false);
    }

    public void NotifyFall()
    {
        _isShowing = false;
        if (promptRoot != null) promptRoot.SetActive(false);
        // Could trigger a full screen vignette or "YOU DIED" style text
    }

    public void NotifySuccess()
    {
        _isShowing = false;
        if (promptRoot != null) promptRoot.SetActive(false);
        Debug.Log("CROSSING SUCCESSFUL!");
    }
}
