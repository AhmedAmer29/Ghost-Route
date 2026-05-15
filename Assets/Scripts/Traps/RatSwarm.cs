using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.AI;

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

    [Header("Rat Attack Movement")]
    public float ratAttackSpeed = 6f;
    public float ratGroundSnapDistance = 6f;
    public float ratGroundOffset = 0.02f;
    public float ratSurroundRadius = 1.25f;
    public float ratSurroundJitter = 0.45f;
    public LayerMask ratGroundMask = ~0;

    private bool _sequenceStarted;
    private float _lingerTimer;
    private PlayerState _player;
    private ParticleSystem _swarmParticles;
    private ParticleSystem _eyesParticles;
    private Collider _zone;
    private Image _redOverlay;

    private List<Transform> _meshRats = new List<Transform>();
    private Dictionary<Transform, Vector3> _attackOffsets = new Dictionary<Transform, Vector3>();
    private Vector3 _originalCamPos;

    void Start()
    {
        _zone = GetComponent<Collider>();
        _zone.isTrigger = true;

        SetupParticles();
        SetupUIOverlay();
        FindMeshRats();

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
        emission.rateOverTime = 0;

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

        _lingerTimer += Time.deltaTime;
        bool tooLoud = _player.IsLoud();

        if (tooLoud || _lingerTimer >= lingerThreshold)
        {
            StartCoroutine(DeathSequence());
        }

        if (backgroundChittering != null)
        {
            backgroundChittering.volume = Mathf.Lerp(0.1f, 0.4f, _lingerTimer / lingerThreshold);
        }
    }

    void FindMeshRats()
    {
        _meshRats.Clear();
        foreach (var transform in GameObject.FindObjectsOfType<Transform>())
        {
            if (IsTopLevelNamedRat(transform, "blackrat"))
            {
                _meshRats.Add(transform);
            }
        }
        Debug.Log($"[RatSwarm] Found {_meshRats.Count} top-level black rats to use in the kill sequence.");
    }

    bool IsTopLevelNamedRat(Transform current, string ratBaseName)
    {
        string objectName = current.name.Trim();
        if (!objectName.Equals(ratBaseName, System.StringComparison.OrdinalIgnoreCase) &&
            !objectName.StartsWith(ratBaseName + " (", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Transform parent = current.parent;
        if (parent == null)
        {
            return true;
        }

        string parentName = parent.name.Trim();
        return !parentName.Equals(ratBaseName, System.StringComparison.OrdinalIgnoreCase) &&
               !parentName.StartsWith(ratBaseName + " (", System.StringComparison.OrdinalIgnoreCase);
    }

    IEnumerator DeathSequence()
    {
        _sequenceStarted = true;
        _originalCamPos = Camera.main != null ? Camera.main.transform.localPosition : Vector3.zero;

        // PHASE 1: Initial Disturbance (0-3s)
        Debug.Log("[RatSwarm] Disturbance detected...");
        if (backgroundChittering != null) backgroundChittering.volume = 0.8f;
        
        float t = 0f;
        Animator targetAnim = null;
        while (t < 3f)
        {
            t += Time.deltaTime;
            if (Camera.main != null)
                Camera.main.transform.localPosition = _originalCamPos + Random.insideUnitSphere * 0.02f;
            yield return null;
        }

        // PHASE 2: The Swarm Begins (3-7s)
        Debug.Log("[RatSwarm] SWARM START!");
        if (swarmScream != null) swarmScream.Play();
        PlayBlackRatAttackAnimations();
        BuildRatSurroundOffsets();
        SnapAllAttackRatsToGround();
        
        // Cache player for this sequence so it doesn't fail if they "exit" the trigger
        PlayerState targetPlayer = _player;
        if (targetPlayer == null) targetPlayer = GameObject.FindObjectOfType<PlayerState>();

        // Make mesh rats start running toward player
        t = 0f;
        while (t < 4f)
        {
            t += Time.deltaTime;
            if (targetPlayer == null) break; 
            
            foreach(var rat in _meshRats)
            {
                if (rat == null) continue;

                Vector3 targetPosition = GetRatAttackTargetPosition(rat, targetPlayer.transform.position);
                
                // Safety 1: Lock rotation to Y axis only (no flying rats)
                Vector3 targetDir = targetPosition - rat.position;
                targetDir.y = 0; 
                if (targetDir != Vector3.zero) rat.rotation = Quaternion.LookRotation(targetDir);
                
                // Safety 2: Keep them on the floor height
                Vector3 nextPos = Vector3.MoveTowards(rat.position, targetPosition, Time.deltaTime * ratAttackSpeed);
                nextPos = GetGroundedRatPosition(rat, nextPos);
                rat.position = nextPos;
            }

            if (Camera.main != null)
                Camera.main.transform.localPosition = _originalCamPos + Random.insideUnitSphere * 0.08f;
            
            yield return null;
        }

        // PHASE 3: The Attack & Health Drain
        Debug.Log($"[RatSwarm] PHASE 3 START. Monitoring {_meshRats.Count} rats for touch.");
        
        bool touched = false;
        float killTimer = 0f;
        while (!touched && killTimer < 10f)
        {
            killTimer += Time.deltaTime;
            foreach(var rat in _meshRats)
            {
                if (rat == null) continue;
                if (Vector3.Distance(rat.position, targetPlayer.transform.position) < 1.5f)
                {
                    touched = true;
                    break;
                }
            }
            yield return null;
        }

        // NEW: Turn the screen red before the final cinematic death
        if (RatDamageEffect.Instance == null)
        {
            GameObject go = new GameObject("RatDamageEffect_Auto");
            go.AddComponent<RatDamageEffect>();
        }

        // Drain health to 0 over 1.5 seconds to get the red screen effect
        float drainTimer = 0f;
        while (drainTimer < 1.5f)
        {
            drainTimer += Time.deltaTime;
            if (RatDamageEffect.Instance != null)
            {
                // Deal massive damage to force the screen to go full red instantly
                RatDamageEffect.Instance.TakeDamage(100f * Time.deltaTime);
            }
            
            // Camera shake while being bitten
            if (Camera.main != null)
            {
                Camera.main.transform.localPosition = _originalCamPos + Random.insideUnitSphere * 0.1f;
            }
            yield return null;
        }

        Debug.Log("[RatSwarm] Triggering Death Sequence Logic.");
        if (playerStruggling != null) playerStruggling.Play();

        if (targetPlayer != null)
        {
            targetPlayer.TriggerDeath("Overwhelmed by Rat Swarm");
            
            // NEW: Disable all rat colliders so you can fall THROUGH them
            foreach(var rat in _meshRats)
            {
                if (rat == null) continue;
                var col = rat.GetComponent<Collider>();
                if (col == null) col = rat.GetComponentInChildren<Collider>();
                if (col != null) col.enabled = false;
            }

            // 1. DISABLE EVERYTHING on the player so nothing fights the animation
            foreach(var comp in targetPlayer.transform.root.GetComponentsInChildren<MonoBehaviour>())
            {
                if (comp != this && comp.GetType().Name != "PlayerState") 
                    comp.enabled = false;
            }

            var cc = targetPlayer.transform.root.GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = false;

            var rb = targetPlayer.GetComponent<Rigidbody>();
            if (rb != null) { rb.isKinematic = true; rb.velocity = Vector3.zero; }

            // 2. Find the CORRECT animator
            Animator[] allAnimators = targetPlayer.GetComponentsInChildren<Animator>();
            targetAnim = null;
            
            foreach(var a in allAnimators)
            {
                for (int i = 0; i < a.parameterCount; i++)
                {
                    if (a.GetParameter(i).name == "RatDeath")
                    {
                        targetAnim = a;
                        break;
                    }
                }
                if (targetAnim != null) break;
            }

            if (targetAnim != null) 
            {
                Debug.Log($"[RatSwarm] FOUND REAL ANIMATOR on {targetAnim.gameObject.name}. Forcing play.");
                targetAnim.enabled = true; 
                
                // Wait a tiny bit for the rig to initialize
                yield return new WaitForSeconds(0.1f);

                // 3. PARENT CAMERA TO HEAD so the view falls with the body
                if (Camera.main != null)
                {
                    Transform head = null;
                    
                    // Try the safe way first
                    if (targetAnim.avatar != null && targetAnim.isHuman)
                        head = targetAnim.GetBoneTransform(HumanBodyBones.Head);
                    
                    // Manual search if Avatar is missing or not humanoid
                    if (head == null)
                    {
                        Transform[] allChildren = targetPlayer.GetComponentsInChildren<Transform>();
                        foreach(var child in allChildren)
                        {
                            if (child.name.ToLower().Contains("head"))
                            {
                                head = child;
                                break;
                            }
                        }
                    }

                    if (head == null) head = targetAnim.transform; // Last resort
                    
                    Debug.Log($"[RatSwarm] Camera parented to: {head.name}");
                    Camera.main.transform.SetParent(head);
                    Camera.main.transform.localPosition = Vector3.zero;
                    Camera.main.transform.localRotation = Quaternion.identity;
                }

                // Play on ALL layers (layer 0, 1, 2...) so it overrides combat/walking
                targetAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                for(int i = 0; i < targetAnim.layerCount; i++)
                {
                    targetAnim.Play("RatDeath_State", i, 0f); 
                }
            }
            else
            {
                Debug.LogError("[RatSwarm] CRITICAL: Could not find ANY animator with a 'RatDeath' parameter! Did you run the Setup tool?");
            }
        }

        // Disable any SewerEnemyAI nearby so they don't hit the corpse
        foreach (var ai in GameObject.FindObjectsOfType<SewerEnemyAI>())
        {
            ai.enabled = false;
        }

        // Disable CameraController specifically so it doesn't fight the animation
        if (Camera.main != null)
        {
            var camCtrl = Camera.main.GetComponent("CameraController");
            if (camCtrl != null) (camCtrl as MonoBehaviour).enabled = false;
        }

        // We no longer unparent the camera! 
        // We let the animation pull the camera down naturally.
        
        // --- NUCLEAR LOCKDOWN ---
        // Disable EVERY script on the camera so NOTHING can fight us
        if (Camera.main != null)
        {
            foreach(var comp in Camera.main.GetComponents<MonoBehaviour>())
            {
                if (comp != this) comp.enabled = false; 
            }
        }

        // --- THE ANIMATION IS WORKING - LET IT DRIVE THE CAMERA ---
        float elapsed = 0f;
        Vector3 initialLocalPos = Camera.main != null ? Camera.main.transform.localPosition : Vector3.zero;

        while (elapsed < 4f) 
        {
            elapsed += Time.deltaTime;
            
            // NO MORE SHAKE (Prevent Earthquake)
            // But we add a tiny manual sink just in case the head bone is slow
            if (Camera.main != null && elapsed < 1.5f)
            {
                float progress = elapsed / 1.5f;
                float smooth = progress * progress;
                Camera.main.transform.localPosition = Vector3.Lerp(initialLocalPos, new Vector3(0, -0.4f, 0.2f), smooth);
            }

            // Red bite flashes
            if (Random.value > 0.95f) StartCoroutine(BiteFlash());

            // Darken screen
            if (ScreenFader.Instance != null)
                ScreenFader.Instance.fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0, 0.8f, elapsed / 3f));

            yield return null;
        }

        // PHASE 4: The Void (8-12s)
        Debug.Log("[RatSwarm] Blackout...");
        
        t = 0f;
        while (t < 4f)
        {
            t += Time.deltaTime;
            if (ScreenFader.Instance != null)
                ScreenFader.Instance.fadeImage.color = new Color(0, 0, 0, Mathf.Lerp(0.8f, 1f, t / 4f));

            if (BlurController.Instance != null)
                BlurController.Instance.SetBlur(Mathf.Lerp(0f, blurMax, t / 4f));

            yield return null;
        }

        // PHASE 5: Reset
        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void PlayBlackRatAttackAnimations()
    {
        foreach (var rat in _meshRats)
        {
            if (rat == null) continue;

            BlackRatAnimationPlayer player = rat.GetComponent<BlackRatAnimationPlayer>();
            if (player != null)
            {
                player.PlayAttackAnimation();
                continue;
            }

            Animator animator = rat.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Play(0, 0, 0f);
            }
        }
    }

    void BuildRatSurroundOffsets()
    {
        _attackOffsets.Clear();
        int count = Mathf.Max(1, _meshRats.Count);

        for (int i = 0; i < _meshRats.Count; i++)
        {
            Transform rat = _meshRats[i];
            if (rat == null) continue;

            float angle = (Mathf.PI * 2f * i) / count;
            float radius = ratSurroundRadius + Random.Range(-ratSurroundJitter, ratSurroundJitter);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            _attackOffsets[rat] = offset;
        }
    }

    Vector3 GetRatAttackTargetPosition(Transform rat, Vector3 playerPosition)
    {
        Vector3 offset;
        if (!_attackOffsets.TryGetValue(rat, out offset))
        {
            offset = Random.insideUnitSphere * ratSurroundRadius;
            offset.y = 0f;
            _attackOffsets[rat] = offset;
        }

        return playerPosition + offset;
    }

    void SnapAllAttackRatsToGround()
    {
        foreach (var rat in _meshRats)
        {
            if (rat == null) continue;
            rat.position = GetGroundedRatPosition(rat, rat.position);
        }
    }

    Vector3 GetGroundedRatPosition(Transform rat, Vector3 desiredPosition)
    {
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(desiredPosition, out navHit, ratGroundSnapDistance, NavMesh.AllAreas))
        {
            return navHit.position + Vector3.up * ratGroundOffset;
        }

        Vector3 origin = desiredPosition + Vector3.up * ratGroundSnapDistance;
        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            Vector3.down,
            ratGroundSnapDistance * 2f,
            ratGroundMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        Vector3 bestPoint = desiredPosition;
        bool foundGround = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == rat || hit.transform.IsChildOf(rat))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestPoint = hit.point;
                foundGround = true;
            }
        }

        if (foundGround)
        {
            return bestPoint + Vector3.up * ratGroundOffset;
        }

        desiredPosition.y = rat.position.y;
        return desiredPosition;
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
        // DEBUG: Tell us exactly what touched the trigger
        Debug.Log($"[RatSwarm] SOMETHING touched the trigger: {other.gameObject.name} (Tag: {other.gameObject.tag})");

        PlayerState ps = other.GetComponentInParent<PlayerState>();
        if (ps == null) ps = other.GetComponent<PlayerState>();
        
        if (ps != null)
        {
            _player = ps;
            _lingerTimer = 0f;
            Debug.Log("[RatSwarm] PLAYER CONFIRMED. Starting noise/linger monitoring.");
        }
        else
        {
            Debug.LogWarning($"[RatSwarm] Object {other.gameObject.name} touched us, but it doesn't have a PlayerState script!");
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
