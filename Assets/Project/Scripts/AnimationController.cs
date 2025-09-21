using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
	[SerializeField] private LayerMask _layerMask;

	private readonly float CLIMB_TIMEOUT = 0.4f;
    private float _moveAnimState;

    private bool _isJumping;
    private bool _isFalling;
    private bool _isLanding;
    private bool _isClimbing;
    private bool _isGrounded;
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
        TouchInputManager.InputMain.jump.performed += Jump;
    }

    private void OnDisable()
    {
        TouchInputManager.InputMain.jump.performed -= Jump;
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
        // animation bools
        _isFalling = !_isGrounded && _rb.linearVelocity.y < -0.1f;
        _animator.SetBool(isFalling_id, _isFalling);
        // Raycasts
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.03f, ~_layerMask);
        
    }

    private void Jump(InputAction.CallbackContext ctx)
    {
        if(!_isGrounded) return;
        PlayJumpAnimation();
    }

    public void PlayWalkRunAnimation(float threshold = 1)
    {
        _animator.SetTrigger("MoveOnGround");
        _moveAnimState = threshold;
        _animator.SetFloat(moveState_id, _moveAnimState);
    }

    public void ChangeMoveState(float threshold)
    {
        _moveAnimState = threshold;
        _animator.SetFloat(moveState_id, _moveAnimState);
    }

    public bool PlayJumpAnimation()
    {
        _isJumping = true;
        _animator.SetTrigger(Jump_id);
        print("jump");
        if(_jumpRoutine != null)
            StopCoroutine(_jumpRoutine);
        _jumpRoutine = StartCoroutine(TryLanding());
        return _isGrounded;
    }

    IEnumerator TryLanding()
    {
        while (_isJumping)
        {
            print($"jumping: {_isJumping}");
            _isLanding = Physics.Raycast(_collider.bounds.center, -transform.up, out var landInfo, _collider.bounds.extents.y + 1.1f, ~_layerMask)
                         && _isFalling;
            if (_isLanding)
            {
                Debug.DrawLine(_collider.bounds.center, landInfo.point, Color.green, 6);
                print("land");
                _animator.SetBool(isLanding_id, true);
                _isJumping = false;
                break;
            }
            yield return null;
        }
        print("land complete");
    }

    public void OnLandAnimationEnd()
    {
        _animator.SetBool(isLanding_id, false);
        _animator.SetBool(Jump_id, false);
    }

    public void OnJumpAnimationEnd()
    {


    }
}
