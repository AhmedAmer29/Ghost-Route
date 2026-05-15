using UnityEngine;

[RequireComponent(typeof(Animator))]
public class BlackRatAnimationPlayer : MonoBehaviour
{
    public string[] idleStateNames = new string[0];
    public string attackStateName;
    public float minStateDuration = 3f;
    public float maxStateDuration = 7f;

    private Animator _animator;
    private float _nextSwitchTime;
    private bool _attackMode;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void Start()
    {
        PlayNextIdle();
    }

    void Update()
    {
        if (_attackMode)
        {
            return;
        }

        if (idleStateNames == null || idleStateNames.Length == 0)
        {
            return;
        }

        if (Time.time >= _nextSwitchTime)
        {
            PlayNextIdle();
        }
    }

    public void Configure(string[] stateNames, string attackState)
    {
        idleStateNames = stateNames ?? new string[0];
        attackStateName = attackState;
    }

    public void PlayAttackAnimation()
    {
        if (_animator == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(attackStateName))
        {
            attackStateName = FindAttackStateFromController();
        }

        if (string.IsNullOrEmpty(attackStateName))
        {
            Debug.LogWarning($"[BlackRatAnimationPlayer] {name} has no attack/run state assigned.");
            return;
        }

        _attackMode = true;
        _animator.CrossFadeInFixedTime(attackStateName, 0.08f);
        Debug.Log($"[BlackRatAnimationPlayer] {name} playing attack/run state: {attackStateName}");
    }

    private string FindAttackStateFromController()
    {
        RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
        if (controller == null)
        {
            return string.Empty;
        }

        AnimationClip[] clips = controller.animationClips;
        string[] keywords = { "run", "walk", "attack", "jump", "move", "scene" };

        foreach (string keyword in keywords)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip == null || !clip.name.ToLowerInvariant().Contains(keyword))
                {
                    continue;
                }

                string stateName = FindExistingStateName(clip, i);
                if (!string.IsNullOrEmpty(stateName))
                {
                    return stateName;
                }
            }
        }

        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            string stateName = FindExistingStateName(clip, i);
            if (!string.IsNullOrEmpty(stateName))
            {
                return stateName;
            }
        }

        return string.Empty;
    }

    private string FindExistingStateName(AnimationClip clip, int index)
    {
        string sanitized = SanitizeStateName(clip.name);
        string[] candidates =
        {
            $"Rat_{index}_{sanitized}",
            sanitized,
            clip.name
        };

        foreach (string candidate in candidates)
        {
            if (_animator.HasState(0, Animator.StringToHash(candidate)))
            {
                return candidate;
            }
        }

        return string.Empty;
    }

    private string SanitizeStateName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Clip";
        }

        char[] chars = rawName.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]))
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }

    private void PlayNextIdle()
    {
        if (_animator == null || idleStateNames == null || idleStateNames.Length == 0)
        {
            return;
        }

        string stateName = idleStateNames[Random.Range(0, idleStateNames.Length)];
        _animator.CrossFadeInFixedTime(stateName, 0.15f);
        _nextSwitchTime = Time.time + Random.Range(minStateDuration, maxStateDuration);
    }
}
