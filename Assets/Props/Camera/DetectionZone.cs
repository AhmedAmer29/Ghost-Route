using UnityEngine;

public class DetectionZone : MonoBehaviour
{
    public Transform spawnPoint;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = spawnPoint.position;
            Debug.Log("Player caught! Respawning...");
        }
    }
}