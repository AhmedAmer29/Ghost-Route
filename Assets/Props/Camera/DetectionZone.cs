using UnityEngine;

public class DetectionZone : MonoBehaviour
{
    public Transform spawnPoint;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Disable CharacterController before teleporting to avoid physics snap issues
            var cc = other.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            other.transform.position = spawnPoint.position;
            if (cc != null) cc.enabled = true;
            Debug.Log("Player caught! Respawning...");
        }
    }
}