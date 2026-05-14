using UnityEngine;

[RequireComponent(typeof(Camera))]
public class UnderwaterPostProcess : MonoBehaviour
{
    public Material material;
    private PlayerState _state;
    private float _transition = 0f;
    private bool _depthModeSet = false;
    private bool _previousSubmerged = false;

    void OnEnable()
    {
        EnsureMaterial();
        EnsureDepthTextureMode();
    }

    void EnsureMaterial()
    {
        if (material == null)
            material = new Material(Shader.Find("Hidden/UnderwaterPostProcess"));
    }

    void EnsureDepthTextureMode()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null && (cam.depthTextureMode & DepthTextureMode.Depth) == 0)
            cam.depthTextureMode |= DepthTextureMode.Depth;
        _depthModeSet = true;
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_depthModeSet)
            EnsureDepthTextureMode();

        if (_state == null)
            _state = GetComponentInParent<PlayerState>();

        bool isSubmerged = (_state != null) && _state.isSubmerged;

        if (isSubmerged != _previousSubmerged)
            Debug.Log($"[UnderwaterPostProcess] isSubmerged changed: {_previousSubmerged} -> {isSubmerged}");
        _previousSubmerged = isSubmerged;

        _transition = Mathf.MoveTowards(_transition, isSubmerged ? 1f : 0f, Time.deltaTime * 3f);

        if (_transition > 0.01f)
        {
            if (material != null)
            {
                material.SetFloat("_EffectAlpha", _transition);
                Graphics.Blit(source, destination, material);
            }
            else
            {
                Graphics.Blit(source, destination);
            }
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
