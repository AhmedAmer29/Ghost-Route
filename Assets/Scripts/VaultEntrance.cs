using UnityEngine;

public class VaultEntrance : MonoBehaviour
{
    public VaultDoor  vaultDoor;
    public AudioClip  voiceOver;

    Rigidbody _rb;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // CharacterController needs a kinematic Rigidbody to fire OnTriggerEnter
        if (GetComponent<Rigidbody>() == null)
        {
            _rb            = gameObject.AddComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }
    }

    void Update()
    {
        // OverlapBox fallback — reliable even when OnTriggerEnter misses
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Bounds b = col.bounds;
        Collider[] hits = Physics.OverlapBox(b.center, b.extents, transform.rotation,
                                             ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                if (vaultDoor != null && !vaultDoor.IsOpen) return;
                VaultCinematic.Play(voiceOver);
                return;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (vaultDoor != null && !vaultDoor.IsOpen) return;
        VaultCinematic.Play(voiceOver);
    }
}
