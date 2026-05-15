using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RatSceneCounter
{
    [MenuItem("Tools/Sewer Tools/COUNT RATS IN OPEN SCENES")]
    public static void CountRatsInOpenScenes()
    {
        int blackRatCount = 0;
        int swarmRatCount = 0;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                CountRatsRecursive(rootObject.transform, ref blackRatCount, ref swarmRatCount);
            }
        }

        int total = blackRatCount + swarmRatCount;

        Debug.Log("[RatCounter] Rat count for currently open scenes");
        Debug.Log($"[RatCounter] blackrat: {blackRatCount}");
        Debug.Log($"[RatCounter] RAT: {swarmRatCount}");
        Debug.Log($"[RatCounter] Total: {total}");
        Debug.Log($"[RatCounter] Active scene: {EditorSceneManager.GetActiveScene().path}");
    }

    private static void CountRatsRecursive(Transform current, ref int blackRatCount, ref int swarmRatCount)
    {
        string objectName = current.name.Trim();

        if (IsTopLevelRat(current, "blackrat"))
        {
            blackRatCount++;
        }
        else if (IsTopLevelRat(current, "RAT"))
        {
            swarmRatCount++;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            CountRatsRecursive(current.GetChild(i), ref blackRatCount, ref swarmRatCount);
        }
    }

    private static bool IsTopLevelRat(Transform current, string ratBaseName)
    {
        string objectName = current.name.Trim();
        if (!objectName.Equals(ratBaseName, System.StringComparison.OrdinalIgnoreCase) &&
            !objectName.StartsWith(ratBaseName + " (", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        Transform parent = current.parent;
        if (parent == null)
        {
            return true;
        }

        string parentName = parent.name.Trim();
        return !parentName.Equals(ratBaseName, System.StringComparison.OrdinalIgnoreCase) &&
               !parentName.StartsWith(ratBaseName + " (", System.StringComparison.OrdinalIgnoreCase);
    }
}
