using UnityEngine;
using System.Linq;

public class CollisionDebugger : MonoBehaviour
{
    public float radius = 8f;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.P)) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);

        var sorted = hits
            .Where(h => h.gameObject != gameObject)
            .OrderBy(h => Vector3.Distance(transform.position, h.bounds.ClosestPoint(transform.position)))
            .ToArray();

        for (int i = 0; i < sorted.Length; i++)
        {
            var h = sorted[i];
            float dist = Vector3.Distance(transform.position, h.bounds.ClosestPoint(transform.position));
            Debug.Log($"[{i + 1}] {h.gameObject.name} | {h.GetType().Name} | dist: {dist:F2}m | Path: {GetPath(h.transform)}", h.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
