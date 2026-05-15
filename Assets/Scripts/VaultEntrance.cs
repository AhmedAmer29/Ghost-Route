using UnityEngine;

// Place this on an empty GameObject INSIDE the vault with a Box Collider (trigger).
// Assign the VaultDoor (DoorPivot) in the Inspector.
public class VaultEntrance : MonoBehaviour
{
    public VaultDoor vaultDoor;

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (vaultDoor != null && !vaultDoor.IsOpen) return;

        VaultCinematic.Play();
    }
}
