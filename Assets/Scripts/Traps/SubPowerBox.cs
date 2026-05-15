using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class SubPowerBox : MonoBehaviour
{
    [Header("Configuration")]
    public bool isReal = true;
    public MasterPowerSystem masterSystem;
    public float interactionDistance = 3f;

    [Header("Look-at gating")]
    [Tooltip("Player must be facing the box for the prompt to appear. 1 = exact, 0 = ignore facing.")]
    [Range(0f, 1f)] public float lookDotThreshold = 0.45f;

    private bool _isFixed;
    private bool _puzzleOpen;
    private WirePuzzleController _activeController;
    private GameObject _promptCanvas;
    private Text _promptText;
    private Transform _player;

    private List<LeverInteraction.WireConfig> DefaultWires()
    {
        return new List<LeverInteraction.WireConfig>
        {
            new LeverInteraction.WireConfig { label = "Wire 1", color = Color.red,    isReal = true },
            new LeverInteraction.WireConfig { label = "Wire 2", color = Color.blue,   isReal = true },
            new LeverInteraction.WireConfig { label = "Wire 3", color = Color.yellow, isReal = true },
            new LeverInteraction.WireConfig { label = "Wire 4", color = Color.green,  isReal = true },
            new LeverInteraction.WireConfig { label = "Wire 5", color = Color.white,  isReal = true }
        };
    }

    void Start()
    {
        CreatePromptUI();
        CachePlayer();
    }

    void CachePlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    void Update()
    {
        // Drive prompt visibility every frame, even after the box is fixed —
        // otherwise the prompt freezes in whatever state it had when _isFixed flipped.
        bool show = ShouldShowPrompt();
        UpdatePrompt(show);

        if (_isFixed || _puzzleOpen) return;

        if (show && Input.GetKeyDown(KeyCode.E))
            OpenPuzzle();
    }

    bool ShouldShowPrompt()
    {
        if (_isFixed || _puzzleOpen) return false;

        if (_player == null) CachePlayer();
        if (_player == null) return false;

        // Use the player position for the distance check so the prompt doesn't pop on
        // if the camera shake / head-bob momentarily wobbles into range.
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > interactionDistance) return false;

        // Require the player to actually be facing the box. This kills the "prompt
        // stays on when I walk away and turn around" issue — distance alone isn't
        // enough because the player can be close to a box while heading elsewhere.
        Camera cam = Camera.main;
        if (cam == null) return true; // distance-only fallback if no camera tagged

        Vector3 toBox = transform.position - cam.transform.position;
        if (toBox.sqrMagnitude < 0.0001f) return true;
        float facing = Vector3.Dot(cam.transform.forward, toBox.normalized);
        return facing >= lookDotThreshold;
    }

    void OnDisable()
    {
        // The puzzle UI/prompt are loose canvases; if we get disabled mid-puzzle
        // (scene unload, component toggle), tear them down so nothing leaks.
        if (_promptCanvas != null) _promptCanvas.SetActive(false);
    }

    void OnDestroy()
    {
        if (_promptCanvas != null) Destroy(_promptCanvas);
        if (_activeController != null) Destroy(_activeController.gameObject);
    }

    void OpenPuzzle()
    {
        _puzzleOpen = true;
        SetPlayerState(false);
        // HIDE the prompt so it doesn't overlap
        if (_promptCanvas) _promptCanvas.SetActive(false);

        GameObject canvasObj = new GameObject($"WirePuzzle_{gameObject.name}");
        _activeController = canvasObj.AddComponent<WirePuzzleController>();
        _activeController.leverHandle = null;
        _activeController.InitializePuzzle(DefaultWires());

        StartCoroutine(WaitForSolve());
    }

    IEnumerator WaitForSolve()
    {
        yield return new WaitUntil(() => _activeController == null || _activeController.IsSolved());

        _isFixed  = true;
        _puzzleOpen = false;

        if (isReal)
        {
            Debug.Log($"<color=green>[SubPowerBox] {gameObject.name} (REAL) - Power circuit overloaded!</color>");
            yield return StartCoroutine(ShowCompletionScreen(true));
            if (masterSystem != null) masterSystem.RegisterFix();
        }
        else
        {
            Debug.Log($"<color=orange>[SubPowerBox] {gameObject.name} (FAKE) - Circuit is dead, no effect.</color>");
            yield return StartCoroutine(ShowCompletionScreen(false));
        }

        SetPlayerState(true);
    }

    IEnumerator ShowCompletionScreen(bool isRealBox)
    {
        GameObject flashObj = new GameObject("CompletionFlash");
        Canvas c = flashObj.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999;
        flashObj.AddComponent<CanvasScaler>();

        GameObject textObj = new GameObject("Txt");
        textObj.transform.SetParent(flashObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text      = isRealBox ? "⚡ CIRCUIT OVERLOADED ⚡" : "✖ DEAD CIRCUIT - NO EFFECT ✖";
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = 28;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.LowerLeft;
        t.color     = isRealBox ? Color.yellow : Color.red;
        // Bottom-left anchor
        t.rectTransform.anchorMin        = new Vector2(0, 0);
        t.rectTransform.anchorMax        = new Vector2(0, 0);
        t.rectTransform.pivot            = new Vector2(0, 0);
        t.rectTransform.anchoredPosition = new Vector2(30, 80);
        t.rectTransform.sizeDelta        = new Vector2(450, 50);
        textObj.AddComponent<Shadow>().effectColor = Color.black;

        // Hold for 2 seconds then fade
        float hold = 2f;
        while (hold > 0f)
        {
            hold -= Time.deltaTime;
            // Flicker effect for horror feel
            if (isRealBox) t.color = new Color(1f, 1f, 0f, 0.6f + Mathf.Sin(Time.time * 12f) * 0.4f);
            yield return null;
        }

        float fade = 0.5f;
        while (fade > 0f)
        {
            fade -= Time.deltaTime;
            t.color = new Color(t.color.r, t.color.g, t.color.b, Mathf.Clamp01(fade / 0.5f));
            yield return null;
        }
        Destroy(flashObj);
    }

    void SetPlayerState(bool canMove)
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null && Camera.main != null && Camera.main.transform.parent != null)
            p = Camera.main.transform.parent.gameObject;

        if (p != null)
        {
            var pm = p.GetComponent("PlayerMovement");
            if (pm == null) pm = p.GetComponent("PlayerControls");
            if (pm != null)
            {
                var prop = pm.GetType().GetProperty("enabled");
                if (prop != null) prop.SetValue(pm, canMove, null);
            }
        }
        Cursor.lockState = canMove ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !canMove;
    }

    void CreatePromptUI()
    {
        _promptCanvas = new GameObject($"BoxPrompt_{gameObject.name}");
        Canvas c = _promptCanvas.AddComponent<Canvas>();
        c.renderMode   = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 997;
        _promptCanvas.AddComponent<CanvasScaler>();
        _promptCanvas.SetActive(false);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(_promptCanvas.transform, false);
        _promptText = textObj.AddComponent<Text>();
        _promptText.text      = "[E] OVERLOAD PANEL";
        _promptText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _promptText.fontSize  = 24;
        _promptText.fontStyle = FontStyle.Bold;
        _promptText.alignment = TextAnchor.LowerLeft;
        _promptText.rectTransform.anchorMin        = new Vector2(0, 0);
        _promptText.rectTransform.anchorMax        = new Vector2(0, 0);
        _promptText.rectTransform.pivot            = new Vector2(0, 0);
        _promptText.rectTransform.anchoredPosition = new Vector2(30, 80);
        _promptText.rectTransform.sizeDelta        = new Vector2(400, 50);
        textObj.AddComponent<Shadow>().effectColor = Color.black;
    }

    void UpdatePrompt(bool show)
    {
        if (_promptCanvas == null) return;

        bool wantActive = show && !_isFixed && !_puzzleOpen;
        if (_promptCanvas.activeSelf != wantActive)
            _promptCanvas.SetActive(wantActive);

        if (wantActive && _promptText != null)
        {
            float pulse = 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f;
            _promptText.color = new Color(1, 1, 1, pulse);
        }
    }
}
