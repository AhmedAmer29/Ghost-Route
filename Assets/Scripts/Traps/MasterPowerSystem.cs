using UnityEngine;
using UnityEngine.Events;

public class MasterPowerSystem : MonoBehaviour
{
    [Header("Puzzle Settings")]
    public int requiredFixes = 3;
    public UnityEvent onAllFixed; // Use this to enable the Lever Switch

    private int _currentFixes = 0;
    public int fixedCount  => _currentFixes;
    public int targetCount => requiredFixes;

    public void RegisterFix()
    {
        _currentFixes++;
        Debug.Log($"<color=cyan>[MasterPower] Progress: {_currentFixes}/{requiredFixes}</color>");

        if (_currentFixes >= requiredFixes)
        {
            CompleteSystem();
        }
    }

    void CompleteSystem()
    {
        Debug.Log("<color=green>[MasterPower] ALL SYSTEMS ONLINE! Lever Unlocked.</color>");
        onAllFixed?.Invoke();

        // Direct push to BossHealth so the gate removes immediately when both
        // boss + circuits are done — rather than relying on its polling coroutine
        // (which can lose us if FindFirstObjectByType returns null during load).
        foreach (var boss in Object.FindObjectsByType<BossHealth>(FindObjectsSortMode.None))
            boss.NotifyCircuitsComplete();
    }
}
