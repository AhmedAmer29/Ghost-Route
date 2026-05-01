using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class GlobalLightColorController : MonoBehaviour
{
    [SerializeField] Color lightColor = new Color(0.9056604f, 0.88485134f, 0f, 1f);
    [SerializeField] bool controlIntensity;
    [Min(0f)]
    [SerializeField] float intensity = 1f;
    [SerializeField] bool applyInEditMode = true;

    [SerializeField, HideInInspector] Color lastAppliedColor = new Color(0.9056604f, 0.88485134f, 0f, 1f);
    [SerializeField, HideInInspector] bool lastControlIntensity;
    [SerializeField, HideInInspector] float lastAppliedIntensity = 1f;

    [ContextMenu("Apply To All Scene Lights")]
    public void ApplyToAllSceneLights()
    {
        Scene scene = gameObject.scene;
        if (!scene.IsValid())
            return;

        foreach (Light sceneLight in FindSceneLights(scene))
        {
            sceneLight.color = lightColor;

            if (controlIntensity)
                sceneLight.intensity = intensity;

#if UNITY_EDITOR
            EditorUtility.SetDirty(sceneLight);
#endif
        }

        RememberAppliedValues();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            EditorSceneManager.MarkSceneDirty(scene);
#endif
    }

    void OnValidate()
    {
        if (!Application.isPlaying && !applyInEditMode)
            return;

        if (HasSettingsChanged())
            ApplyToAllSceneLights();
    }

    bool HasSettingsChanged()
    {
        return lightColor != lastAppliedColor
            || controlIntensity != lastControlIntensity
            || (controlIntensity && !Mathf.Approximately(intensity, lastAppliedIntensity));
    }

    void RememberAppliedValues()
    {
        lastAppliedColor = lightColor;
        lastControlIntensity = controlIntensity;
        lastAppliedIntensity = intensity;
    }

    static Light[] FindSceneLights(Scene scene)
    {
        Light[] allLights = FindObjectsOfType<Light>(true);
        int matchCount = 0;

        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i].gameObject.scene == scene)
                matchCount++;
        }

        Light[] sceneLights = new Light[matchCount];
        int sceneLightIndex = 0;

        for (int i = 0; i < allLights.Length; i++)
        {
            if (allLights[i].gameObject.scene == scene)
                sceneLights[sceneLightIndex++] = allLights[i];
        }

        return sceneLights;
    }
}
