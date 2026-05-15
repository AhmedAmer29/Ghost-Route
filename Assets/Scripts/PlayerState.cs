using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PlayerState : MonoBehaviour
{
    [Header("Respawn")]
    [Tooltip("Seconds to wait after death before respawning by reloading the scene.")]
    public float respawnDelay = 2.5f;
    [Tooltip("If false, dying just freezes the player and waits for code to call Respawn() manually.")]
    public bool autoRespawnOnDeath = true;

    [Header("Noise")]
    [Range(0f, 1f)] public float noiseLevel;
    public float noiseBuildRate = 2.5f;
    public float noiseDecayRate = 1.2f;
    public float noiseThreshold_Loud = 0.6f;

    [Header("Water / Drowning")]
    public bool isInWater;
    public bool isSubmerged;
    public float drownDuration = 8f;
    public float drownTimer;

    [Header("Silt / Sinking")]
    public bool isInSilt;
    public bool isSinking;
    public float sinkSpeed = 0.1f;
    public float sinkTimer;
    public float siltSuffocateTime = 5f;

    [Header("Climbing")]
    public bool isClimbing;
    public float climbSpeed = 3f;

    [Header("Read-only")]
    public float currentSpeed;
    public float verticalVelocity;
    public bool isRunning;
    public bool isCrouching;
    public bool isGrounded;

    private Component _controls;
    private Component _combat;
    private Animator _anim;
    private CharacterController _cc;
    private float _originalHeight;
    private float _sinkOffset;

    void Start()
    {
        _controls = GetComponent("PlayerControls");
        if (_controls == null) _controls = GetComponent("PlayerMovement");

        _combat = GetComponent("PlayerCombat");
        _anim = GetComponentInChildren<Animator>();
        _cc = GetComponent<CharacterController>();
        if (_cc != null) _originalHeight = _cc.height;
        _sinkOffset = 0f;
    }

    void Update()
    {
        ReadPlayerState();
        UpdateNoise();
        UpdateDrowning();
        UpdateSinking();

        if (deathCooldown > 0f)
            deathCooldown -= Time.deltaTime;
    }

    void ReadPlayerState()
    {
        if (_controls != null)
        {
            var spd = _controls.GetType().GetField("currentSpeed");
            var run = _controls.GetType().GetField("isRunning");
            var crc = _controls.GetType().GetField("isCrouching");
            var grd = _controls.GetType().GetField("isGrounded");

            if (spd != null) currentSpeed = (float)spd.GetValue(_controls);
            if (run != null) isRunning = (bool)run.GetValue(_controls);
            if (crc != null) isCrouching = (bool)crc.GetValue(_controls);
            if (grd != null) isGrounded = (bool)grd.GetValue(_controls);
        }

        if (_cc != null)
            verticalVelocity = _cc.velocity.y;
    }

    void UpdateNoise()
    {
        float target = 0f;

        if (isRunning)
            target = 0.8f;
        else if (currentSpeed > 0.2f)
            target = 0.4f;

        if (isInWater && currentSpeed > 0.1f)
            target = Mathf.Max(target, 0.7f);

        if (isRunning && !isGrounded)
            target = Mathf.Max(target, 0.9f);

        if (IsAttacking())
            target = Mathf.Max(target, 0.7f);

        float rate = (target > noiseLevel) ? noiseBuildRate : noiseDecayRate;
        noiseLevel = Mathf.MoveTowards(noiseLevel, target, Time.deltaTime * rate);
    }

    bool IsAttacking()
    {
        if (_anim == null) return false;
        var info = _anim.GetCurrentAnimatorStateInfo(0);
        return info.IsName("Punch") || info.IsName("Kick");
    }

    public bool IsLoud()
    {
        return noiseLevel >= noiseThreshold_Loud;
    }

    void UpdateDrowning()
    {
        if (isSubmerged)
        {
            drownTimer += Time.deltaTime;
            if (drownTimer >= drownDuration)
                TriggerDeath("Drowned");
        }
        else if (!isInWater)
        {
            drownTimer = Mathf.Max(0f, drownTimer - Time.deltaTime * 2f);
        }
    }

    void UpdateSinking()
    {
        if (isSinking)
        {
            sinkTimer += Time.deltaTime;
            _sinkOffset += sinkSpeed * Time.deltaTime;

            if (_cc != null)
                _cc.height = Mathf.Max(0.3f, _originalHeight - _sinkOffset);

            if (sinkTimer >= siltSuffocateTime)
                TriggerDeath("Suffocated in silt");
        }
        else if (!isInSilt)
        {
            sinkTimer = Mathf.Max(0f, sinkTimer - Time.deltaTime * 3f);
            _sinkOffset = Mathf.MoveTowards(_sinkOffset, 0f, Time.deltaTime * sinkSpeed * 2f);

            if (_cc != null)
                _cc.height = Mathf.Lerp(_cc.height, _originalHeight, Time.deltaTime * 5f);
        }
    }

    public float GetSinkOffset()
    {
        return _sinkOffset;
    }

    public float GetSuffocationProgress()
    {
        return isSinking ? sinkTimer / siltSuffocateTime : 0f;
    }

    public float GetDrownProgress()
    {
        return isSubmerged ? drownTimer / drownDuration : 0f;
    }

    public bool isDead;
    public float deathCooldown;

    public void TriggerDeath(string cause)
    {
        if (isDead) return;
        isDead = true;
        deathCooldown = 3f;
        Debug.Log($"<color=red>[PlayerState] DEATH: {cause}</color>");

        var kr = GetComponent<PlayerKnockbackReceiver>();
        if (kr != null && !kr.IsStunned)
            kr.ApplyKnockback(-transform.forward, 2f, 0f, 3f);

        if (_cc != null) _cc.enabled = false;
        if (_controls != null)
        {
            var en = _controls.GetType().GetProperty("enabled");
            if (en != null) en.SetValue(_controls, false, null);
        }

        if (autoRespawnOnDeath)
            StartCoroutine(DeathToRespawn(cause));
    }

    IEnumerator DeathToRespawn(string cause)
    {
        // Build a black overlay + "YOU DIED — Drowned" message so the player
        // sees feedback rather than just freezing in place.
        GameObject canvasGO = new GameObject("DeathCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;
        canvasGO.AddComponent<CanvasScaler>();

        GameObject imgGO = new GameObject("Black");
        imgGO.transform.SetParent(canvasGO.transform, false);
        Image img = imgGO.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        GameObject txtGO = new GameObject("DeathText");
        txtGO.transform.SetParent(canvasGO.transform, false);
        Text label = txtGO.AddComponent<Text>();
        label.text = $"YOU DIED\n<size=28>{cause}</size>";
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 56;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(1f, 0.2f, 0.2f, 0f);
        label.supportRichText = true;
        RectTransform lrt = label.rectTransform;
        lrt.anchorMin = new Vector2(0, 0.35f);
        lrt.anchorMax = new Vector2(1, 0.65f);
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        // Fade in over the first half, hold, then reload.
        float fadeIn = Mathf.Min(1f, respawnDelay * 0.5f);
        float t = 0f;
        while (t < fadeIn)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeIn);
            img.color = new Color(0f, 0f, 0f, a * 0.92f);
            label.color = new Color(1f, 0.2f, 0.2f, a);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, respawnDelay - fadeIn));

        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"<color=yellow>[PlayerState] Respawning by reloading scene '{scene}'</color>");
        // Make sure controls and cursor are sane before the next scene takes over.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(scene);
    }

    public void Respawn()
    {
        isDead = false;
        deathCooldown = 0f;
        if (_cc != null) _cc.enabled = true;
        if (_controls != null)
        {
            var en = _controls.GetType().GetProperty("enabled");
            if (en != null) en.SetValue(_controls, true, null);
        }
    }
}