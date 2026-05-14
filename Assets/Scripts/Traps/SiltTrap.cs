using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SiltTrap : MonoBehaviour
{
    [Header("Silt Settings")]
    public float sinkDepth = 1.5f;
    public float sinkSpeed = 0.15f;

    [Header("Visual")]
    public GameObject mudPlane;
    public GameObject bubbleParticles;

    private PlayerState _player;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;
    }

    void OnTriggerEnter(Collider other)
    {
        _player = other.GetComponentInParent<PlayerState>();
        if (_player == null) _player = other.GetComponent<PlayerState>();

        if (_player != null)
        {
            _player.isInSilt = true;
            _player.isSinking = true;
            _player.sinkSpeed = sinkSpeed;
            Debug.Log("[SiltTrap] Player entered the mire...");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_player != null && (other.GetComponentInParent<PlayerState>() == _player || other.GetComponent<PlayerState>() == _player))
        {
            _player.isSinking = false;
            _player.isInSilt = false;
            Debug.Log("[SiltTrap] Player escaped the mire.");
            _player = null;
        }
    }
}