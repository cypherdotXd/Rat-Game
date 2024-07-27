using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private int moveState_id = 0;
    private int Jump_id = 0;
    private int isFalling_id = 0;
    private int isLanding_id = 0;
    private int isClimbing_id = 0;

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

        PlayWalkRunAnimation(1);

    }

    private void FixedUpdate()
    {
        // animation bools
        _isFalling = !_isGrounded && _rb.velocity.y < 0.0f;

        // Raycasts
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.01f, ~_layerMask);
        _isLanding = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.9f, ~_layerMask) && _isFalling;


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
        if (_isLanding)
            return false;
        _isJumping = true;
        _animator.SetTrigger(Jump_id);
        StartCoroutine(TryLanding());
        return _isGrounded;
    }

    IEnumerator TryLanding()
    {
        while (_isJumping)
        {
            if (_isLanding)
            {
                print("land");
                _animator.SetBool(isLanding_id, true);
                break;
            }
            yield return null;
        }
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
