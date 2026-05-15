using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Singleton. Place ONE of these anywhere in the scene (e.g., on the Player).
/// Rats call TakeDamage() on it. Screen goes progressively redder until HP hits 0.
/// </summary>
public class RatDamageEffect : MonoBehaviour
{
    public static RatDamageEffect Instance { get; private set; }

    [Header("Health")]
    public float maxHP        = 100f;
    public float currentHP    = 100f;

    [Header("Recovery")]
    [Tooltip("HP recovered per second when NOT being attacked")]
    public float healRate     = 4f;
    private float _timeSinceLastHit = 0f;
    private float _healDelay        = 2f;   // seconds after last hit before healing starts

    [Header("Death")]
    [Tooltip("Scene to reload on death. Leave blank to reload current scene.")]
    public string deathScene  = "";

    // ── Internal UI ──────────────────────────────────────────────────────────
    private Canvas    _canvas;
    private Image     _redOverlay;
    private Text      _deathText;
    private bool      _dead = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
        // Only run DontDestroyOnLoad if this is a root object, otherwise Unity crashes here!
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        BuildUI();
    }

    void Update()
    {
        if (_dead) return;

        // Passive healing when not being hit
        _timeSinceLastHit += Time.deltaTime;
        if (_timeSinceLastHit > _healDelay && currentHP < maxHP)
        {
            currentHP = Mathf.Min(maxHP, currentHP + healRate * Time.deltaTime);
        }

        RefreshOverlay();
    }

    // ── Public API ────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (_dead) return;
        currentHP -= amount;
        _timeSinceLastHit = 0f;

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            StartCoroutine(Die());
        }
    }

    // ── Overlay ───────────────────────────────────────────────────────────────
    void RefreshOverlay()
    {
        if (_redOverlay == null) return;

        // Alpha scales from 0 (full HP) to 0.85 (dead)
        float t = 1f - (currentHP / maxHP);
        float pulse = Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.15f * t; // flicker at low HP
        float alpha = Mathf.Clamp01(t * 0.85f + pulse);

        _redOverlay.color = new Color(1f, 0f, 0f, alpha);
    }

    IEnumerator Die()
    {
        _dead = true;
        if (_redOverlay != null) _redOverlay.color = new Color(1f, 0f, 0f, 0.95f);

        // Show death message
        if (_deathText != null)
        {
            _deathText.gameObject.SetActive(true);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 1.5f;
                _deathText.color = new Color(1f, 1f, 1f, t);
                yield return null;
            }
        }

        yield return new WaitForSecondsRealtime(2f);

        // Reload
        string scene = string.IsNullOrEmpty(deathScene)
            ? SceneManager.GetActiveScene().name
            : deathScene;
        SceneManager.LoadScene(scene);
    }

    // ── Build UI at runtime ───────────────────────────────────────────────────
    void BuildUI()
    {
        // Canvas
        GameObject cObj = new GameObject("RatDamageCanvas");
        cObj.transform.SetParent(transform);
        _canvas              = cObj.AddComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 990;
        cObj.AddComponent<CanvasScaler>();

        // Red vignette overlay (full screen)
        GameObject imgObj = new GameObject("RedOverlay");
        imgObj.transform.SetParent(cObj.transform, false);
        _redOverlay = imgObj.AddComponent<Image>();
        _redOverlay.color = new Color(1f, 0f, 0f, 0f);
        RectTransform rt = _redOverlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Death text
        GameObject txtObj = new GameObject("DeathText");
        txtObj.transform.SetParent(cObj.transform, false);
        _deathText           = txtObj.AddComponent<Text>();
        _deathText.text      = "YOU DIED";
        _deathText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _deathText.fontSize  = 72;
        _deathText.fontStyle = FontStyle.Bold;
        _deathText.alignment = TextAnchor.MiddleCenter;
        _deathText.color     = new Color(1f, 1f, 1f, 0f);
        RectTransform trt    = _deathText.rectTransform;
        trt.anchorMin        = Vector2.zero;
        trt.anchorMax        = Vector2.one;
        trt.offsetMin        = Vector2.zero;
        trt.offsetMax        = Vector2.zero;
        txtObj.AddComponent<Shadow>().effectColor = Color.black;
        txtObj.gameObject.SetActive(false);
    }
}
