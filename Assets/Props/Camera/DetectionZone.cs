using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DetectionZone : MonoBehaviour
{
    [Header("Respawn")]
    public Transform spawnPoint;

    [Header("Caught Message")]
    [TextArea] public string caughtMessage = "You have been caught";

    [Header("Message Settings")]
    public Color messageColor       = Color.white;
    public float messageFontSize    = 36f;
    public float messageDisplayTime = 3f;
    public float messageAreaWidth   = 800f;
    public float messageAreaHeight  = 100f;

    TextMeshProUGUI _subtitleText;
    float _messageTimer;
    bool  _showingMessage;
    bool  _triggered;

    // Cached collider data used for OverlapBox polling
    Collider[] _colliders;

    void Awake()
    {
        _colliders = GetComponents<Collider>();
        bool hasBox = false;

        foreach (Collider col in _colliders)
        {
            if (col is MeshCollider mc)
                mc.convex = true;
            col.isTrigger = true;
            if (col is BoxCollider) hasBox = true;
        }

        // CharacterController only reliably fires OnTriggerEnter/Stay on objects
        // that have a Rigidbody. Add a kinematic one automatically for pressure plates.
        if (hasBox && GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb    = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic  = true;
            rb.useGravity   = false;
        }
    }

    void Start() => SetupUI();

    void Update()
    {
        // Message countdown
        if (_showingMessage)
        {
            _messageTimer -= Time.deltaTime;
            if (_messageTimer <= 0f)
            {
                if (_subtitleText != null) _subtitleText.text = "";
                _showingMessage = false;
                _triggered      = false;
            }
        }

        // Active polling — works reliably for both lasers and flat pressure plates
        // with CharacterController, which often misses OnTriggerEnter on floor triggers.
        if (_triggered || _colliders == null) return;

        // Only poll with OverlapBox for BoxColliders (flat pressure plates).
        // MeshColliders (lasers) rely on OnTriggerEnter/Stay which work correctly for them.
        foreach (Collider col in _colliders)
        {
            if (col == null || !(col is BoxCollider bc)) continue;

            Bounds b = bc.bounds;
            Vector3 center = b.center + Vector3.up * 0.3f;
            Vector3 half   = b.extents + new Vector3(0f, 0.3f, 0f);

            Collider[] hits = Physics.OverlapBox(center, half, transform.rotation,
                ~0, QueryTriggerInteraction.Ignore);

            foreach (Collider hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    Trigger(hit.gameObject);
                    return;
                }
            }
        }
    }

    // Keep trigger callbacks as well for lasers (they still work fine vertically)
    void OnTriggerEnter(Collider other) { if (other.CompareTag("Player")) Trigger(other.gameObject); }
    void OnTriggerStay(Collider other)  { if (other.CompareTag("Player")) Trigger(other.gameObject); }

    void Trigger(GameObject player)
    {
        if (_triggered) return;
        _triggered = true;

        Transform spawn = spawnPoint;
        if (spawn == null && UVRevealController.Instance != null)
            spawn = UVRevealController.Instance.spawnPoint;

        if (spawn != null)
            player.transform.position = spawn.position;

        UVRevealController.ResetOnDeath();

        if (_subtitleText != null)
        {
            _subtitleText.text     = caughtMessage;
            _subtitleText.color    = messageColor;
            _subtitleText.fontSize = messageFontSize;
            _subtitleText.GetComponent<RectTransform>().sizeDelta =
                new Vector2(messageAreaWidth, messageAreaHeight);
        }

        _messageTimer   = messageDisplayTime;
        _showingMessage = true;
    }

    void SetupUI()
    {
        GameObject canvasObj = new GameObject("DetectionZoneCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("CaughtText");
        textObj.transform.SetParent(canvasObj.transform, false);

        _subtitleText = textObj.AddComponent<TextMeshProUGUI>();
        _subtitleText.text               = "";
        _subtitleText.color              = messageColor;
        _subtitleText.fontSize           = messageFontSize;
        _subtitleText.alignment          = TextAlignmentOptions.Center;
        _subtitleText.enableWordWrapping = true;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0f);
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 80f);
        rect.sizeDelta        = new Vector2(messageAreaWidth, messageAreaHeight);
    }
}
