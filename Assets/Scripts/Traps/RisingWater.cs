using UnityEngine;

public class RisingWater : MonoBehaviour
{
    [Header("Water Rise")]
    public float riseSpeed = 0.033f;
    public float maxHeight = 20f;
    public float startHeight;

    [Header("Drowning")]
    public float drownDuration = 8f;
    public Color waterColor = new Color(0.08f, 0.05f, 0.02f, 0.85f);

    private float _currentRise;
    private bool _rising;
    private Material _waterMat;
    private float _currentLevel;
    private Camera _camera;
    private PlayerState _playerState;

    void Start()
    {
        startHeight = transform.position.y;
        _currentLevel = startHeight;
        _camera = Camera.main;

        if (_camera != null)
        {
            _playerState = _camera.GetComponentInParent<PlayerState>();
            if (_playerState == null)
                _playerState = _camera.GetComponent<PlayerState>();
            Debug.Log($"[RisingWater] Camera: {(_camera != null ? _camera.name : "null")}, PlayerState: {(_playerState != null ? "found" : "null")}");
        }
        else
        {
            Debug.LogWarning("[RisingWater] Camera.main is null!");
        }

        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            _waterMat = r.material;
            _waterMat.color = waterColor;
        }

        _rising = true;
        _currentRise = 0f;

        if (r != null)
        {
            Debug.Log($"[RisingWater] Water renderer bounds: minY={r.bounds.min.y:F2}, maxY={r.bounds.max.y:F2}, centerY={r.bounds.center.y:F2}, surfaceOffset={r.bounds.max.y - transform.position.y:F2}");
        }
        else
        {
            Debug.Log("[RisingWater] No renderer found on water GameObject - will use transform.position.y as fallback.");
        }

        Debug.Log($"[RisingWater] Water starts rising. Speed: {riseSpeed:F4}m/s, Max rise: {maxHeight}m, StartY: {startHeight}");

        if (GetComponent<WaterSurfaceEffects>() == null)
            gameObject.AddComponent<WaterSurfaceEffects>();
    }

    void Update()
    {
        if (_rising)
        {
            _currentRise += riseSpeed * Time.deltaTime;
            if (_currentRise >= maxHeight)
            {
                _currentRise = maxHeight;
                _rising = false;
                Debug.Log($"[RisingWater] Water reached max height: {_currentLevel:F2}");
            }

            _currentLevel = startHeight + _currentRise;
            Vector3 pos = transform.position;
            pos.y = _currentLevel;
            transform.position = pos;
        }

        UpdateSubmersion();
    }

    void UpdateSubmersion()
    {
        if (_playerState == null || !_playerState.isInWater)
            return;

        float cameraY = (_camera != null) ? _camera.transform.position.y : float.MaxValue;

        Renderer rend = GetComponent<Renderer>();
        float waterSurfaceY = rend != null ? rend.bounds.max.y : _currentLevel;

        bool wasSubmerged = _playerState.isSubmerged;
        bool submerged = cameraY < waterSurfaceY;

        if (wasSubmerged != submerged)
        {
            Debug.Log($"[RisingWater] Submerged CHANGED: {wasSubmerged} -> {submerged} (camY={cameraY:F2}, waterY={_currentLevel:F2}, waterSurfaceY={waterSurfaceY:F2}, centerToSurface={waterSurfaceY - _currentLevel:F2})");
        }

        _playerState.isSubmerged = submerged;
    }

    PlayerState FindPlayerState(Collider other)
    {
        PlayerState ps = other.GetComponentInParent<PlayerState>();
        if (ps == null) ps = other.GetComponent<PlayerState>();
        return ps;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerState ps = FindPlayerState(other);
        if (ps == null) return;

        ps.isInWater = true;
        Debug.Log($"[RisingWater] Player entered water. PlayerState.isInWater set to true.");

        if (_playerState == null)
        {
            _playerState = ps;
            Debug.Log("[RisingWater] Cached PlayerState reference from OnTriggerEnter.");
        }
    }

    void OnTriggerStay(Collider other)
    {
        PlayerState ps = FindPlayerState(other);
        if (ps == null) return;

        if (_playerState == null)
        {
            _playerState = ps;
            Debug.Log("[RisingWater] Cached PlayerState reference from OnTriggerStay.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        PlayerState ps = FindPlayerState(other);
        if (ps == null) return;

        Debug.Log($"[RisingWater] Player EXITED water. Clearing isInWater & isSubmerged (was isSubmerged={ps.isSubmerged})");
        ps.isInWater = false;
        ps.isSubmerged = false;
    }
}