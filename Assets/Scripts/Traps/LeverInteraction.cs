using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class LeverInteraction : MonoBehaviour
{
    [System.Serializable]
    public class WireConfig
    {
        public string label = "Wire";
        public Color  color = Color.white;
        public bool   isReal = true;
    }

    [Header("Lever Settings")]
    public float     interactionDistance = 3f;
    public Transform leverHandle;                         
    private Transform _targetTransform;                   
    
    [Tooltip("Try changing this if it rotates the wrong way (e.g., 60,0,0 or 0,0,60)")]
    public Vector3   pullRotation = new Vector3(60, 0, 0);
    public float     pullSpeed    = 2f;

    [Header("On Pull Complete")]
    public UnityEvent onLeverPulled;                      

    [HideInInspector] public bool isLocked = true;

    private bool    _pulled;
    private Canvas  _promptCanvas;
    private Text    _promptText;

    void Start()
    {
        // 1. Find the Handle
        if (leverHandle == null) leverHandle = transform.Find("Handle");

        if (leverHandle != null)
        {
            // FORCE STATIC OFF (Crucial!)
            leverHandle.gameObject.isStatic = false;

            // 2. Detect Bone (For Skinned Meshes)
            SkinnedMeshRenderer smr = leverHandle.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.rootBone != null)
            {
                _targetTransform = smr.rootBone;
                _targetTransform.gameObject.isStatic = false;
                Debug.Log($"[Lever] Rotating root bone: {_targetTransform.name}");
            }
            else
            {
                _targetTransform = leverHandle;
            }
        }

        BuildPromptUI();
    }

    [ContextMenu("TEST PULL")]
    public void TestPull()
    {
        if (Application.isPlaying) PullLever();
        else Debug.LogWarning("Please enter Play Mode to test the lever pull!");
    }

    void Update()
    {
        if (_pulled) return;

        float dist    = Vector3.Distance(transform.position, Camera.main.transform.position);
        bool  inRange = dist <= interactionDistance;

        UpdatePromptUI(inRange);

        if (inRange && Input.GetKeyDown(KeyCode.E))
        {
            if (isLocked) FlashLocked();
            else PullLever();
        }
    }

    public void Unlock()
    {
        isLocked = false;
        if (_promptText != null) _promptText.color = Color.green;
    }

    void PullLever()
    {
        if (_pulled) return;
        _pulled = true;
        if (_promptCanvas != null) _promptCanvas.gameObject.SetActive(false);
        StartCoroutine(PullSequence());
    }

    IEnumerator PullSequence()
    {
        if (_targetTransform != null)
        {
            Quaternion startRot = _targetTransform.localRotation;
            Quaternion endRot   = startRot * Quaternion.Euler(pullRotation);
            
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * pullSpeed;
                _targetTransform.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            _targetTransform.localRotation = endRot;
        }

        yield return new WaitForSeconds(0.2f);
        StartCoroutine(ShowPowerOnFlash());
        onLeverPulled?.Invoke();
    }

    IEnumerator ShowPowerOnFlash()
    {
        GameObject flashObj = new GameObject("PowerOnFlash");
        Canvas c = flashObj.AddComponent<Canvas>();
        c.renderMode  = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 999;
        flashObj.AddComponent<CanvasScaler>();

        GameObject textObj = new GameObject("Txt");
        textObj.transform.SetParent(flashObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text      = "⚡  POWER RESTORED  ⚡";
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize  = 30;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.LowerLeft;
        t.color     = Color.green;
        t.rectTransform.anchorMin = new Vector2(0, 0);
        t.rectTransform.anchorMax = new Vector2(0, 0);
        t.rectTransform.pivot     = new Vector2(0, 0);
        t.rectTransform.anchoredPosition = new Vector2(30, 80);
        t.rectTransform.sizeDelta = new Vector2(420, 50);
        textObj.AddComponent<Shadow>().effectColor = Color.black;

        float timer = 2.5f;
        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            float pulse = 0.6f + Mathf.Sin(Time.time * 10f) * 0.4f;
            t.color = new Color(0f, 1f, 0f, Mathf.Clamp01(timer / 2.5f) * pulse);
            yield return null;
        }
        Destroy(flashObj);
    }

    void FlashLocked()
    {
        StartCoroutine(LockedFlash());
    }

    IEnumerator LockedFlash()
    {
        if (_promptText == null) yield break;
        string original = _promptText.text;
        _promptText.text = "⚠ POWER NOT RESTORED ⚠";
        _promptText.color = Color.red;
        yield return new WaitForSeconds(1.5f);
        _promptText.text = original;
    }

    void BuildPromptUI()
    {
        GameObject canvasObj = new GameObject("LeverPromptCanvas");
        _promptCanvas = canvasObj.AddComponent<Canvas>();
        _promptCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _promptCanvas.sortingOrder = 998;
        canvasObj.AddComponent<CanvasScaler>();

        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(canvasObj.transform, false);
        _promptText = textObj.AddComponent<Text>();
        _promptText.text      = "[E] PULL LEVER";
        _promptText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _promptText.fontSize  = 26;
        _promptText.fontStyle = FontStyle.Bold;
        _promptText.alignment = TextAnchor.LowerLeft;
        _promptText.rectTransform.anchorMin = new Vector2(0, 0);
        _promptText.rectTransform.anchorMax = new Vector2(0, 0);
        _promptText.rectTransform.pivot     = new Vector2(0, 0);
        _promptText.rectTransform.anchoredPosition = new Vector2(30, 80);
        _promptText.rectTransform.sizeDelta        = new Vector2(500, 50);
        _promptText.color = Color.white;
        textObj.AddComponent<Shadow>().effectColor = Color.black;

        _promptCanvas.gameObject.SetActive(false);
        DontDestroyOnLoad(canvasObj);
    }

    void UpdatePromptUI(bool show)
    {
        if (_promptCanvas == null || _pulled) return;
        _promptCanvas.gameObject.SetActive(show);
        if (show && _promptText != null)
        {
            float pulse = 0.65f + Mathf.Sin(Time.time * 4f) * 0.35f;
            Color baseColor = isLocked ? Color.white : Color.green;
            _promptText.color = new Color(baseColor.r, baseColor.g, baseColor.b, pulse);
            _promptText.text  = isLocked ? "[E] PULL LEVER  (LOCKED)" : "[E] PULL LEVER";
        }
    }
}
