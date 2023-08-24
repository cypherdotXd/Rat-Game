using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
	[SerializeField] private LayerMask _layerMask;

	private readonly float CLIMB_TIMEOUT = 0.4f;
    private float _moveAnimState;
    private float wallClimbTimer = 0f;

    private bool _isJumping;
    private bool _isFalling;
    private bool _isLanding;
    private bool _isClimbing;
    private bool _isGrounded;
    private Rigidbody _rb;
    private Collider _collider;

    private int forward_motion_state_id = 0;
    private int is_jumping_id = 0;
    private int is_falling_id = 0;
    private int is_landing_id = 0;
    private int is_climbing_id = 0;

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        forward_motion_state_id = Animator.StringToHash("forward_motion_state");
        is_jumping_id  = Animator.StringToHash("is_jumping");
        is_falling_id  = Animator.StringToHash("is_falling");
        is_landing_id  = Animator.StringToHash("is_landing");
        is_climbing_id = Animator.StringToHash("is_climbing");
    }

    // Update is called once per frame
    void Update()
    {

        _animator.SetFloat(forward_motion_state_id, _moveAnimState);
        _animator.SetBool(is_jumping_id,  _isJumping);
        _animator.SetBool(is_falling_id,  _isFalling);
        _animator.SetBool(is_landing_id,  _isLanding);
        _animator.SetBool(is_climbing_id, _isClimbing);
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

    public void PlayWalkRunAnimation(float threshold)
    {
        _moveAnimState = threshold;
    }

    public bool PlayJumpAnimation()
    {
        if (_isGrounded)
        {
            _isJumping = true;
        }
        return _isGrounded;
    }

    public void PlayClimbingAnimation(bool value)
    {
        _isClimbing = value;
        if (value)
        {
            PlayWalkRunAnimation(1);
        }
    }
}
