using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
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

    private int moveState_id = 0;
    private int isJumping_id = 0;
    private int isFalling_id = 0;
    private int isLanding_id = 0;
    private int isClimbing_id = 0;

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        moveState_id = Animator.StringToHash("forward_motion_state");
        isJumping_id  = Animator.StringToHash("is_jumping");
        isFalling_id  = Animator.StringToHash("is_falling");
        isLanding_id  = Animator.StringToHash("is_landing");
        isClimbing_id = Animator.StringToHash("is_climbing");

        PlayWalkRunAnimation(1);
    }

    private void FixedUpdate()
    {
        // Raycasts
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.01f, ~_layerMask);
        _isLanding = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.9f, ~_layerMask);

        // animation bools
        _isFalling = !_isGrounded && _rb.velocity.y < 0.0f;
        _isJumping = !_isGrounded && !_isClimbing && _rb.velocity.y > 0.0f;

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
        if (_isGrounded)
        {
            _isJumping = true;
        }
        return _isGrounded;
    }

}
