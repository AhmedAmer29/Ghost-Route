using UnityEngine;

// Attach this to the root of your desk clock model.
// The clock needs a Box Collider so the raycast can hit it.
public class AlarmInteraction : MonoBehaviour
{
    [Header("References")]
    public Main2GameManager gameManager;

    [Header("Optional")]
    [Tooltip("A UI label like 'Click to stop alarm' — leave empty for now")]
    public GameObject interactPrompt;

    private bool interactionEnabled = false;
    private bool alreadyStopped     = false;

    void Start()
    {
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    // Called by Main2GameManager.EnableAlarmInteraction()
    // (triggered either from a Timeline Signal or directly in code)
    public void EnableInteraction()
    {
        interactionEnabled = true;
        if (interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    void Update()
    {
        if (!interactionEnabled || alreadyStopped) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Max reach 3 units — increase if the clock is further from the camera
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    StopAlarm();
            }
        }
    }

    void StopAlarm()
    {
        alreadyStopped     = true;
        interactionEnabled = false;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        // Only the alarm audio stops — the clock tick keeps running.
        gameManager.OnAlarmStopped();
    }
}
