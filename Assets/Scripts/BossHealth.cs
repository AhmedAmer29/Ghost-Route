using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHits = 4;
    public int hitsTaken = 0;

    [Header("Death")]
    public float fallDuration = 1.5f;
    public float destroyDelay = 6f;

    [Header("Reward gating")]
    [Tooltip("Drag the blocker GameObject here (preferred). Used over the name lookup.")]
    public GameObject blockerObject;
    [Tooltip("Fallback name lookup if blockerObject is empty")]
    public string objectToRemoveOnKey = "SerwersP_013 (1)";
    [Tooltip("If true: remove the blocker the moment the boss dies, ignoring circuits")]
    public bool removeOnDeathOnly = false;
    [Tooltip("Seconds the 'Now you have the key' message stays on screen")]
    public float keyMessageDuration = 3f;

    private bool _dead;
    private bool _blockerRemoved;
    public static bool HasKey { get; private set; }

    public bool IsDead => _dead;

    public void TakeHit()
    {
        if (_dead) return;
        hitsTaken++;
        Debug.Log($"[BossHealth] Hit {hitsTaken}/{maxHits}");
        if (hitsTaken >= maxHits) Die();
    }

    void Die()
    {
        _dead = true;
        HasKey = true;
        Debug.Log("[BossHealth] Boss defeated. HasKey = true.");

        var ai = GetComponent<SewerEnemyAI>();
        if (ai != null) ai.enabled = false;

        var agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        var anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;

        StartCoroutine(DeathSequence());
        StartCoroutine(BlockerRemovalRoutine());
    }

    IEnumerator DeathSequence()
    {
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.AngleAxis(90f, transform.right) * start;
        float t = 0f;
        while (t < fallDuration)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, end, t / fallDuration);
            yield return null;
        }
        transform.rotation = end;

        GameObject msg = BuildMessage("Now you have the key");
        yield return new WaitForSeconds(keyMessageDuration);
        if (msg != null) Destroy(msg);

        yield return new WaitForSeconds(Mathf.Max(0f, destroyDelay - keyMessageDuration));
        gameObject.SetActive(false);
    }

    IEnumerator BlockerRemovalRoutine()
    {
        if (removeOnDeathOnly)
        {
            Debug.Log("[BossHealth] removeOnDeathOnly=true → removing blocker immediately on death");
            yield return new WaitForSeconds(1.0f);
            TryRemoveBlocker();
            yield break;
        }

        var mps = Object.FindFirstObjectByType<MasterPowerSystem>();
        if (mps == null)
        {
            Debug.LogWarning("[BossHealth] No MasterPowerSystem found → removing blocker immediately");
            TryRemoveBlocker();
            yield break;
        }

        Debug.Log($"[BossHealth] Waiting on circuits: {mps.fixedCount}/{mps.targetCount}");
        int last = -1;
        while (mps != null && mps.fixedCount < mps.targetCount)
        {
            if (mps.fixedCount != last)
            {
                Debug.Log($"[BossHealth] Circuits: {mps.fixedCount}/{mps.targetCount}");
                last = mps.fixedCount;
            }
            yield return null;
        }

        Debug.Log("[BossHealth] Circuits complete → showing message + removing blocker");
        GameObject msg = BuildMessage("All systems online — way out has opened");
        yield return new WaitForSeconds(2.5f);
        if (msg != null) Destroy(msg);

        TryRemoveBlocker();
    }

    // Hook this up to any UnityEvent / context-menu / debug button to force the gate open
    [ContextMenu("Force Remove Blocker")]
    public void ForceRemoveBlocker()
    {
        Debug.Log("[BossHealth] ForceRemoveBlocker() called");
        TryRemoveBlocker();
    }

    void TryRemoveBlocker()
    {
        if (_blockerRemoved)
        {
            Debug.Log("[BossHealth] Blocker already removed, skipping");
            return;
        }

        GameObject blocker = ResolveBlocker();

        if (blocker != null)
        {
            Debug.Log($"<color=lime>[BossHealth] Disabling blocker '{blocker.name}' (was active={blocker.activeSelf})</color>");
            blocker.SetActive(false);
            _blockerRemoved = true;
        }
        else
        {
            Debug.LogError($"[BossHealth] Couldn't find blocker. Direct ref={(blockerObject==null?"null":blockerObject.name)} nameLookup='{objectToRemoveOnKey}'. Try assigning blockerObject directly in inspector.");
        }
    }

    GameObject ResolveBlocker()
    {
        // 1. Prefer the direct GameObject reference (works for inactive parents too)
        if (blockerObject != null) return blockerObject;

        if (string.IsNullOrEmpty(objectToRemoveOnKey)) return null;

        // 2. Fast path: GameObject.Find (active roots only)
        GameObject found = GameObject.Find(objectToRemoveOnKey);
        if (found != null) return found;

        // 3. Last resort: iterate all transforms including inactive ones
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t == null || t.gameObject == null) continue;
            if (t.gameObject.scene.IsValid() == false) continue;       // skip prefab assets
            if (t.gameObject.name == objectToRemoveOnKey)
                return t.gameObject;
        }
        return null;
    }

    GameObject BuildMessage(string text)
    {
        GameObject cObj = new GameObject("BossMessageCanvas");
        Canvas canvas = cObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        cObj.AddComponent<CanvasScaler>();

        GameObject txtObj = new GameObject("BossMessageText");
        txtObj.transform.SetParent(cObj.transform, false);
        Text label = txtObj.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 56;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.9f, 0.3f, 1f);

        RectTransform rt = label.rectTransform;
        rt.anchorMin = new Vector2(0f, 0.55f);
        rt.anchorMax = new Vector2(1f, 0.75f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        txtObj.AddComponent<Shadow>().effectColor = Color.black;

        return cObj;
    }
}
