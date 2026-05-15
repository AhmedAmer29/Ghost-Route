using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerMovement))]
public class StaminaUI : MonoBehaviour
{
    private Image _staminaFill;
    private Image _breathFill;
    private CanvasGroup _breathGroup;

    private Image _vignetteTop;
    private Image _vignetteBottom;
    private Image _vignetteLeft;
    private Image _vignetteRight;

    private PlayerMovement _movement;
    private PlayerState _state;
    private GameObject _canvasObj;

    void Start()
    {
        _movement = GetComponent<PlayerMovement>();
        _state = GetComponent<PlayerState>();
        CreateUI();
    }

    void CreateUI()
    {
        _canvasObj = new GameObject("PlayerStatusCanvas");
        Canvas canvas = _canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        _canvasObj.AddComponent<CanvasScaler>();
        _canvasObj.AddComponent<GraphicRaycaster>();
        DontDestroyOnLoad(_canvasObj);

        CreateVignette(_canvasObj.transform);
        CreateStaminaBar(_canvasObj.transform);
        CreateBreathBar(_canvasObj.transform);
    }

    void CreateVignette(Transform parent)
    {
        Color dark = new Color(0f, 0f, 0f, 0.85f);
        float thick = 0.12f;

        _vignetteTop = MakeVignetteBar(parent, "VignetteTop", new Vector2(0f, 1f - thick), new Vector2(1f, 1f), dark);
        _vignetteBottom = MakeVignetteBar(parent, "VignetteBottom", new Vector2(0f, 0f), new Vector2(1f, thick), dark);
        _vignetteLeft = MakeVignetteBar(parent, "VignetteLeft", new Vector2(0f, thick), new Vector2(thick, 1f - thick), dark);
        _vignetteRight = MakeVignetteBar(parent, "VignetteRight", new Vector2(1f - thick, thick), new Vector2(1f, 1f - thick), dark);
    }

    Image MakeVignetteBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color c)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        RectTransform rt = img.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CanvasGroup cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        return img;
    }

    void CreateStaminaBar(Transform parent)
    {
        GameObject bgObj = new GameObject("StaminaBG");
        bgObj.transform.SetParent(parent, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.sizeDelta = new Vector2(220f, 22f);
        bgRect.anchoredPosition = new Vector2(0f, 35f);

        GameObject labelLeft = new GameObject("StaminaLabel");
        labelLeft.transform.SetParent(bgObj.transform, false);
        Text leftText = labelLeft.AddComponent<Text>();
        leftText.text = "STAMINA";
        leftText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        leftText.fontSize = 11;
        leftText.fontStyle = FontStyle.Bold;
        leftText.alignment = TextAnchor.MiddleLeft;
        leftText.color = new Color(1f, 1f, 1f, 0.7f);
        RectTransform leftRect = leftText.rectTransform;
        leftRect.anchorMin = Vector2.zero;
        leftRect.anchorMax = Vector2.one;
        leftRect.offsetMin = new Vector2(6f, 0f);
        leftRect.offsetMax = new Vector2(0f, 0f);

        GameObject fillObj = new GameObject("StaminaFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        _staminaFill = fillObj.AddComponent<Image>();
        _staminaFill.color = new Color(0.3f, 0.9f, 0.3f, 1f);
        RectTransform fillRect = _staminaFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
    }

    void CreateBreathBar(Transform parent)
    {
        GameObject bgObj = new GameObject("BreathBG");
        bgObj.transform.SetParent(parent, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.3f, 0.5f, 0.85f);

        _breathGroup = bgObj.AddComponent<CanvasGroup>();
        _breathGroup.alpha = 0f;

        RectTransform bgRect = bgImage.rectTransform;
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.sizeDelta = new Vector2(220f, 22f);
        bgRect.anchoredPosition = new Vector2(0f, 65f);

        GameObject labelLeft = new GameObject("BreathLabel");
        labelLeft.transform.SetParent(bgObj.transform, false);
        Text leftText = labelLeft.AddComponent<Text>();
        leftText.text = "BREATH";
        leftText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        leftText.fontSize = 11;
        leftText.fontStyle = FontStyle.Bold;
        leftText.alignment = TextAnchor.MiddleLeft;
        leftText.color = new Color(1f, 1f, 1f, 0.9f);
        RectTransform leftRect = leftText.rectTransform;
        leftRect.anchorMin = Vector2.zero;
        leftRect.anchorMax = Vector2.one;
        leftRect.offsetMin = new Vector2(6f, 0f);
        leftRect.offsetMax = new Vector2(0f, 0f);

        GameObject fillObj = new GameObject("BreathFill");
        fillObj.transform.SetParent(bgObj.transform, false);
        _breathFill = fillObj.AddComponent<Image>();
        _breathFill.color = new Color(0.2f, 0.7f, 1f, 1f);
        RectTransform fillRect = _breathFill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
    }

    void Update()
    {
        UpdateStamina();
        UpdateBreath();
        UpdateUnderwaterEffects();
    }

    void UpdateStamina()
    {
        if (_movement == null || _staminaFill == null) return;

        float pct = _movement.currentStamina / _movement.maxStamina;

        RectTransform rt = _staminaFill.rectTransform;
        Vector2 aMax = rt.anchorMax;
        aMax.x = Mathf.Lerp(aMax.x, pct, Time.deltaTime * 10f);
        rt.anchorMax = aMax;

        if (pct < 0.01f)
        {
            _staminaFill.color = new Color(1f, 0.15f, 0.15f, 0.6f + Mathf.Sin(Time.time * 12f) * 0.4f);
        }
        else if (pct < 0.3f)
        {
            float pulse = 0.9f + Mathf.Sin(Time.time * 8f) * 0.1f;
            _staminaFill.color = Color.Lerp(
                new Color(1f, 0.3f, 0.1f),
                new Color(1f, 0.8f, 0.1f),
                (pct - 0f) / 0.3f
            ) * pulse;
        }
        else
        {
            float t = Mathf.Clamp01((pct - 0.3f) / 0.7f);
            _staminaFill.color = Color.Lerp(
                new Color(1f, 0.8f, 0.1f),
                new Color(0.3f, 0.9f, 0.3f),
                t
            );
        }
    }

    void UpdateBreath()
    {
        if (_state == null || _breathFill == null || _breathGroup == null) return;

        float breathPct = 1f - _state.GetDrownProgress();

        RectTransform rt = _breathFill.rectTransform;
        Vector2 aMax = rt.anchorMax;
        aMax.x = Mathf.Lerp(aMax.x, breathPct, Time.deltaTime * 10f);
        rt.anchorMax = aMax;

        if (breathPct < 0.25f)
        {
            float flash = 0.5f + Mathf.Sin(Time.time * 8f) * 0.5f;
            _breathFill.color = Color.Lerp(new Color(1f, 0.1f, 0.1f), new Color(0.2f, 0.7f, 1f), flash);
        }
        else
        {
            _breathFill.color = new Color(0.2f, 0.7f, 1f, 1f);
        }

        float targetAlpha = (_state.isSubmerged || breathPct < 0.99f) ? 1f : 0f;
        _breathGroup.alpha = Mathf.MoveTowards(_breathGroup.alpha, targetAlpha, Time.deltaTime * 3f);
    }

    void UpdateUnderwaterEffects()
    {
        if (_state == null) return;

        float vigTarget = _state.isSubmerged ? 1f : 0f;
        float vigAlpha = Mathf.MoveTowards(
            _vignetteTop.GetComponent<CanvasGroup>().alpha,
            vigTarget,
            Time.deltaTime * 2.5f
        );

        SetVignetteAlpha(vigAlpha);
    }

    void SetVignetteAlpha(float a)
    {
        if (_vignetteTop != null)
        {
            _vignetteTop.GetComponent<CanvasGroup>().alpha = a;
            _vignetteBottom.GetComponent<CanvasGroup>().alpha = a;
            _vignetteLeft.GetComponent<CanvasGroup>().alpha = a;
            _vignetteRight.GetComponent<CanvasGroup>().alpha = a;
        }
    }

    void OnDestroy()
    {
        if (_canvasObj != null)
            Destroy(_canvasObj);
    }
}
