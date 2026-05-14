using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Collider))]
public class RatSwarm : MonoBehaviour
{
    [Header("Triggers")]
    public float lingerThreshold = 60f; // Seconds in chamber before auto-trigger
    public float noiseThreshold = 0.6f; // How loud the player must be to trigger
    public float triggerRadius = 15f;   // Radius to detect player presence

    [Header("Visuals - Swarm")]
    public int ratCount = 400;
    public float ratSize = 0.12f;
    public Color ratColor = new Color(0.2f, 0.15f, 0.1f);
    public Material ratMaterial;

    [Header("Visuals - Warnings")]
    public float redEyesIntensity = 1f;
    public int redEyesCount = 30;

    [Header("Audio")]
    public AudioSource backgroundChittering;
    public AudioSource swarmScream;
    public AudioSource playerStruggling;

    [Header("Screenshake & Post FX")]
    public float shakeIntensity = 0.5f;
    public float blurMax = 2f;

    private bool _swarmActive;
    private bool _sequenceStarted;
    private float _lingerTimer;
    private PlayerState _player;
    private ParticleSystem _swarmParticles;
    private ParticleSystem _eyesParticles;
    private Collider _zone;
    private Image _redOverlay;

    void Start()
    {
        _zone = GetComponent<Collider>();
        _zone.isTrigger = true;

        SetupParticles();
        SetupUIOverlay();

        if (backgroundChittering != null)
        {
            backgroundChittering.loop = true;
            backgroundChittering.volume = 0.1f;
            backgroundChittering.Play();
        }
    }

    void SetupParticles()
    {
        // 1. Swarm Particles
        GameObject swarmGo = new GameObject("RatSwarmParticles");
        swarmGo.transform.SetParent(transform);
        _swarmParticles = swarmGo.AddComponent<ParticleSystem>();
        var main = _swarmParticles.main;
        main.startLifetime = 3f;
        main.startSpeed = 5f;
        main.startSize = ratSize;
        main.startColor = ratColor;
        main.maxParticles = ratCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = _swarmParticles.emission;
        emission.rateOverTime = 0; // Controlled by sequence

        var shape = _swarmParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(16.5f, 1f, 17f);

        var renderer = _swarmParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = ratMaterial != null ? ratMaterial : new Material(Shader.Find("Sprites/Default"));

        // 2. Eyes Particles (Warnings)
        GameObject eyesGo = new GameObject("RatEyesParticles");
        eyesGo.transform.SetParent(transform);
        _eyesParticles = eyesGo.AddComponent<ParticleSystem>();
        var eMain = _eyesParticles.main;
        eMain.startLifetime = 5f;
        eMain.startSize = 0.03f;
        eMain.startColor = Color.red;
        eMain.maxParticles = redEyesCount;
        
        var eEmission = _eyesParticles.emission;
        eEmission.rateOverTime = 5;

        var eShape = _eyesParticles.shape;
        eShape.shapeType = ParticleSystemShapeType.Box;
        eShape.scale = new Vector3(15, 0.5f, 15);

        _eyesParticles.Play();
    }

    void SetupUIOverlay()
    {
        // Find Canvas or create a simple GUI overlay later if needed.
        // For now we'll try to find the ScreenFader's canvas.
        if (ScreenFader.Instance != null && ScreenFader.Instance.fadeImage != null)
        {
            GameObject go = new GameObject("RatBiteOverlay");
            go.transform.SetParent(ScreenFader.Instance.fadeImage.canvas.transform);
            _redOverlay = go.AddComponent<Image>();
            _redOverlay.color = new Color(1, 0, 0, 0);
            _redOverlay.raycastTarget = false;
            
            RectTransform rt = _redOverlay.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.one;
        }
    }

    void Update()
    {
        if (_sequenceStarted || _player == null) return;

        // Trigger 1: Lingering
        _lingerTimer += Time.deltaTime;
        
        // Trigger 2: Noise
        bool tooLoud = _player.IsLoud();

        if (tooLoud || _lingerTimer >= lingerThreshold)
        {
            StartCoroutine(DeathSequence());
        }

        // Warning chittering increases with linger timer
        if (backgroundChittering != null)
        {
            backgroundChittering.volume = Mathf.Lerp(0.1f, 0.4f, _lingerTimer / lingerThreshold);
        }
    }

    IEnumerator DeathSequence()
    {
        _sequenceStarted = true;
        _swarmActive = true;

        // PHASE 1: Initial Disturbance (0-3s)
        Debug.Log("[RatSwarm] Disturbance detected...");
        if (backgroundChittering != null) backgroundChittering.volume = 0.8f;
        
        float t = 0f;
        while (t < 3f)
        {
            t += Time.deltaTime;
            // Slight camera shake starts
            Camera.main.transform.localPosition += Random.insideUnitSphere * 0.01f;
            yield return null;
        }

        // PHASE 2: The Swarm Begins (3-6s)
        Debug.Log("[RatSwarm] SWARM START!");
        if (swarmScream != null) swarmScream.Play();
        
        var em = _swarmParticles.emission;
        em.rateOverTime = ratCount;
        _swarmParticles.Play();

        t = 0f;
        while (t < 3f)
        {
            t += Time.deltaTime;
            Camera.main.transform.localPosition += Random.insideUnitSphere * 0.05f;
            yield return null;
        }

        // PHASE 3: Overwhelm (6-10s)
        Debug.Log("[RatSwarm] Overwhelmed!");
        if (playerStruggling != null) playerStruggling.Play();

        // Lockdown player movement
        _player.TriggerDeath("Overwhelmed by Rat Swarm");

        t = 0f;
        while (t < 4f)
        {
            t += Time.deltaTime;
            
            // Camera shake increases
            Camera.main.transform.localPosition += Random.insideUnitSphere * (shakeIntensity * (t / 4f));
            
            // Red bite flashes
            if (Random.value > 0.8f) StartCoroutine(BiteFlash());

            // Darken screen gradually
            if (ScreenFader.Instance != null)
                ScreenFader.Instance.fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0, 0.7f, t / 4f));

            yield return null;
        }

        // PHASE 4: Final Moments (10-15s)
        Debug.Log("[RatSwarm] Final Moments...");
        
        t = 0f;
        while (t < 5f)
        {
            t += Time.deltaTime;
            
            // Screen goes completely black
            if (ScreenFader.Instance != null)
                ScreenFader.Instance.fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0.7f, 1f, t / 5f));

            // Blur increases
            if (BlurController.Instance != null)
                BlurController.Instance.SetBlur(Mathf.Lerp(0f, blurMax, t / 5f));

            yield return null;
        }

        // PHASE 5: Transition / Respawn
        Debug.Log("[RatSwarm] Restarting Level...");
        yield return new WaitForSeconds(2f); // Hold on black for 2 seconds
        
        // Reload the current scene (standard Unity way to respawn)
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    IEnumerator BiteFlash()
    {
        if (_redOverlay == null) yield break;
        _redOverlay.color = new Color(1, 0, 0, 0.4f);
        yield return new WaitForSeconds(0.1f);
        _redOverlay.color = new Color(1, 0, 0, 0);
    }

    IEnumerator FadeAudio(AudioSource source, float duration)
    {
        float startVol = source.volume;
        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            source.volume = Mathf.Lerp(startVol, 0, t / duration);
            yield return null;
        }
        source.Stop();
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerState ps = other.GetComponentInParent<PlayerState>();
        if (ps == null) ps = other.GetComponent<PlayerState>();
        if (ps != null)
        {
            _player = ps;
            _lingerTimer = 0f;
            Debug.Log("[RatSwarm] Player entered nesting chamber.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerState ps = other.GetComponentInParent<PlayerState>();
        if (ps == null) ps = other.GetComponent<PlayerState>();
        if (ps != null && ps == _player)
        {
            _player = null;
            _lingerTimer = 0f;
            Debug.Log("[RatSwarm] Player left nesting chamber.");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawCube(transform.position, new Vector3(16.5f, 4f, 17f));
    }
}