using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    private int forward_motion_state_id = 0;
    private int is_jumping_id = 0;
    private int is_falling_id = 0;
    private int is_landing_id = 0;
    private int is_climbing_id = 0;

    // Start is called before the first frame update
    void Start()
    {
        forward_motion_state_id = Animator.StringToHash("forward_motion_state");
        is_jumping_id = Animator.StringToHash("is_jumping");
        is_falling_id = Animator.StringToHash("is_falling");
        is_landing_id = Animator.StringToHash("is_landing");
        is_climbing_id = Animator.StringToHash("is_climbing");
    }

    // Update is called once per frame
    void Update()
    {
        float moveAnimState = MovementController.move_anim_state;
        bool isJumping = MovementController.is_jumping;
        bool isFalling = MovementController.is_falling;
        bool isLanding = MovementController.is_landing;
        bool isClimbing = MovementController.is_climbing;

        _animator.SetFloat(forward_motion_state_id, moveAnimState);
        _animator.SetBool(is_jumping_id, isJumping);
        _animator.SetBool(is_falling_id, isFalling);
        _animator.SetBool(is_landing_id, isLanding);
        _animator.SetBool(is_climbing_id, isClimbing);
    }
}
