using UnityEngine;
using System.Collections;

public class WaterSurfaceEffects : MonoBehaviour
{
    [Header("Toggle")]
    public bool effectsEnabled = false;

    [Header("Crown Mesh")]
    public int crownSegments = 24;
    public float crownHeightBase = 0.6f;
    public float crownRadiusBase = 0.3f;
    public float crownRiseDuration = 0.035f;
    public float crownFallDuration = 0.045f;

    [Header("Finger Droplets")]
    public int fingerBaseCount = 12;
    public float fingerSpeed = 4f;
    public float fingerSize = 0.12f;

    [Header("Satellite Droplets")]
    public int primaryCount = 20;
    public float primarySizeMin = 0.015f;
    public float primarySizeMax = 0.03f;
    public int microCount = 40;
    public float microSizeMin = 0.015f;
    public float microSizeMax = 0.03f;

    [Header("Mist Layer")]
    public float mistSpeedMin = 0.5f;
    public float mistSpeedMax = 2f;
    public float mistLifetime = 1.2f;

    [Header("Foam Layer")]
    public int foamCount = 8;
    public float foamLifetime = 3f;
    public float foamSizeMin = 0.2f;
    public float foamSizeMax = 0.5f;

    [Header("Bubble Layer")]
    public int bubbleCount = 20;
    public float bubbleRiseSpeed = 0.3f;
    public float bubbleSizeMin = 0.03f;
    public float bubbleSizeMax = 0.07f;
    public float bubbleLifetime = 2f;

    [Header("Worthington Jet")]
    public float jetVelocityThreshold = 3f;
    public int jetParticleCount = 12;
    public float jetSpeed = 10f;
    public float jetDelay = 0.07f;
    public float jetLifetime = 0.4f;
    public float jetConeAngle = 5f;

    [Header("Spray Mist")]
    public int sprayMistCount = 50;
    public float sprayMistLifetime = 1f;
    public float sprayMistSpeedMin = 1f;
    public float sprayMistSpeedMax = 3f;

    [Header("Footstep Splashes")]
    public float footstepInterval = 0.4f;
    public int footstepDroplets = 8;
    public int footstepMist = 10;
    public float footstepSpeedThreshold = 0.5f;

    [Header("Surface Displacement")]
    public ComputeShader waveSimShader;
    public int dispTexSize = 128;
    public float dispWaveSpeed = 2f;
    public float dispDamping = 0.993f;
    public float dispHeightScale = 0.08f;
    public float dispUpdateInterval = 0.033f;

    private PlayerState _playerState;
    private Transform _playerTransform;
    private CharacterController _cc;
    private bool _wasFeetInWater;
    private float _footstepTimer;
    private float _waterSurfaceY;
    private Color _splashColor;
    private float _impactVelocity;
    private bool _splashActive;
    private Renderer _waterRenderer;

    private GameObject _particleRoot;
    private Material _sharedParticleMat;

    private ParticleSystem _dropletPS;
    private ParticleSystem _mistPS;
    private ParticleSystem _foamPS;
    private ParticleSystem _bubblePS;
    private ParticleSystem _jetPS;
    private ParticleSystem _sprayMistPS;

    private GameObject _crownRoot;
    private Mesh _crownMesh;
    private MeshFilter _crownMf;
    private MeshRenderer _crownMr;
    private Material _crownMat;
    private MaterialPropertyBlock _crownMpb;

    private ComputeShader _waveSim;
    private RenderTexture _dispHeight;
    private RenderTexture _dispVelocity;
    private bool _displacementReady;
    private float _dispTimer;
    private int _kernelWaveSim;
    private int _kernelInject;

    struct SplashParams
    {
        public Vector3 position;
        public float impactVelocity;
        public float objectRadius;
    }

    void Start()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            _playerState = cam.GetComponentInParent<PlayerState>();
            if (_playerState != null)
                _playerTransform = _playerState.transform;
        }
        if (_playerState == null)
        {
            _playerState = FindObjectOfType<PlayerState>();
            if (_playerState != null)
                _playerTransform = _playerState.transform;
        }
        if (_playerTransform != null)
            _cc = _playerTransform.GetComponent<CharacterController>();

        Renderer waterRend = GetComponent<Renderer>();
        if (waterRend != null)
        {
            _waterRenderer = waterRend;
            Material m = waterRend.material;
            Color scum = m.GetColor("_ScumColor");
            Color shallow = m.GetColor("_ShallowColor");
            _splashColor = Color.Lerp(scum, shallow, 0.5f);
            _splashColor.a = 1f;
        }
        else
        {
            _splashColor = new Color(0.55f, 0.5f, 0.4f, 1f);
        }

        _splashColor = Color.green;

        Texture2D splatTex = GenerateSplatTexture();
        _sharedParticleMat = CreateParticleMaterial(splatTex);

        _particleRoot = new GameObject("SplashParticles");
        _particleRoot.transform.SetParent(null);

        if (!effectsEnabled) return;

        CreateDropletSystem();
        CreateMistSystem();
        CreateFoamSystem();
        CreateBubbleSystem();
        CreateJetSystem();
        CreateSprayMistSystem();
        CreateCrownSystem();
        InitDisplacement();

        UpdateWaterSurfaceY();

        Debug.Log($"[WaterFX] Started. PlayerState: {(_playerState != null ? "found" : "NULL")}, CC: {(_cc != null ? "found" : "NULL")}, WaterY: {_waterSurfaceY:F2}");
        Debug.Log($"[WaterFX] ParticleShader: {(_sharedParticleMat != null ? _sharedParticleMat.shader.name : "NULL")}, supported: {(_sharedParticleMat != null ? _sharedParticleMat.shader.isSupported : false)}");
        Debug.Log($"[WaterFX] CrownShader: {(_crownMat != null ? _crownMat.shader.name : "NULL")}, supported: {(_crownMat != null ? _crownMat.shader.isSupported : false)}");
    }

    void Update()
    {
        if (!effectsEnabled) return;
        if (_playerState == null || _playerTransform == null) return;

        UpdateWaterSurfaceY();

        float footY = GetFootY();
        bool feetInWater = footY < _waterSurfaceY;
        bool submerged = _playerState.isSubmerged;
        float speed = _playerState.currentSpeed;
        _impactVelocity = Mathf.Abs(_playerState.verticalVelocity);

        if (feetInWater && !_wasFeetInWater && !_splashActive)
        {
            float entryForce = Mathf.Max(_impactVelocity, speed * 0.5f);
            Debug.Log($"[WaterFX] Entry splash. footY={footY:F2} waterY={_waterSurfaceY:F2} vel={_impactVelocity:F2} force={entryForce:F2}");
            SplashParams p = new SplashParams
            {
                position = GetSpawnPosition(),
                impactVelocity = Mathf.Max(entryForce, 1f),
                objectRadius = _cc != null ? _cc.radius : 0.3f
            };
            StartCoroutine(SplashSequence(p));
        }
        else if (!feetInWater && _wasFeetInWater)
        {
            SpawnExitSplash();
        }

        _wasFeetInWater = feetInWater;

        if (feetInWater && !submerged)
        {
            _footstepTimer += Time.deltaTime;
            if (speed > footstepSpeedThreshold && _footstepTimer >= footstepInterval)
            {
                _footstepTimer = 0f;
                SpawnFootstepSplash();
            }
        }

        if (_displacementReady)
        {
            _dispTimer += Time.deltaTime;
            if (_dispTimer >= dispUpdateInterval)
            {
                _dispTimer = 0f;
                UpdateDisplacement();
            }
        }
    }

    // =================================================================
    //  SPLASH TIMELINE
    // =================================================================
    IEnumerator SplashSequence(SplashParams p)
    {
        _splashActive = true;
        Debug.Log($"[WaterFX] SplashSequence starting. vel={p.impactVelocity:F2} radius={p.objectRadius:F2} pos={p.position}");

        float crownRadius = p.objectRadius * 0.4f + p.impactVelocity * 0.06f;
        crownRadius = Mathf.Clamp(crownRadius, 0.15f, 1.5f);

        SpawnCrown(p, crownRadius);
        SpawnBubbles(p);
        SpawnFoam(p);
        SpawnSprayMist(p);
        InjectDisplacement(p.position, p.impactVelocity);

        yield return new WaitForSeconds(0.01f);

        SpawnFingerDroplets(p.position, p.impactVelocity, crownRadius);

        yield return new WaitForSeconds(0.05f);
        Debug.Log($"[WaterFX] Particle counts - droplets: {_dropletPS.particleCount}, mist: {_mistPS.particleCount}, foam: {_foamPS.particleCount}, bubbles: {_bubblePS.particleCount}");

        var rend = _dropletPS.GetComponent<ParticleSystemRenderer>();
        Debug.Log($"[WaterFX] DropletR: enabled={rend.enabled}, mat={(rend.sharedMaterial != null ? rend.sharedMaterial.shader.name : "NULL")}");
        ParticleSystem.Particle[] ps = new ParticleSystem.Particle[10];
        int pc = _dropletPS.GetParticles(ps);
        if (pc > 0)
            Debug.Log($"[WaterFX] First particle: pos={ps[0].position} size={ps[0].startSize} color={ps[0].startColor}");

        GameObject testPS = new GameObject("_TEST_PS");
        testPS.transform.position = p.position;
        ParticleSystem tp = testPS.AddComponent<ParticleSystem>();
        var tm = tp.main;
        tm.startColor = Color.magenta;
        tm.startSize = 0.5f;
        tm.startLifetime = 3f;
        tm.maxParticles = 100;
        tp.Emit(10);
        Debug.Log($"[WaterFX] Test PS at {p.position}, emitted 10, count={tp.particleCount}");

        if (p.impactVelocity >= jetVelocityThreshold)
        {
            yield return new WaitForSeconds(jetDelay - 0.01f);
            SpawnWorthingtonJet(p);
        }

        _splashActive = false;
    }

    // =================================================================
    //  CROWN MESH
    // =================================================================
    void CreateCrownSystem()
    {
        if (_particleRoot == null) _particleRoot = new GameObject("SplashParticles");

        _crownRoot = new GameObject("SplashCrown");
        _crownRoot.transform.SetParent(_particleRoot.transform, false);
        _crownRoot.SetActive(false);

        _crownMf = _crownRoot.AddComponent<MeshFilter>();
        _crownMr = _crownRoot.AddComponent<MeshRenderer>();

        Shader s = FindShader("Particles/Standard Unlit");
        _crownMat = new Material(s);
        
        if (s.name.Contains("Particles") || s.name.Contains("Unlit") || s.name.Contains("Standard"))
        {
            _crownMat.SetColor("_TintColor", _splashColor);
            _crownMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _crownMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _crownMat.SetInt("_ZWrite", 0);
            _crownMat.renderQueue = 3000;
            _crownMat.EnableKeyword("_ALPHABLEND_ON");
        }

        _crownMpb = new MaterialPropertyBlock();
        _crownMr.sharedMaterial = _crownMat;
    }

    void SpawnCrown(SplashParams p, float crownRadius)
    {
        if (_crownRoot == null) CreateCrownSystem();
        if (_crownRoot == null) return;

        float crownHeight = crownHeightBase * Mathf.Sqrt(p.impactVelocity) * Mathf.Sqrt(p.objectRadius * 2f);
        crownHeight = Mathf.Clamp(crownHeight, 0.1f, 2f);

        _crownRoot.transform.position = p.position;
        _crownRoot.SetActive(true);

        if (_crownMesh != null)
        {
            Destroy(_crownMesh);
            _crownMesh = null;
        }

        _crownMesh = GenerateCrownMesh(
            crownSegments,
            crownRadius * 0.85f,
            crownRadius * 1.15f,
            crownHeight
        );
        _crownMf.sharedMesh = _crownMesh;

        StartCoroutine(AnimateCrown());
    }

    Mesh GenerateCrownMesh(int segments, float bottomRadius, float topRadius, float height)
    {
        Mesh mesh = new Mesh();
        int vertCount = segments * 2;
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Vector3[] normals = new Vector3[vertCount];
        int[] tris = new int[segments * 6];

        for (int i = 0; i < segments; i++)
        {
            float a = (i / (float)segments) * Mathf.PI * 2f;
            float c = Mathf.Cos(a);
            float s = Mathf.Sin(a);

            verts[i] = new Vector3(c * bottomRadius, 0, s * bottomRadius);
            verts[i + segments] = new Vector3(c * topRadius, height, s * topRadius);
            uvs[i] = new Vector2(i / (float)segments, 0);
            uvs[i + segments] = new Vector2(i / (float)segments, 1);

            Vector3 outDir = new Vector3(c, 0, s);
            normals[i] = (outDir + Vector3.up * 0.3f).normalized;
            normals[i + segments] = (outDir + Vector3.up * 0.5f).normalized;

            int ni = (i + 1) % segments;
            int ti = i * 6;
            tris[ti] = i;
            tris[ti + 1] = i + segments;
            tris[ti + 2] = ni;
            tris[ti + 3] = ni;
            tris[ti + 4] = i + segments;
            tris[ti + 5] = ni + segments;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.normals = normals;
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        return mesh;
    }

    IEnumerator AnimateCrown()
    {
        float elapsed = 0f;

        while (elapsed < crownRiseDuration)
        {
            float t = elapsed / crownRiseDuration;
            float s = t * t * (3f - 2f * t);
            _crownRoot.transform.localScale = new Vector3(s, s, s);
            SetCrownAlpha(s * 0.8f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _crownRoot.transform.localScale = Vector3.one;
        SetCrownAlpha(0.8f);

        elapsed = 0f;
        while (elapsed < crownFallDuration)
        {
            float t = elapsed / crownFallDuration;
            float alpha = 0.8f * (1f - t);
            float expand = 1f + t * 0.3f;
            float shrink = 1f - t * 0.8f;
            _crownRoot.transform.localScale = new Vector3(expand, shrink, expand);
            SetCrownAlpha(alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _crownRoot.SetActive(false);
    }

    void SetCrownAlpha(float a)
    {
        if (_crownMpb == null || _crownMr == null) return;
        Color c = _splashColor;
        c.a = Mathf.Clamp01(a);
        _crownMpb.SetColor("_TintColor", c);
        _crownMr.SetPropertyBlock(_crownMpb);
    }

    // =================================================================
    //  FINGER + SATELLITE DROPLETS
    // =================================================================
    void SpawnFingerDroplets(Vector3 center, float vel, float crownRadius)
    {
        int fingers = Mathf.Clamp(Mathf.RoundToInt(fingerBaseCount + vel * 1.5f), 6, 30);
        var p = new ParticleSystem.EmitParams();
        _dropletPS.transform.position = center;

        Debug.Log($"[WaterFX] Droplets at {center}, fingers={fingers}, crownR={crownRadius:F2}");

        for (int i = 0; i < fingers; i++)
        {
            float angle = (i / (float)fingers) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            p.position = dir * crownRadius;
            p.velocity = dir * (fingerSpeed * 0.5f + vel * 0.4f) + Vector3.up * (2f + vel * 0.3f);
            p.startSize = fingerSize * Random.Range(0.7f, 1.3f);
            p.startLifetime = Random.Range(0.4f, 0.7f);
            _dropletPS.Emit(p, 1);
        }

        for (int i = 0; i < fingers * 2; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float rad = crownRadius * Random.Range(0.6f, 1.1f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0.1f, Mathf.Sin(angle));
            p.position = dir * rad;
            p.velocity = dir * (1f + vel * 0.2f) + Vector3.up * Random.Range(0.5f, 2.5f);
            p.startSize = Random.Range(primarySizeMin, primarySizeMax);
            p.startLifetime = Random.Range(0.4f, 0.8f);
            _dropletPS.Emit(p, 1);
        }

        for (int i = 0; i < microCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float rad = crownRadius * Random.Range(0.4f, 1.3f);
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            p.position = dir * rad;
            p.velocity = dir * Random.Range(0.5f, 2f) + Vector3.up * Random.Range(0.3f, 2.5f)
                + Random.insideUnitSphere * 0.5f;
            p.startSize = Random.Range(microSizeMin, microSizeMax);
            p.startLifetime = Random.Range(0.3f, 0.6f);
            _dropletPS.Emit(p, 1);
        }
    }

    // =================================================================
    //  WORTHINGTON JET
    // =================================================================
    void SpawnWorthingtonJet(SplashParams p)
    {
        _jetPS.transform.position = p.position;
        var ep = new ParticleSystem.EmitParams();

        for (int i = 0; i < jetParticleCount; i++)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float spread = Mathf.Tan(jetConeAngle * Mathf.Deg2Rad) * 0.3f * Random.Range(0f, 1f);
            Vector3 off = new Vector3(Mathf.Cos(angle) * spread, 0, Mathf.Sin(angle) * spread);
            ep.position = off;
            ep.velocity = Vector3.up * (jetSpeed * (0.6f + p.impactVelocity * 0.08f)) + off * Random.Range(1f, 3f);
            ep.startSize = Random.Range(0.01f, 0.03f);
            ep.startLifetime = jetLifetime * Random.Range(0.7f, 1.3f);
            _jetPS.Emit(ep, 1);
        }

        float jetPeak = (jetSpeed * 0.15f + p.impactVelocity * 0.05f) * 0.5f;
        for (int i = 0; i < 4; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 d = new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a));
            ep.position = d * 0.1f + Vector3.up * jetPeak;
            ep.velocity = d * Random.Range(1f, 3f) + Vector3.up * Random.Range(-1f, 1f);
            ep.startSize = Random.Range(0.02f, 0.04f);
            ep.startLifetime = Random.Range(0.5f, 0.8f);
            _jetPS.Emit(ep, 1);
        }
    }

    // =================================================================
    //  SUBSURFACE BUBBLES
    // =================================================================
    void SpawnBubbles(SplashParams p)
    {
        _bubblePS.transform.position = new Vector3(
            p.position.x, _waterSurfaceY - 0.3f, p.position.z
        );
        var ep = new ParticleSystem.EmitParams();
        int count = Mathf.RoundToInt(bubbleCount * Mathf.Clamp01(p.impactVelocity / 3f));

        for (int i = 0; i < count; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(0f, p.objectRadius * 0.8f + 0.2f);
            Vector3 localPos = new Vector3(Mathf.Cos(a) * r, Random.Range(0f, 0.5f), Mathf.Sin(a) * r);

            ep.position = localPos;
            ep.velocity = Vector3.up * bubbleRiseSpeed * Random.Range(0.7f, 1.3f)
                + new Vector3(Mathf.Sin(localPos.y * 10f) * 0.05f, 0, Mathf.Cos(localPos.y * 10f) * 0.05f);
            ep.startSize = Random.Range(bubbleSizeMin, bubbleSizeMax);
            ep.startLifetime = bubbleLifetime * Random.Range(0.7f, 1.3f);
            _bubblePS.Emit(ep, 1);
        }
    }

    // =================================================================
    //  FOAM
    // =================================================================
    void SpawnFoam(SplashParams p)
    {
        _foamPS.transform.position = p.position;
        var ep = new ParticleSystem.EmitParams();

        for (int i = 0; i < foamCount; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float r = Random.Range(0f, p.objectRadius * 1.5f + p.impactVelocity * 0.05f);
            Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            ep.position = d * r;
            ep.velocity = d * Random.Range(0.05f, 0.2f);
            ep.startSize = Random.Range(foamSizeMin, foamSizeMax);
            ep.startLifetime = foamLifetime * Random.Range(0.7f, 1.3f);
            _foamPS.Emit(ep, 1);
        }
    }

    // =================================================================
    //  SPRAY MIST
    // =================================================================
    void SpawnSprayMist(SplashParams p)
    {
        _sprayMistPS.transform.position = p.position;
        var ep = new ParticleSystem.EmitParams();

        for (int i = 0; i < sprayMistCount; i++)
        {
            ep.position = Random.insideUnitSphere * 0.3f;
            ep.velocity = Random.onUnitSphere * Random.Range(sprayMistSpeedMin, sprayMistSpeedMax)
                + Vector3.up * Random.Range(1f, 3f);
            ep.startSize = Random.Range(0.01f, 0.025f);
            ep.startLifetime = sprayMistLifetime * Random.Range(0.5f, 1.5f);
            _sprayMistPS.Emit(ep, 1);
        }
    }

    // =================================================================
    //  FOOTSTEP / EXIT SPLASHES
    // =================================================================
    void SpawnFootstepSplash()
    {
        Vector3 pos = GetSpawnPosition();
        _dropletPS.transform.position = pos;
        _mistPS.transform.position = pos;

        float r = (_cc != null ? _cc.radius : 0.3f) * 0.5f;
        var ep = new ParticleSystem.EmitParams();

        for (int i = 0; i < footstepDroplets; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 d = new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a));
            ep.position = d * r;
            ep.velocity = d * Random.Range(0.5f, 1.5f) + Vector3.up * Random.Range(0.5f, 2f);
            ep.startSize = Random.Range(0.02f, 0.06f);
            ep.startLifetime = Random.Range(0.3f, 0.6f);
            _dropletPS.Emit(ep, 1);
        }

        for (int i = 0; i < footstepMist; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float rad = Random.Range(0f, r);
            Vector3 d = new Vector3(Mathf.Cos(a), 0.3f, Mathf.Sin(a));
            ep.position = d * rad;
            ep.velocity = d * Random.Range(0.2f, 0.8f) + Vector3.up * Random.Range(0.2f, 0.5f);
            ep.startSize = Random.Range(0.02f, 0.05f);
            ep.startLifetime = Random.Range(0.3f, 0.6f);
            _mistPS.Emit(ep, 1);
        }
    }

    void SpawnExitSplash()
    {
        Vector3 pos = GetSpawnPosition();
        _dropletPS.transform.position = pos;
        _mistPS.transform.position = pos;
        _foamPS.transform.position = pos;

        float r = (_cc != null ? _cc.radius : 0.3f) * 0.5f;
        var ep = new ParticleSystem.EmitParams();

        for (int i = 0; i < footstepDroplets; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            Vector3 d = new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a));
            ep.position = d * r;
            ep.velocity = d * Random.Range(0.3f, 1f) + Vector3.up * Random.Range(0.2f, 1f);
            ep.startSize = Random.Range(0.02f, 0.05f);
            ep.startLifetime = Random.Range(0.3f, 0.6f);
            _dropletPS.Emit(ep, 1);
        }

        for (int i = 0; i < footstepMist; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float rad = Random.Range(0f, r);
            Vector3 d = new Vector3(Mathf.Cos(a), 0.1f, Mathf.Sin(a));
            ep.position = d * rad;
            ep.velocity = d * Random.Range(0.2f, 0.5f) + Vector3.up * Random.Range(0.1f, 0.3f);
            ep.startSize = Random.Range(0.02f, 0.04f);
            ep.startLifetime = Random.Range(0.3f, 0.5f);
            _mistPS.Emit(ep, 1);
        }

        for (int i = 0; i < foamCount / 2; i++)
        {
            float a = Random.Range(0f, Mathf.PI * 2f);
            float rad = Random.Range(0f, r);
            Vector3 d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            ep.position = d * rad;
            ep.velocity = d * Random.Range(0.1f, 0.3f);
            ep.startSize = Random.Range(foamSizeMin, foamSizeMax);
            ep.startLifetime = Random.Range(1f, 2f);
            _foamPS.Emit(ep, 1);
        }
    }

    // =================================================================
    //  SURFACE DISPLACEMENT
    // =================================================================
    void InitDisplacement()
    {
        if (waveSimShader == null)
        {
            _displacementReady = false;
            return;
        }

        _waveSim = waveSimShader;
        _kernelWaveSim = _waveSim.FindKernel("WaveSim");
        _kernelInject = _waveSim.FindKernel("InjectSplash");

        _dispHeight = new RenderTexture(dispTexSize, dispTexSize, 0, RenderTextureFormat.RFloat);
        _dispHeight.enableRandomWrite = true;
        _dispHeight.Create();

        _dispVelocity = new RenderTexture(dispTexSize, dispTexSize, 0, RenderTextureFormat.RFloat);
        _dispVelocity.enableRandomWrite = true;
        _dispVelocity.Create();

        _displacementReady = true;
        _dispTimer = 0f;

        if (_waterRenderer != null)
        {
            Material mat = _waterRenderer.material;
            mat.SetTexture("_DisplacementTex", _dispHeight);
            Bounds b = _waterRenderer.bounds;
            mat.SetVector("_DisplacementArea", new Vector4(
                b.min.x, b.min.z,
                b.max.x - b.min.x, b.max.z - b.min.z
            ));
            mat.SetFloat("_DisplacementHeight", dispHeightScale);
        }
    }

    void InjectDisplacement(Vector3 worldPos, float force)
    {
        if (!_displacementReady || _waterRenderer == null) return;

        Bounds b = _waterRenderer.bounds;
        float sx = b.max.x - b.min.x;
        float sz = b.max.z - b.min.z;
        if (sx < 0.01f || sz < 0.01f) return;

        float u = Mathf.Clamp01((worldPos.x - b.min.x) / sx);
        float v = Mathf.Clamp01((worldPos.z - b.min.z) / sz);

        var center = new Vector2Int(
            Mathf.RoundToInt(u * (dispTexSize - 1)),
            Mathf.RoundToInt(v * (dispTexSize - 1))
        );

        _waveSim.SetTexture(_kernelInject, "_Target", _dispHeight);
        _waveSim.SetInts("_InjCenter", new int[] { center.x, center.y });
        _waveSim.SetFloat("_InjForce", force * 0.05f);
        _waveSim.SetFloat("_InjRadius", 5f + force * 2f);

        int groups = Mathf.CeilToInt(dispTexSize / 8f);
        _waveSim.Dispatch(_kernelInject, groups, groups, 1);
    }

    void UpdateDisplacement()
    {
        _waveSim.SetTexture(_kernelWaveSim, "_Height", _dispHeight);
        _waveSim.SetTexture(_kernelWaveSim, "_Velocity", _dispVelocity);
        _waveSim.SetInt("_Width", dispTexSize);
        _waveSim.SetInt("_HeightVal", dispTexSize);
        _waveSim.SetFloat("_WaveSpeedSq", dispWaveSpeed * dispWaveSpeed * 0.1f);
        _waveSim.SetFloat("_Damping", dispDamping);
        _waveSim.SetFloat("_DeltaTime", dispUpdateInterval);

        int groups = Mathf.CeilToInt(dispTexSize / 8f);
        _waveSim.Dispatch(_kernelWaveSim, groups, groups, 1);
    }

    // =================================================================
    //  PARTICLE SYSTEM CREATION
    // =================================================================
    ParticleSystem CreatePS(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(_particleRoot.transform, false);
        return go.AddComponent<ParticleSystem>();
    }

    void SetupPSMain(ParticleSystem ps, float spdMin, float spdMax,
        float sizeMin, float sizeMax, float lifeMin, float lifeMax,
        Color color, float gravity, int maxP)
    {
        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(spdMin, spdMax);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = gravity;
        main.playOnAwake = false;
        main.maxParticles = maxP;
        var e = ps.emission;
        e.enabled = false;
    }

    void SetupShape(ParticleSystem ps, ParticleSystemShapeType type, float radius, float angle)
    {
        var shape = ps.shape;
        shape.shapeType = type;
        shape.radius = radius;
        shape.angle = angle;
    }

    void SetupSizeOverLife(ParticleSystem ps, float k0v, float k1v, float k1t, float k2v)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve c = new AnimationCurve();
        c.AddKey(0f, k0v);
        c.AddKey(k1t, k1v);
        c.AddKey(1f, k2v);
        sol.size = new ParticleSystem.MinMaxCurve(1f, c);
    }

    void SetupColorFade(ParticleSystem ps, float a0, float a1t, float a1v)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(a0, 0f),
                new GradientAlphaKey(a1v, a1t),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    void SetupRenderer(ParticleSystem ps, Material mat, ParticleSystemRenderMode mode, float stretch)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.material = mat;
        r.renderMode = mode;
        if (mode == ParticleSystemRenderMode.Stretch)
            r.velocityScale = stretch;
    }

    void CreateDropletSystem()
    {
        _dropletPS = CreatePS("SplashDroplets");
        SetupPSMain(_dropletPS, 2f, 5f, fingerSize, fingerSize, 0.3f, 0.8f, _splashColor, 2.5f, 200);
        SetupShape(_dropletPS, ParticleSystemShapeType.Cone, 0.01f, 0f);
        SetupSizeOverLife(_dropletPS, 0.4f, 1f, 0.2f, 0.05f);
        SetupColorFade(_dropletPS, 1f, 0.5f, 0f);
        SetupRenderer(_dropletPS, _sharedParticleMat, ParticleSystemRenderMode.Stretch, 0.8f);

        var rot = _dropletPS.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

        var noise = _dropletPS.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        noise.frequency = 1.5f;
        noise.scrollSpeed = 1f;
    }

    void CreateMistSystem()
    {
        _mistPS = CreatePS("SplashMist");
        Color mc = new Color(_splashColor.r, _splashColor.g, _splashColor.b, 0.35f);
        SetupPSMain(_mistPS, mistSpeedMin, mistSpeedMax, 0.04f, 0.12f, 0.5f, mistLifetime, mc, 0.6f, 100);
        SetupShape(_mistPS, ParticleSystemShapeType.Cone, 0.01f, 0f);
        SetupSizeOverLife(_mistPS, 0.3f, 1f, 0.4f, 0f);
        SetupColorFade(_mistPS, 0.6f, 0.5f, 0f);
        SetupRenderer(_mistPS, _sharedParticleMat, ParticleSystemRenderMode.Billboard, 0f);
    }

    void CreateFoamSystem()
    {
        _foamPS = CreatePS("SplashFoam");
        Color fc = Color.Lerp(_splashColor, Color.grey, 0.3f);
        fc.a = 0.7f;
        SetupPSMain(_foamPS, 0.1f, 0.3f, foamSizeMin, foamSizeMax, 1.5f, foamLifetime, fc, 0.1f, 50);
        SetupShape(_foamPS, ParticleSystemShapeType.Cone, 0.01f, 0f);
        SetupSizeOverLife(_foamPS, 0.2f, 1f, 0.3f, 0.7f);
        SetupColorFade(_foamPS, 0.6f, 0.3f, 0.4f);
        SetupRenderer(_foamPS, _sharedParticleMat, ParticleSystemRenderMode.Billboard, 0f);
    }

    void CreateBubbleSystem()
    {
        _bubblePS = CreatePS("SplashBubbles");
        Color bc = new Color(0.7f, 0.8f, 1f, 0.4f);
        SetupPSMain(_bubblePS, bubbleRiseSpeed * 0.7f, bubbleRiseSpeed * 1.3f,
            bubbleSizeMin, bubbleSizeMax, 1f, bubbleLifetime, bc, -0.2f, 80);
        SetupShape(_bubblePS, ParticleSystemShapeType.Cone, 0.01f, 0f);

        var sol = _bubblePS.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve bcCurve = new AnimationCurve();
        bcCurve.AddKey(0f, 0.3f);
        bcCurve.AddKey(0.3f, 1f);
        bcCurve.AddKey(0.8f, 0.7f);
        bcCurve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, bcCurve);

        SetupColorFade(_bubblePS, 0.5f, 0.4f, 0f);
        SetupRenderer(_bubblePS, _sharedParticleMat, ParticleSystemRenderMode.Billboard, 0f);

        var noise = _bubblePS.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        noise.frequency = 2f;
        noise.scrollSpeed = 0.5f;
    }

    void CreateJetSystem()
    {
        _jetPS = CreatePS("SplashJet");
        SetupPSMain(_jetPS, jetSpeed * 0.5f, jetSpeed * 1.3f, 0.01f, 0.03f, 0.3f, jetLifetime, _splashColor, 2f, 50);
        SetupShape(_jetPS, ParticleSystemShapeType.Cone, 0.01f, jetConeAngle);
        SetupSizeOverLife(_jetPS, 0.3f, 1f, 0.2f, 0.1f);
        SetupColorFade(_jetPS, 1f, 0.4f, 0f);
        SetupRenderer(_jetPS, _sharedParticleMat, ParticleSystemRenderMode.Stretch, 0.5f);
    }

    void CreateSprayMistSystem()
    {
        _sprayMistPS = CreatePS("SplashSprayMist");
        Color smc = new Color(_splashColor.r, _splashColor.g, _splashColor.b, 0.2f);
        SetupPSMain(_sprayMistPS, sprayMistSpeedMin, sprayMistSpeedMax,
            0.01f, 0.03f, 0.5f, sprayMistLifetime, smc, 0.3f, 100);
        SetupShape(_sprayMistPS, ParticleSystemShapeType.Cone, 0.01f, 0f);
        SetupColorFade(_sprayMistPS, 0.3f, 0.3f, 0f);
        SetupRenderer(_sprayMistPS, _sharedParticleMat, ParticleSystemRenderMode.Billboard, 0f);
    }

    // =================================================================
    //  TEXTURE / MATERIAL HELPERS
    // =================================================================
    Texture2D GenerateSplatTexture()
    {
        Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return tex;
    }

    Material CreateParticleMaterial(Texture2D tex)
    {
        Material mat = new Material(FindShader("Particles/Alpha Blended"));
        mat.mainTexture = tex;
        return mat;
    }

    Shader FindShader(string shaderName)
    {
        Shader s = Shader.Find(shaderName);
        if (s != null) return s;

        // Fallbacks for Built-in legacy shaders which are often moved in newer Unity versions
        if (shaderName.Contains("Alpha Blended"))
        {
            s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (s != null) return s;
            s = Shader.Find("Mobile/Particles/Alpha Blended");
            if (s != null) return s;
        }
        
        if (shaderName.Contains("Standard Unlit"))
        {
            s = Shader.Find("Legacy Shaders/Particles/Additive");
            if (s != null) return s;
            s = Shader.Find("Unlit/Texture");
            if (s != null) return s;
        }

        // Ultimate fallback
        s = Shader.Find("Sprites/Default");
        if (s != null) return s;
        
        return Shader.Find("Standard");
    }


    // =================================================================
    //  UTILITY
    // =================================================================
    float GetFootY()
    {
        if (_cc != null)
            return _playerTransform.position.y + _cc.center.y - _cc.height * 0.5f;
        return _playerTransform.position.y;
    }

    void UpdateWaterSurfaceY()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
            _waterSurfaceY = rend.bounds.max.y;
        else
            _waterSurfaceY = transform.position.y;
    }

    Vector3 GetSpawnPosition()
    {
        return new Vector3(
            _playerTransform.position.x,
            _waterSurfaceY + 0.02f,
            _playerTransform.position.z
        );
    }

    void OnDestroy()
    {
        if (_dispHeight != null) { _dispHeight.Release(); _dispHeight = null; }
        if (_dispVelocity != null) { _dispVelocity.Release(); _dispVelocity = null; }
    }
}
