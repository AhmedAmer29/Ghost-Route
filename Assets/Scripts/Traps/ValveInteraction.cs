using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ValveInteraction : MonoBehaviour
{
    [Header("Settings")]
    public string targetValveName = "Pipe_P (1)"; 
    public float interactionDistance = 5f; 
    public float spamRequirement = 100f;   
    public float spamPower = 4.5f;        
    public float decayRate = 5f;          
    public Vector3 valveRotationAxis = Vector3.forward; 
    public float maxValveRotation = 720f; 

    [Header("Door / Reward")]
    public Transform doorToOpen;

    [Header("Gauge Colors")]
    public Color zoneRed = new Color(0.8f, 0.1f, 0.1f, 1f);
    public Color zoneYellow = new Color(0.9f, 0.8f, 0.1f, 1f);
    public Color zoneGreen = new Color(0.1f, 0.9f, 0.3f, 1f);

    [Header("Status")]
    public bool isInteracting;
    public float currentProgress;
    public bool isFinished;

    private Transform _valveHandle;
    private Quaternion _initialValveRotation;
    private Transform _player;
    private Component _playerControls;
    private Canvas _valveCanvas;
    private Image _arcFill;
    private RectTransform _indicator;
    private RectTransform _spamPromptRect;
    private Text _spamPromptText;
    private GameObject _promptUI;
    private RisingWater _waterTrap;

    private float _pulseTimer;
    private Sprite _circleSprite;

    void Start()
    {
        _valveHandle = transform.Find(targetValveName);
        if (_valveHandle == null) _valveHandle = transform;
        
        _initialValveRotation = _valveHandle.localRotation;

        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) _player = pObj.transform;
        else _player = Camera.main.transform.parent;

        _waterTrap = FindObjectOfType<RisingWater>();
        
        // Generate a circular sprite programmatically so it doesn't look like a square
        _circleSprite = CreateCircleSprite();
        CreateUI();
    }

    Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < radius)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (isFinished) return;

        float dist = Vector3.Distance(transform.position, Camera.main.transform.position);
        bool inRange = dist <= interactionDistance;

        if (!isInteracting)
        {
            UpdatePrompt(inRange);
            if (inRange && Input.GetKeyDown(KeyCode.E)) StartInteraction();
        }
        else
        {
            UpdateInteraction();
            if (Input.GetKeyDown(KeyCode.E) || dist > interactionDistance + 2f) StopInteraction();
        }
    }

    void StartInteraction()
    {
        isInteracting = true;
        SetPlayerEnabled(false);
        if (_promptUI) _promptUI.SetActive(false);
        if (_valveCanvas) _valveCanvas.enabled = true;
    }

    void StopInteraction()
    {
        isInteracting = false;
        SetPlayerEnabled(true);
        if (_valveCanvas) _valveCanvas.enabled = false;
    }

    void UpdateInteraction()
    {
        currentProgress = Mathf.Max(0f, currentProgress - decayRate * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentProgress += spamPower;
            _pulseTimer = 0.2f;
            StartCoroutine(ShakeCamera(0.05f));
        }

        UpdateUIVisuals();
        UpdateValveModel();

        if (currentProgress >= spamRequirement) Finish();
    }

    void Finish()
    {
        isFinished = true;
        isInteracting = false;
        currentProgress = spamRequirement;
        
        UpdateValveModel();
        SetPlayerEnabled(true);
        if (_valveCanvas) _valveCanvas.enabled = false;

        Debug.Log("<color=green>[ValveInteraction] SPAMMING COMPLETE! Opening Door...</color>");

        if (doorToOpen != null)
        {
            var ds = doorToOpen.GetComponent<ElectricalBoxDoor>();
            if (ds != null)
            {
                ds.Open();
            }
            else
            {
                // FORCE OPEN FALLBACK: If no script, just rotate it manually
                Debug.Log("[ValveInteraction] No door script found, using manual fallback rotation.");
                StartCoroutine(ManualDoorOpen(doorToOpen));
            }
        }

        if (_waterTrap != null)
        {
            var field = _waterTrap.GetType().GetField("_rising", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(_waterTrap, false);
            Debug.Log("[ValveInteraction] Water stopped rising.");
        }
    }

    IEnumerator ManualDoorOpen(Transform door)
    {
        Quaternion start = door.localRotation;
        Quaternion target = start * Quaternion.Euler(0, -110, 0); // Open 110 degrees
        float elapsed = 0f;
        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime * 2f;
            door.localRotation = Quaternion.Slerp(start, target, elapsed);
            yield return null;
        }
    }

    void UpdateValveModel()
    {
        if (_valveHandle == null) return;
        float t = currentProgress / spamRequirement;
        _valveHandle.localRotation = _initialValveRotation * Quaternion.Euler(valveRotationAxis * (t * maxValveRotation));
    }

    void SetPlayerEnabled(bool enabled)
    {
        if (_playerControls == null && _player != null)
        {
            _playerControls = _player.GetComponent("PlayerControls");
            if (_playerControls == null) _playerControls = _player.GetComponent("PlayerMovement");
        }

        if (_playerControls != null)
        {
            var prop = _playerControls.GetType().GetProperty("enabled");
            if (prop != null) prop.SetValue(_playerControls, enabled, null);
        }
        
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

    void UpdatePrompt(bool inRange)
    {
        if (_promptUI != null)
        {
            _promptUI.SetActive(inRange);
            float pulse = 0.7f + Mathf.Sin(Time.time * 5f) * 0.3f;
            _promptUI.GetComponent<Text>().color = new Color(1, 1, 1, pulse);
        }
    }

    void UpdateUIVisuals()
    {
        if (_indicator == null) return;
        float t = currentProgress / spamRequirement;
        
        // Map 0-1 to +90 to -90 degrees (9 o'clock to 3 o'clock)
        float targetAngle = 90f - (t * 180f); 
        float jitter = (_pulseTimer > 0) ? Random.Range(-5f, 5f) : 0f;
        _indicator.localRotation = Quaternion.Euler(0, 0, targetAngle + jitter);

        if (_spamPromptRect != null)
        {
            _pulseTimer = Mathf.Max(0f, _pulseTimer - Time.deltaTime);
            float scale = 1f + (_pulseTimer > 0 ? 0.2f * (_pulseTimer / 0.2f) : 0f);
            _spamPromptRect.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void CreateUI()
    {
        GameObject canvasObj = new GameObject("ValveInteractionCanvas");
        _valveCanvas = canvasObj.AddComponent<Canvas>();
        _valveCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _valveCanvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        _valveCanvas.enabled = false;

        // Container
        GameObject gaugeRoot = new GameObject("GaugeRoot");
        gaugeRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform gaugeRT = gaugeRoot.AddComponent<RectTransform>();
        gaugeRT.sizeDelta = new Vector2(400, 400);
        gaugeRT.anchoredPosition = new Vector2(0, 50);

        // BACKGROUND SEGMENTS (THE GAUGE - Perfect Top Half Alignment)
        CreateSegment(gaugeRoot.transform, "Red", zoneRed, 0.166f, 90f);
        CreateSegment(gaugeRoot.transform, "Yellow", zoneYellow, 0.166f, 30f);
        CreateSegment(gaugeRoot.transform, "Green", zoneGreen, 0.166f, -30f);

        // Center Hole (Makes it a Ring)
        GameObject hole = new GameObject("Hole");
        hole.transform.SetParent(gaugeRoot.transform, false);
        Image holeImg = hole.AddComponent<Image>();
        holeImg.sprite = _circleSprite;
        holeImg.color = new Color(0, 0, 0, 0.85f);
        holeImg.rectTransform.sizeDelta = new Vector2(250, 250);

        // THE NEEDLE (Car Style)
        GameObject ind = new GameObject("Needle");
        ind.transform.SetParent(gaugeRoot.transform, false);
        _indicator = ind.AddComponent<RectTransform>();
        _indicator.sizeDelta = new Vector2(6, 175);
        Image indImg = ind.AddComponent<Image>();
        indImg.color = new Color(1, 0.1f, 0.1f); // Red needle looks cooler
        indImg.rectTransform.pivot = new Vector2(0.5f, 0f);
        ind.AddComponent<Outline>().effectColor = Color.black;

        // SPAM PROMPT
        GameObject textObj = new GameObject("SpamPrompt");
        textObj.transform.SetParent(canvasObj.transform, false);
        _spamPromptRect = textObj.AddComponent<RectTransform>();
        _spamPromptText = textObj.AddComponent<Text>();
        _spamPromptText.text = "SPAM SPACE";
        _spamPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _spamPromptText.fontSize = 36;
        _spamPromptText.fontStyle = FontStyle.Bold;
        _spamPromptText.alignment = TextAnchor.MiddleCenter;
        _spamPromptRect.sizeDelta = new Vector2(500, 100);
        _spamPromptRect.anchoredPosition = new Vector2(0, -20);
        textObj.AddComponent<Shadow>().effectColor = Color.black;

        // PRESS E PROMPT
        _promptUI = new GameObject("InteractPrompt");
        _promptUI.transform.SetParent(canvasObj.transform, false);
        Text pt = _promptUI.AddComponent<Text>();
        pt.text = "[E] INTERACT WITH VALVE";
        pt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        pt.fontSize = 26;
        pt.fontStyle = FontStyle.Bold;
        pt.alignment = TextAnchor.MiddleCenter;
        pt.rectTransform.sizeDelta = new Vector2(600, 100);
        pt.rectTransform.anchoredPosition = new Vector2(0, -180);
        _promptUI.AddComponent<Shadow>().effectColor = Color.black;
        _promptUI.SetActive(false);

        DontDestroyOnLoad(canvasObj);
    }

    void CreateSegment(Transform parent, string name, Color c, float fill, float rotation)
    {
        GameObject seg = new GameObject(name);
        seg.transform.SetParent(parent, false);
        Image img = seg.AddComponent<Image>();
        img.sprite = _circleSprite; 
        img.color = c;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillAmount = fill;
        img.rectTransform.sizeDelta = new Vector2(380, 380);
        img.rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
    }

    IEnumerator ShakeCamera(float duration)
    {
        Vector3 op = Camera.main.transform.localPosition;
        float e = 0f;
        while (e < duration) {
            Camera.main.transform.localPosition = op + Random.insideUnitSphere * 0.05f;
            e += Time.deltaTime;
            yield return null;
        }
        Camera.main.transform.localPosition = op;
    }
}
