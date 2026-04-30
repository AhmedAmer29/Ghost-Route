using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SecurityCamera))]
public class SecurityCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SecurityCamera cam = (SecurityCamera)target;

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Test Message", GUILayout.Height(30)))
        {
            cam.ShowMessage();
        }
    }
}