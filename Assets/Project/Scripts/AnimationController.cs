using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class StateController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private float _landTriggerDistance = 0.1f;

	private readonly float CLIMB_TIMEOUT = 0.4f;
    private float _moveAnimState;

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
        // Raycasts
        _isGroundInReach = Physics.Raycast(_collider.bounds.center, -transform.up, out _landHitInfo,
            _collider.bounds.extents.y + 1f, ~_layerMask);
        _isGrounded = _isGroundInReach && _landHitInfo.distance < 0.035f;
        // animation bools
        _isFalling = !_isGrounded && _rb.linearVelocity.y < -0.05f;
        _animator.SetBool(isFalling_id, _isFalling);
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

    private void Jump(InputAction.CallbackContext ctx)
    {
        // if(!_isGrounded) return;
        PlayJumpAnimation();
    }

    public bool PlayJumpAnimation()
    {
        _isJumping = true;
        _animator.SetTrigger(Jump_id);
        if(_jumpRoutine != null)
            StopCoroutine(_jumpRoutine);
        _jumpRoutine = StartCoroutine(TryLanding());
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

}
