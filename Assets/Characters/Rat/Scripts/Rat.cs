using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;


public class Rat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform rat_mesh;
    [SerializeField] private Transform cameraRig;
    [SerializeField] private Animator _animator;
    [SerializeField] private LayerMask layer_mask;
    private readonly float NORMAL_SPEED = 2f;
    private readonly float ACCELERATION = 5f;
    private readonly float SPEED_MULTIPLIER = 1f;
    private readonly float MAX_JUMP_HEIGHT = 1f;
    private readonly float CLIMB_TIMEOUT = 0.7f;
    private readonly float CLIMB_FORCE = 2f;
    private readonly float TURN_ACC = 2f;
    private readonly float HEALTH = 100f;

    private Vector3 _velocity;
    private Vector3 targetVelocity;
    private Vector3 positionPreviousFrame;

    private bool is_moving = false;
    private bool is_wall_infront = false;
    private bool is_grounded = false;
    private bool is_jumping = false;
    private bool is_falling = false;
    private bool climbing_wall_flag = false;

    private float current_forward_speed = 0f;
    private float y_rotation = 0f;
    private float target_jump_height = 0f;
    private float forward_motion_animState = 0f;
    private float wall_climb_timer = 0f;

    private int forward_motion_state_id = 0;
    private int is_jumping_id = 0;
    private int is_falling_id = 0;
    private int is_climbing_id = 0;

    private Rigidbody _rb;

    private CapsuleCollider _collider;


    public void OnEnable()
    {
        EventManager.jumpEvent += jumpAndFall;
        EventManager.joystickMoveEvent += walkAndRun;
        EventManager.touchMoved += lookAround;
        TouchInputManager.swipeUpInL += startWallClimb;
    }

    public void OnDisable()
    {
        EventManager.jumpEvent -= jumpAndFall;
        EventManager.joystickMoveEvent -= walkAndRun;
        EventManager.touchMoved -= lookAround;
        TouchInputManager.swipeUpInL -= startWallClimb;
    }

    // Start is called once only 
    void Start()
    {
        //inits
        is_jumping = false;
        is_falling = false;
        forward_motion_animState = 0.1f;
        _collider = GetComponent<CapsuleCollider>();
        _rb = GetComponent<Rigidbody>();
        forward_motion_state_id = Animator.StringToHash("forward_motion_state");
        is_jumping_id = Animator.StringToHash("is_jumping");
        is_falling_id = Animator.StringToHash("is_falling");
        is_climbing_id = Animator.StringToHash("is_climbing");
        target_jump_height = MAX_JUMP_HEIGHT;
    }
    Vector3 cameraOffset = Vector3.zero;
    // Update is called once per frame
    void Update()
    {
        cameraRig.position = transform.position + cameraOffset;

        // calculates the real velocity manually and stores it into the velocity variable (used to check if rat is falling downwards)
        _velocity = (transform.localPosition - positionPreviousFrame) / Time.deltaTime;
        positionPreviousFrame = transform.localPosition;

        // setting animation states and flags
        is_falling = !is_grounded && _velocity.y < 0.0f && !climbing_wall_flag;
        is_jumping = !(is_grounded || is_falling) && !climbing_wall_flag;

        setAnimationState();
    }


    private RaycastHit ground_hitinfo = new RaycastHit();
    private RaycastHit wall_hitinfo = new RaycastHit();

    private void FixedUpdate()
    {

        //  climb wall
        if (climbing_wall_flag)
        {
            climbWall();
        }
        if (!is_wall_infront || wall_climb_timer > CLIMB_TIMEOUT)
        {
            resetClimbWall();
        }

        //Raycasts
        is_wall_infront = Physics.Raycast(_collider.bounds.center, transform.forward, out wall_hitinfo, _collider.bounds.extents.z + 0.3f, ~layer_mask);
        is_grounded = Physics.Raycast(_collider.bounds.center, -transform.up, out ground_hitinfo, _collider.bounds.extents.y + 0.5f, ~layer_mask);

        //move();

    }

    float xRotation = 0;
    float yOffset = 0;
    private void lookAround(Vector2 delta)
    {
        cameraRig.transform.eulerAngles += delta.x * 15 * Time.deltaTime * Vector3.up;
        xRotation -= delta.y * 0.05f;
        xRotation = Mathf.Clamp(xRotation, -10, 30);
        Camera.main.transform.localRotation = Quaternion.Slerp(Camera.main.transform.localRotation, Quaternion.Euler(xRotation, 0, 0), 5f * Time.deltaTime);
        yOffset -= delta.y * 0.002f;
        yOffset = Mathf.Clamp(yOffset, -0.6f, 0.6f);
        cameraOffset = Vector3.Lerp(cameraOffset, yOffset * Vector3.up, 3f * Time.deltaTime);
    }

    private void moveWihtSpeed(float speed, float threshold, float acceleration)
    {
        //move if grounded
        bool can_move = !is_wall_infront;
        //can_move = !is_wall_infront && !ground_hitinfo.collider.CompareTag("unwalkable");
        _animator.SetFloat(forward_motion_state_id, threshold, 1 / acceleration, Time.deltaTime);
        can_move = !is_wall_infront;
        if (can_move || threshold < 0f)
        {
            is_moving = true;
            current_forward_speed = Mathf.Lerp(current_forward_speed, threshold * speed, acceleration * Time.deltaTime);
            targetVelocity = current_forward_speed * transform.forward;
            targetVelocity.y = _rb.velocity.y;
            _rb.velocity = targetVelocity;
        }
        if(threshold != 0)
        {
            //rotate
            float yRotation = cameraRig.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Abs(threshold) * TURN_ACC * Time.deltaTime);
        }
    }

    private void walkAndRun(float inputDirection)
    {
        if (is_grounded)
        {
            moveWihtSpeed(NORMAL_SPEED, SPEED_MULTIPLIER * inputDirection, ACCELERATION);
        }
        else
        {
            if(inputDirection > 1.1f) moveWihtSpeed(NORMAL_SPEED,2* SPEED_MULTIPLIER, ACCELERATION); else moveWihtSpeed(NORMAL_SPEED, SPEED_MULTIPLIER, ACCELERATION);
        }
    }

    private void jumpAndFall()
    {
        if (is_grounded)
        {
            is_jumping = true;
            _rb.velocity = Mathf.Sqrt(target_jump_height * 20f) * Vector3.up;
        }
    }

    private void startWallClimb()
    {
        if (is_wall_infront && !is_grounded)
        {
            climbing_wall_flag = true;
        }
    }

    // try to climb wall if player hits jump and swipe up in front of the wall
    private void climbWall()
    {
        wall_climb_timer += Time.deltaTime;
        forward_motion_animState = 1f;
        climbing_wall_flag = true;
        // calculate and apply perpendicular angle between wall and player mesh
        Vector3 normal = wall_hitinfo.normal;
        float angle = Vector3.SignedAngle(transform.up, normal, transform.right);
        rat_mesh.localEulerAngles = Vector3.right * angle;
        // apply upword force
        _rb.velocity = CLIMB_FORCE * Mathf.Sqrt(target_jump_height * 2f * 9.8f) * Vector3.up;

    }

    private void resetClimbWall()
    {
        if (wall_climb_timer > CLIMB_TIMEOUT)
        {
            _rb.velocity = -1.3f * Mathf.Sqrt(target_jump_height * 2f * 9.8f) * transform.forward;
        }
        wall_climb_timer = 0f;
        climbing_wall_flag = false;

        rat_mesh.localEulerAngles = Vector3.right * 0f;
    }

    private void setAnimationState()
    {
        _animator.SetBool(is_jumping_id, is_jumping);
        _animator.SetBool(is_falling_id, is_falling);
        _animator.SetBool(is_climbing_id, climbing_wall_flag);

    }
}
