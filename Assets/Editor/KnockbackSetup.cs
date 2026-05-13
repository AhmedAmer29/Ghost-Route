using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class KnockbackSetup
{
    static KnockbackSetup()
    {
        EditorApplication.delayCall += RunSetup;
    }

    private static void RunSetup()
    {
        if (SessionState.GetBool("KnockbackSetupDone_V5", false))
            return;

        SessionState.SetBool("KnockbackSetupDone_V5", true);

        Debug.Log("[Knockback Setup] Running setup script...");

        // Find the player in the active scene. Look for CharacterController first, then MainCamera.
        GameObject player = null;
        CharacterController cc = Object.FindAnyObjectByType<CharacterController>();
        
        if (cc != null)
        {
            player = cc.gameObject;
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null) player = mainCam.gameObject;
        }

        if (player != null)
        {
            // Check if it already has the receiver
            if (player.GetComponent<PlayerKnockbackReceiver>() == null)
            {
                PlayerKnockbackReceiver receiver = player.AddComponent<PlayerKnockbackReceiver>();
                
                // Try to find the player movement script to disable during stun.
                // Since we don't know the exact class, we can look for common ones like FirstPersonController
                MonoBehaviour[] allBehaviours = player.GetComponents<MonoBehaviour>();
                foreach (var mb in allBehaviours)
                {
                    if (mb.GetType().Name.Contains("Controller") && mb.GetType().Name != "CharacterController")
                    {
                        receiver.componentsToDisable = new MonoBehaviour[] { mb };
                        break;
                    }
                }

                Debug.Log($"[Knockback Setup] Added PlayerKnockbackReceiver to {player.name}!");
                EditorSceneManager.MarkSceneDirty(player.scene);
            }
        }
        else
        {
            Debug.LogWarning("[Knockback Setup] Could not find a CharacterController or MainCamera in the scene. Please attach PlayerKnockbackReceiver manually.");
        }
    }
}