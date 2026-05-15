using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SecurityCamera : MonoBehaviour
{
    [Header("Rotation Settings")]
    public bool isRotating = true;
    public float rotationSpeed = 30f;
    public float minAngle = 30f;
    public float maxAngle = 100f;

    [Header("Tilt")]
    [Range(-90f, 90f)]
    public float tiltAngle = 0f;

    [Header("Cone Shape")]
    public float coneVisualLength = 0.4f;
    public float coneDetectionLength = 3f;
    [Range(1f, 89f)]
    public float coneAngle = 25f;

    [Header("Cone Visual")]
    public Color coneColor = new Color(1f, 0.02f, 0.02f, 1f);
    [Tooltip("0 = visible, 1 = invisible")]
    [Range(0f, 1f)]
    public float coneTransparency = 0.9f;
    public int coneSegments = 24;
    public bool showCone = true;

    [Header("Detection")]
    public Transform spawnPoint;

    [Header("Caught Messages")]
    [TextArea] public string firstCaughtMessage = "You have been seen.";
    
    [System.Serializable]
    public class CatchThreshold
    {
        public int catchCount = 3;
        [TextArea] public string[] randomMessages = { "I see you again.", "Stop right there!" };
    }
    public List<CatchThreshold> thresholds = new List<CatchThreshold>();

    [Header("Message Settings")]
    public Color messageColor = Color.white;
    public float messageFontSize = 36f;
    public float messageDisplayTime = 3f;
    public float messageAreaWidth = 800f;
    public float messageAreaHeight = 100f;

    [Header("Bone Name")]
    public string headBoneName = "Head";

    private Transform headBone;
    private float currentAngle;
    private int direction = 1;
    private MeshFilter mf;
    private Mesh mesh;
    private Material coneMaterial;
    private Canvas canvas;
    private TextMeshProUGUI subtitleText;
    private float messageTimer = 0f;
    private bool showingMessage = false;
    private int catchCount = 0;
    private float respawnCooldown = 0f;

    void Start()
    {
        currentAngle = minAngle;
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == headBoneName) { headBone = t; break; }

        if (headBone != null)
        {
            GameObject coneObj = new GameObject("ConeVisual");
            coneObj.transform.SetParent(headBone, false);
            mf = coneObj.AddComponent<MeshFilter>();
            MeshRenderer mr = coneObj.AddComponent<MeshRenderer>();
            mesh = new Mesh();
            mf.mesh = mesh;
            coneMaterial = new Material(Shader.Find("Sprites/Default"));
            ApplyConeVisualSettings();
            coneMaterial.renderQueue = 3000;
            mr.material = coneMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        SetupUI();
    }

    void SetupUI()
    {
        GameObject canvasObj = new GameObject("SubtitleCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("SubtitleText");
        textObj.transform.SetParent(canvasObj.transform, false);
        subtitleText = textObj.AddComponent<TextMeshProUGUI>();
        subtitleText.text = "";
        subtitleText.color = messageColor;
        subtitleText.fontSize = messageFontSize;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.enableWordWrapping = true;

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 80f);
        rect.sizeDelta = new Vector2(messageAreaWidth, messageAreaHeight);
    }

    void Update()
    {
        if (headBone == null) return;

        if (isRotating)
        {
            currentAngle += rotationSpeed * direction * Time.deltaTime;
            if (currentAngle >= maxAngle) { currentAngle = maxAngle; direction = -1; }
            else if (currentAngle <= minAngle) { currentAngle = minAngle; direction = 1; }
            headBone.localRotation = Quaternion.Euler(tiltAngle, currentAngle, 0);
        }

        if (respawnCooldown > 0f)
            respawnCooldown -= Time.deltaTime;

        DetectPlayer();
        ApplyConeVisualSettings();
        if (showCone) BuildCone();

        if (showingMessage)
        {
            subtitleText.color = messageColor;
            subtitleText.fontSize = messageFontSize;
            RectTransform rect = subtitleText.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(messageAreaWidth, messageAreaHeight);
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) { subtitleText.text = ""; showingMessage = false; }
        }
    }

    string PickMessage()
    {
        // Find the highest threshold that has been reached
        CatchThreshold matched = null;
        foreach (var t in thresholds)
        {
            if (catchCount >= t.catchCount)
                matched = t;
        }

        // If a threshold is matched and has messages, pick random one
        if (matched != null && matched.randomMessages.Length > 0)
            return matched.randomMessages[Random.Range(0, matched.randomMessages.Length)];

        // Default first caught message
        return firstCaughtMessage;
    }

    public void ShowMessage()
    {
        if (subtitleText == null) return;
        subtitleText.text = PickMessage();
        subtitleText.color = messageColor;
        subtitleText.fontSize = messageFontSize;
        RectTransform rect = subtitleText.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(messageAreaWidth, messageAreaHeight);
        messageTimer = messageDisplayTime;
        showingMessage = true;
    }

    void DetectPlayer()
    {
        if (respawnCooldown > 0f) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || spawnPoint == null) return;

        Vector3 origin = headBone.position;
        Vector3 coneForward = headBone.TransformDirection(Vector3.forward);
        Vector3 toPlayer = (player.transform.position + Vector3.up * 1f) - origin;

        float dist = toPlayer.magnitude;
        float angle = Vector3.Angle(coneForward, toPlayer);

        if (dist > coneDetectionLength) return;
        if (angle > coneAngle) return;

        RaycastHit hit;
        if (Physics.Raycast(origin, toPlayer.normalized, out hit, dist))
            if (!hit.transform.root.CompareTag("Player")) return;

        catchCount++;
        // Disable CharacterController before teleporting to avoid physics snap issues
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.position = spawnPoint.position;
        if (cc != null) cc.enabled = true;
        respawnCooldown = 3f;   // 3-second grace period so player isn't caught instantly again
        ShowMessage();
    }

    void ApplyConeVisualSettings()
    {
        if (coneMaterial == null) return;

        float coneVisibility = 1f - Mathf.Clamp01(coneTransparency);
        Color renderColor = new Color(
            coneColor.r * coneVisibility,
            coneColor.g * coneVisibility,
            coneColor.b * coneVisibility,
            coneVisibility
        );
        coneMaterial.color = renderColor;
    }

    void BuildCone()
    {
        if (mf == null) return;
        float baseRadius = Mathf.Tan(coneAngle * Mathf.Deg2Rad) * coneVisualLength;
        Vector3 tip = Vector3.zero;
        Vector3 baseCenter = Vector3.forward * coneVisualLength;
        Vector3[] verts = new Vector3[coneSegments + 2];
        int[] tris = new int[coneSegments * 6];
        verts[0] = tip;
        verts[1] = baseCenter;
        for (int i = 0; i < coneSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / coneSegments;
            verts[i + 2] = baseCenter + new Vector3(Mathf.Cos(angle) * baseRadius, Mathf.Sin(angle) * baseRadius, 0f);
        }
        for (int i = 0; i < coneSegments; i++)
        {
            int next = (i + 1) % coneSegments;
            int t = i * 6;
            tris[t] = 0; tris[t+1] = i+2; tris[t+2] = next+2;
            tris[t+3] = 0; tris[t+4] = next+2; tris[t+5] = i+2;
        }
        mesh.Clear();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
    }
}
