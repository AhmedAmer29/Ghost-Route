using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class FalseLadder : MonoBehaviour
{
    [Header("Ladder")]
    public Transform ladderModel;
    public Transform climbStart;
    public Transform breakPoint;
    public Transform fallDestination;

    [Header("Climb")]
    public float climbSpeed = 3f;
    public KeyCode climbKey = KeyCode.W;

    [Header("Break")]
    public float breakDelay = 0.3f;
    public float fallDuration = 1.5f;
    public float screenShakeIntensity = 0.3f;

    [Header("Light (exit illusion)")]
    public Light exitLight;
    public float lightIntensityMax = 2f;

    private bool _playerNear;
    private bool _climbing;
    private bool _broken;
    private PlayerState _player;
    private CharacterController _playerCC;
    private Vector3 _playerOrigPos;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
        if (exitLight != null) exitLight.intensity = lightIntensityMax;
    }

    void Update()
    {
        if (!_playerNear || _broken) return;

        if (Input.GetKeyDown(climbKey) && !_climbing)
            StartCoroutine(ClimbSequence());
    }

    IEnumerator ClimbSequence()
    {
        _climbing = true;

        if (_player != null) _player.isClimbing = true;
        _playerOrigPos = _playerCC.transform.position;
        Vector3 breakPos = breakPoint != null ? breakPoint.position : climbStart.position + Vector3.up * 4f;

        while (_playerCC.transform.position.y < breakPos.y)
        {
            if (Input.GetKeyUp(climbKey)) { StopClimbing(); yield break; }

            Vector3 mv = _playerCC.transform.position;
            mv.y += climbSpeed * Time.deltaTime;
            _playerCC.enabled = false;
            _playerCC.transform.position = mv;
            _playerCC.enabled = true;

            yield return null;
        }

        yield return new WaitForSeconds(breakDelay);

        BreakLadder();
    }

    void StopClimbing()
    {
        _climbing = false;
        if (_player != null) _player.isClimbing = false;
    }

    void BreakLadder()
    {
        _broken = true;
        _climbing = false;
        if (_player != null) _player.isClimbing = false;
        if (exitLight != null) exitLight.intensity = 0f;

        Debug.Log("[FalseLadder] LADDER BREAKS! Player falls...");

        if (ladderModel != null)
        {
            Rigidbody rb = ladderModel.GetComponent<Rigidbody>();
            if (rb == null) rb = ladderModel.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(Vector3.down * 2f + Random.onUnitSphere, ForceMode.Impulse);
        }

        StartCoroutine(FallSequence());
    }

    IEnumerator FallSequence()
    {
        float elapsed = 0f;
        Vector3 startFall = _playerCC.transform.position;
        Vector3 endFall = fallDestination != null ? fallDestination.position : startFall - Vector3.up * 10f;

        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fallDuration;

            _playerCC.enabled = false;
            _playerCC.transform.position = Vector3.Lerp(startFall, endFall, t * t);
            _playerCC.enabled = true;

            Camera cam = Camera.main;
            if (cam != null)
                cam.transform.localPosition += Random.insideUnitSphere * screenShakeIntensity * (1f - t);

            yield return null;
        }

        _playerCC.enabled = false;
        _playerCC.transform.position = endFall;
        _playerCC.enabled = true;

        if (_player != null)
            _player.TriggerDeath("Fell from broken ladder");
    }

    void OnTriggerEnter(Collider other)
    {
        _player = other.GetComponentInParent<PlayerState>();
        if (_player == null) _player = other.GetComponent<PlayerState>();

        _playerCC = other.GetComponent<CharacterController>();
        if (_playerCC == null) _playerCC = other.GetComponentInParent<CharacterController>();

        if (_player != null && _playerCC != null)
            _playerNear = true;
    }

    void OnTriggerExit(Collider other)
    {
        _playerNear = false;
        _climbing = false;
        if (_player != null) _player.isClimbing = false;
        _player = null;
        _playerCC = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (climbStart != null)
            Gizmos.DrawWireCube(climbStart.position, Vector3.one * 0.3f);
        if (breakPoint != null)
            Gizmos.DrawWireSphere(breakPoint.position, 0.3f);
        Gizmos.color = Color.red;
        if (fallDestination != null)
            Gizmos.DrawWireSphere(fallDestination.position, 0.5f);
    }
}