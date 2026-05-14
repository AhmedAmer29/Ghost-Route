using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class CeilingCollapse : MonoBehaviour
{
    [Header("Trigger")]
    public bool triggerOnNoise = true;
    public bool triggerOnCombat = true;

    [Header("Debris")]
    public GameObject[] debrisPrefabs;
    public int debrisCount = 15;
    public float spawnRadius = 3f;
    public float debrisForce = 15f;
    public float debrisDamage = 25f;

    [Header("Timing")]
    public float rumbleDuration = 1.5f;
    public float collapseDelay = 0.5f;

    private bool _triggered;
    private PlayerState _player;

    void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (_triggered) return;

        _player = other.GetComponentInParent<PlayerState>();
        if (_player == null) _player = other.GetComponent<PlayerState>();
        if (_player == null || _player.isDead) return;

        bool shouldTrigger = false;

        if (triggerOnNoise && _player.IsLoud())
            shouldTrigger = true;

        if (triggerOnCombat && Input.GetMouseButtonDown(0))
            shouldTrigger = true;

        if (shouldTrigger)
        {
            _triggered = true;
            StartCoroutine(CollapseSequence());
        }
    }

    IEnumerator CollapseSequence()
    {
        Debug.Log("[CeilingCollapse] Rumbling...");

        float rumbleTimer = 0f;
        while (rumbleTimer < rumbleDuration)
        {
            rumbleTimer += Time.deltaTime;
            Camera cam = Camera.main;
            if (cam != null)
            {
                float intens = Mathf.Lerp(0.02f, 0.15f, rumbleTimer / rumbleDuration);
                cam.transform.localPosition += Random.insideUnitSphere * intens;
            }
            yield return null;
        }

        yield return new WaitForSeconds(collapseDelay);

        Debug.Log("[CeilingCollapse] CEILING COLLAPSES!");
        SpawnDebris();

        if (_player != null && !_player.isDead)
        {
            _player.TriggerDeath("Crushed by ceiling collapse");
        }
    }

    void SpawnDebris()
    {
        if (debrisPrefabs == null || debrisPrefabs.Length == 0)
        {
            for (int i = 0; i < debrisCount; i++)
                SpawnCubeDebris();

            return;
        }

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject prefab = debrisPrefabs[Random.Range(0, debrisPrefabs.Length)];
            if (prefab == null)
            {
                SpawnCubeDebris();
                continue;
            }

            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            pos.y = transform.position.y;
            GameObject debris = Instantiate(prefab, pos, Random.rotation);

            Rigidbody rb = debris.GetComponent<Rigidbody>();
            if (rb == null) rb = debris.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = Random.Range(5f, 30f);
            rb.AddForce(Vector3.down * debrisForce + Random.insideUnitSphere * debrisForce * 0.5f, ForceMode.Impulse);

            Destroy(debris, 5f);
        }
    }

    void SpawnCubeDebris()
    {
        Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
        pos.y = transform.position.y;
        GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debris.transform.position = pos;
        debris.transform.rotation = Random.rotation;
        debris.transform.localScale = Vector3.one * Random.Range(0.2f, 0.8f);
        debris.name = "Rubble";

        Renderer r = debris.GetComponent<Renderer>();
        if (r != null) r.material.color = new Color(0.3f, 0.25f, 0.2f);

        Rigidbody rb = debris.AddComponent<Rigidbody>();
        rb.mass = Random.Range(5f, 30f);
        rb.AddForce(Vector3.down * debrisForce + Random.insideUnitSphere * debrisForce * 0.5f, ForceMode.Impulse);

        Destroy(debris, 5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.3f, 0.1f, 0.4f);
        Gizmos.DrawCube(transform.position, new Vector3(spawnRadius * 2f, 0.5f, spawnRadius * 2f));
    }
}