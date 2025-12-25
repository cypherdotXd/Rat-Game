using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private float _landTriggerDistance = 0.1f;

    private float _moveAnimState;
    private TweenerCore<float, float, FloatOptions> _moveThresholdTween;

    public bool _isJumping;
    public bool _isFalling;
    public bool _isLanding;
    public bool _isClimbing;
    public bool _isGrounded;
    public bool _isGroundInReach;
    private RaycastHit _landHitInfo;
    private Rigidbody _rb;
    private Collider _collider;
    private Coroutine _jumpRoutine;
    private int moveState_id = 0;
    private int Jump_id = 0;
    private int isFalling_id = 0;
    private int isLanding_id = 0;
    private int isClimbing_id = 0;

    private void OnEnable()
    {
        InputSystem.actions.FindAction("jump").performed += Jump;
    }

    private void OnDisable()
    {
        InputSystem.actions.FindAction("jump").performed -= Jump;
    }

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        moveState_id = Animator.StringToHash("forward_motion_state");
        Jump_id  = Animator.StringToHash("Jump");
        isFalling_id  = Animator.StringToHash("is_falling");
        isLanding_id  = Animator.StringToHash("is_landing");
        isClimbing_id = Animator.StringToHash("is_climbing");

        PlayWalkRunAnimation();
    }

    private void FixedUpdate()
    {
        // Raycasts
        _isGroundInReach = Physics.Raycast(_collider.bounds.center, -transform.up, out _landHitInfo,
            _collider.bounds.extents.y + 1f, ~_layerMask);
        _isGrounded = _isGroundInReach && _landHitInfo.distance < 0.035f;
        // animation bools
        _isFalling = !_isGrounded && _rb.linearVelocity.y < -0.05f;
        // _animator.SetBool(isFalling_id, _isFalling);
    }
    
    public void PlayWalkRunAnimation(float? threshold = null, float transitionTime = 0)
    {
        _animator.SetBool("is_moving", true);
        _animator.SetTrigger("MoveOnGround");
        var targetValue = threshold ?? _moveAnimState;
        _moveThresholdTween?.Kill();
        if (transitionTime == 0)
        {
            _moveAnimState = targetValue;
            _animator.SetFloat(moveState_id, targetValue);
        }
        else
        {
            _moveThresholdTween = DOTween.To(() => _moveAnimState, x => _moveAnimState = x, targetValue, transitionTime);
            _moveThresholdTween.OnUpdate(() => _animator.SetFloat(moveState_id, _moveAnimState, _moveAnimState, Time.deltaTime));
        }
    }

    public void ChangeMoveState(float threshold)
    {
        _moveAnimState = threshold;
        _animator.SetFloat(moveState_id, _moveAnimState);
    }

    private void Jump(InputAction.CallbackContext ctx)
    {
        // if(!_isGrounded) return;
        // PlayJumpAnimation();
    }

    public bool PlayJumpAnimation()
    {
        _isJumping = true;
        _animator.SetBool("is_moving", false);
        _animator.SetTrigger(Jump_id);
        // if(_jumpRoutine != null)
        //     StopCoroutine(_jumpRoutine);
        // _jumpRoutine = StartCoroutine(TryLanding());
        return _isGrounded;
    }

    IEnumerator TryLanding()
    {
        while (_rb.linearVelocity.y == 0f) yield return null;
        while (_isJumping)
        {
            var isJumpingDone = _isGroundInReach && _landHitInfo.distance < _landTriggerDistance && _rb.linearVelocity.y < -0.0001f;
            if (isJumpingDone)
            {
                // print($"Land: {_rb.linearVelocity.y}, d: {_landHitInfo.distance}");
                Debug.DrawLine(_collider.bounds.center, _landHitInfo.point, Color.green, 6);
                _isJumping = false;
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        print($"LAND");
        _animator.SetTrigger("Land");
    }

    public void PlayLandingAnimation()
    {
        _animator.SetTrigger("Land");
        _animator.SetBool("is_moving", false);
    }
    
    public void PlayFallingAnimation(bool isFalling)
    {
        _animator.SetBool("is_moving", false);
        _animator.SetBool(isFalling_id, isFalling);
    }

}
