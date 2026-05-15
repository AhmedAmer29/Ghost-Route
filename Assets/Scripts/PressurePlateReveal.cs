using UnityEngine;

public class PressurePlateReveal : MonoBehaviour
{
    [Tooltip("Set true on plates the player must step on. Leave false for decoys.")]
    public bool isCorrect = false;

    Renderer[] _renderers;
    bool       _visible;

    void Start()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);

        // Make sure there's a trigger collider so OnTriggerEnter fires
        Collider col = GetComponentInChildren<Collider>();
        if (col != null) col.isTrigger = true;

        Hide();
    }

    public void ShowWhite() => SetState(true,  new Color(1f, 1f,  1f,   1f));
    public void ShowGreen() => SetState(true,  new Color(0f, 3f,  0.5f, 1f));

    public void Hide()
    {
        _visible = false;
        if (_renderers == null) return;
        foreach (Renderer r in _renderers)
            r.enabled = false;
    }

    void SetState(bool visible, Color emission)
    {
        _visible = visible;
        if (_renderers == null) return;
        foreach (Renderer r in _renderers)
        {
            r.enabled = visible;
            foreach (Material m in r.materials)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isCorrect) return; // correct plate — no penalty

        // Wrong plate — respawn and reset the reveal button
        UVRevealController.ResetOnDeath();

        if (UVRevealController.Instance != null)
            UVRevealController.Instance.RespawnPlayer();
    }
}
