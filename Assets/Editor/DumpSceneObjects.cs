using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DumpSceneObjects
{
    [MenuItem("Tools/Dump Scene Objects")]
    static void Dump()
    {
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            DumpRecursive(go, "");
        }
    }
    
    static void DumpRecursive(GameObject go, string indent)
    {
        Debug.Log(indent + go.name + " at " + go.transform.position);
        for(int i=0; i<go.transform.childCount; i++) {
            DumpRecursive(go.transform.GetChild(i).gameObject, indent + "  ");
        }
    }
}
