using UnityEngine;

public class RopeCrossingTrigger : MonoBehaviour
{
    public RopeCrossingController controller;
    public Transform playerAttachPoint; // Where the player should snap to start

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCrossing(other.gameObject);
        }
    }

    private void StartCrossing(GameObject player)
    {
        if (controller == null) return;

        // Disable standard controls
        var controls = player.GetComponent("PlayerMovement");
        if (controls == null) controls = player.GetComponent("PlayerControls");

        if (controls != null)
        {
            var en = controls.GetType().GetProperty("enabled");
            if (en != null) en.SetValue(controls, false, null);
        }

        // Lock CharacterController
        var cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Snap to start position
        if (playerAttachPoint != null)
        {
            player.transform.position = playerAttachPoint.position;
            player.transform.rotation = playerAttachPoint.rotation;
        }

        // Start logic — route through PlayerRopeMovement which bridges the controller
        PlayerRopeMovement ropeMovement = player.GetComponent<PlayerRopeMovement>();
        if (ropeMovement != null)
            ropeMovement.BeginCrossing();
        else
            controller.StartCrossing(); // Fallback if PlayerRopeMovement not present
        
        // Disable trigger so it doesn't fire again
        gameObject.SetActive(false);
    }

    // Optional: Reset trigger on respawn
    public void ResetTrigger()
    {
        gameObject.SetActive(true);
    }
}
