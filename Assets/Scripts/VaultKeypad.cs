using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class VaultKeypad : MonoBehaviour
{
    [Header("References")]
    public VaultDoor vaultDoor;

    [Header("Code")]
    public string correctCode = "1234";

    [Header("Proximity")]
    public float interactDistance = 3f;

    // Shared flag so other scripts (e.g. mouse look) can pause while keypad is open
    public static bool IsOpen { get; private set; }

    Transform       _player;
    TextMeshProUGUI _promptText;
    GameObject      _panel;
    TextMeshProUGUI _displayText;
    TextMeshProUGUI _statusText;

    string _input   = "";
    bool   _unlocked;
    bool   _checking;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) _player = p.transform;

        if (vaultDoor == null)
        {
            vaultDoor = FindObjectOfType<VaultDoor>();
            if (vaultDoor == null) Debug.LogError("[VaultKeypad] No VaultDoor found in scene!");
            else Debug.Log("[VaultKeypad] Auto-found VaultDoor on: " + vaultDoor.gameObject.name);
        }

        BuildUI();
    }

    void Update()
    {
        if (_unlocked) return;

        float dist = _player != null
            ? Vector3.Distance(transform.position, _player.position)
            : float.MaxValue;

        if (!IsOpen)
        {
            _promptText.text = dist <= interactDistance ? "Press  E  to enter the passcode" : "";

            if (dist <= interactDistance && Input.GetKeyDown(KeyCode.E))
                OpenKeypad();
        }
        else
        {
            _promptText.text = "";

            // Auto-close if player walks too far away
            if (dist > interactDistance * 1.5f)
            {
                CloseKeypad();
                return;
            }

            if (!_checking) HandleInput();
            if (Input.GetKeyDown(KeyCode.Escape)) CloseKeypad();
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────

    void HandleInput()
    {
        for (int i = 0; i <= 9; i++)
        {
            if (_input.Length >= 4) break;
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                _input += i.ToString();
                RefreshDisplay();
                if (_input.Length == 4) StartCoroutine(CheckCode());
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) && _input.Length > 0)
        {
            _input = _input[..^1];
            RefreshDisplay();
        }
    }

    IEnumerator CheckCode()
    {
        _checking = true;
        yield return new WaitForSeconds(0.3f);

        if (_input == correctCode)
        {
            _statusText.text  = "ACCESS GRANTED";
            _statusText.color = new Color(0.1f, 0.85f, 0.3f);
            yield return new WaitForSeconds(1.2f);
            CloseKeypad();
            _unlocked = true;
            vaultDoor?.Open();
        }
        else
        {
            _statusText.text  = "WRONG CODE";
            _statusText.color = new Color(0.9f, 0.15f, 0.15f);
            yield return StartCoroutine(Shake(_panel.GetComponent<RectTransform>()));
            _input = "";
            _statusText.text = "";
            RefreshDisplay();
        }

        _checking = false;
    }

    IEnumerator Shake(RectTransform rt)
    {
        Vector2 origin = rt.anchoredPosition;
        float   t      = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            rt.anchoredPosition = origin + new Vector2(Mathf.Sin(t * 60f) * 10f, 0f);
            yield return null;
        }
        rt.anchoredPosition = origin;
    }

    // ── Keypad open / close ───────────────────────────────────────────────

    void OpenKeypad()
    {
        IsOpen = true;
        _input = "";
        _statusText.text = "";
        RefreshDisplay();
        _panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    void CloseKeypad()
    {
        IsOpen = false;
        _panel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ── Display ───────────────────────────────────────────────────────────

    void RefreshDisplay()
    {
        string s = "";
        for (int i = 0; i < 4; i++)
            s += (i < _input.Length ? "●" : "○") + (i < 3 ? "   " : "");
        _displayText.text = s;
    }

    // ── UI construction ───────────────────────────────────────────────────

    void BuildUI()
    {
        var cObj = new GameObject("VaultKeypadCanvas");
        var cv   = cObj.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 200;
        cObj.AddComponent<CanvasScaler>();
        cObj.AddComponent<GraphicRaycaster>();

        // Proximity prompt
        _promptText = MakeText(cObj.transform, "", 22f, Color.white,
            new Vector2(0f, 80f), new Vector2(700f, 50f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));

        // Dark panel
        _panel = new GameObject("VaultPanel");
        _panel.transform.SetParent(cObj.transform, false);
        var bg = _panel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.04f, 0.06f, 0.93f);
        var pr = _panel.GetComponent<RectTransform>();
        pr.anchorMin        = new Vector2(0.5f, 0.5f);
        pr.anchorMax        = new Vector2(0.5f, 0.5f);
        pr.pivot            = new Vector2(0.5f, 0.5f);
        pr.sizeDelta        = new Vector2(360f, 300f);
        pr.anchoredPosition = Vector2.zero;

        // Gold top bar
        var bar = new GameObject("TopBar");
        bar.transform.SetParent(_panel.transform, false);
        var barImg = bar.AddComponent<Image>();
        barImg.color = new Color(0.7f, 0.55f, 0.05f);
        var barR = bar.GetComponent<RectTransform>();
        barR.anchorMin        = new Vector2(0f, 1f);
        barR.anchorMax        = new Vector2(1f, 1f);
        barR.pivot            = new Vector2(0.5f, 1f);
        barR.offsetMin        = new Vector2(0f, -6f);
        barR.offsetMax        = Vector2.zero;

        MakeText(_panel.transform, "VAULT ACCESS",         26f, new Color(0.85f, 0.68f, 0.08f),
            new Vector2(0f,  105f), new Vector2(320f, 40f));
        MakeText(_panel.transform, "ENTER 4-DIGIT CODE",  13f, new Color(0.55f, 0.55f, 0.55f),
            new Vector2(0f,   72f), new Vector2(320f, 25f));

        // Digit dots
        var dObj = new GameObject("Digits");
        dObj.transform.SetParent(_panel.transform, false);
        _displayText           = dObj.AddComponent<TextMeshProUGUI>();
        _displayText.fontSize  = 40f;
        _displayText.color     = new Color(0.9f, 0.72f, 0.1f);
        _displayText.alignment = TextAlignmentOptions.Center;
        var dr = _displayText.GetComponent<RectTransform>();
        dr.anchorMin = dr.anchorMax = dr.pivot = new Vector2(0.5f, 0.5f);
        dr.sizeDelta        = new Vector2(300f, 60f);
        dr.anchoredPosition = new Vector2(0f, 15f);
        RefreshDisplay();

        // Status
        var sObj = new GameObject("Status");
        sObj.transform.SetParent(_panel.transform, false);
        _statusText           = sObj.AddComponent<TextMeshProUGUI>();
        _statusText.text      = "";
        _statusText.fontSize  = 18f;
        _statusText.color     = Color.red;
        _statusText.alignment = TextAlignmentOptions.Center;
        var sr = _statusText.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
        sr.sizeDelta        = new Vector2(300f, 40f);
        sr.anchoredPosition = new Vector2(0f, -35f);

        MakeText(_panel.transform, "Type digits  ·  Backspace to clear  ·  Esc to cancel",
            10f, new Color(0.38f, 0.38f, 0.38f),
            new Vector2(0f, -105f), new Vector2(320f, 30f));

        _panel.SetActive(false);
    }

    TextMeshProUGUI MakeText(Transform parent, string text, float size, Color color,
                             Vector2 pos, Vector2 sizeDelta,
                             Vector2? anchorMin = null, Vector2? anchorMax = null)
    {
        var obj = new GameObject("T");
        obj.transform.SetParent(parent, false);
        var tmp           = obj.AddComponent<TextMeshProUGUI>();
        tmp.text          = text;
        tmp.fontSize      = size;
        tmp.color         = color;
        tmp.alignment     = TextAlignmentOptions.Center;
        var r             = tmp.GetComponent<RectTransform>();
        r.anchorMin       = anchorMin ?? new Vector2(0.5f, 0.5f);
        r.anchorMax       = anchorMax ?? new Vector2(0.5f, 0.5f);
        r.pivot           = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta       = sizeDelta;
        return tmp;
    }
}
