using UnityEngine;

public class CubeInspectProcedural : MonoBehaviour
{
    [SerializeField] float tiltSpeed = 0.7f;
    [SerializeField] float tiltAmount = 8f;
    float timeOffset;

    void Start() => timeOffset = Random.Range(0f, 10f);

    void LateUpdate()
    {
        float t = Time.time + timeOffset;
        // Small breathing-style micro rotation layered on animation
        Quaternion micro = Quaternion.Euler(
            Mathf.Sin(t * tiltSpeed) * tiltAmount * 0.4f,
            0f,
            Mathf.Sin(t * tiltSpeed * 0.6f) * tiltAmount
        );
        transform.localRotation *= micro;
    }
}