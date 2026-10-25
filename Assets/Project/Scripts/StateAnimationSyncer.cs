using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class StateAnimationSyncer : MonoBehaviour
{
    private static readonly int ForwardMotionState = Animator.StringToHash("forward_motion_state");
    private static readonly int IsMoving = Animator.StringToHash("is_moving");
    private static readonly int IsFalling = Animator.StringToHash("is_falling");
    private static readonly int IsClimbing = Animator.StringToHash("is_climbing");
    private static readonly int JumpTrigger = Animator.StringToHash("Jump");
    private static readonly int LandTrigger = Animator.StringToHash("Land");
    private static readonly int FallTrigger = Animator.StringToHash("Fall");

    [SerializeField] private Animator _animator;

    private float _moveAnimState;
    private TweenerCore<float, float, FloatOptions> _moveThresholdTween;

    private void Awake()
    {
        if (!_animator) _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        PlayWalkRunAnimation();
    }

    private void OnDisable()
    {
        _moveThresholdTween?.Kill();
    }

    public void PlayWalkRunAnimation(float? threshold = null, float transitionTime = 0f)
    {
        _animator.SetBool(IsMoving, true);
        _animator.SetBool(IsClimbing, false);
        _animator.SetBool(IsFalling, false);

        var targetValue = threshold ?? _moveAnimState;
        _moveThresholdTween?.Kill();

        if (transitionTime <= 0f)
        {
            _moveAnimState = targetValue;
            _animator.SetFloat(ForwardMotionState, targetValue);
        }
        else
        {
            _moveThresholdTween = DOTween.To(() => _moveAnimState, x => _moveAnimState = x, targetValue, transitionTime);
            _moveThresholdTween.OnUpdate(() => _animator.SetFloat(ForwardMotionState, _moveAnimState));
        }
    }

    public void ChangeMoveState(float threshold)
    {
        _moveAnimState = threshold;
        _animator.SetFloat(ForwardMotionState, _moveAnimState);
    }

    public void PlayJumpAnimation()
    {
        _animator.SetBool(IsMoving, false);
        _animator.SetBool(IsClimbing, false);
        _animator.ResetTrigger(LandTrigger);
        _animator.SetTrigger(JumpTrigger);
    }

    public void PlayLandingAnimation()
    {
        _animator.SetBool(IsMoving, false);
        _animator.SetBool(IsFalling, false);
        _animator.SetTrigger(LandTrigger);
    }

    public void PlayFallingAnimation()
    {
        _animator.SetBool(IsMoving, false);
        _animator.SetBool(IsFalling, true);
        _animator.SetTrigger(FallTrigger);
    }

    public void NotifyFalling(bool isFalling)
    {
        _animator.SetBool(IsMoving, false);
        _animator.SetBool(IsFalling, isFalling);
    }

    public void SetClimbing(bool isClimbing)
    {
        _animator.SetBool(IsClimbing, isClimbing);
    }
}
