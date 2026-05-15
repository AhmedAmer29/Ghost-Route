using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach to a fade-overlay GameObject that survives a scene load
// (DontDestroyOnLoad). Once the next scene finishes loading, fades the black
// image back to clear and destroys the overlay — prevents a stuck black screen.
public class SceneFadeReveal : MonoBehaviour
{
    Image image;
    float duration;
    bool done;

    public void Init(Image img, float fadeDuration)
    {
        image = img;
        duration = Mathf.Max(0.01f, fadeDuration);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (done) return;
        done = true;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(FadeBack());
    }

    IEnumerator FadeBack()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            if (image != null)
            {
                Color c = image.color;
                c.a = Mathf.Lerp(1f, 0f, t / duration);
                image.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
