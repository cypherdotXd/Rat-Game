using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PlayerContorller : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayerMask;
    private readonly float NORMAL_SPEED = 2f;
    private readonly float ACCELERATION = 5f;
    private readonly float SPEED_MULTIPLIER = 1f;
    private readonly float MAX_JUMP_HEIGHT = 1f;
    private readonly float CLIMB_TIMEOUT = 0.7f;
    private readonly float CLIMB_FORCE = 2f;
    private readonly float TURN_ACC = 2f;

    private Animator _animator;
    private Vector3 targetVelocity;

    private bool is_moving = false;
    private bool is_wall_infront = false;
    private bool is_grounded = false;
    private bool is_jumping = false;
    private bool is_falling = false;

    private float current_forward_speed = 0f;
    private float y_rotation = 0f;
    private float forward_motion_animState = 0f;

    private int forward_motion_state_id = 0;
    private int is_jumping_id = 0;
    private int is_falling_id = 0;
    private int is_climbing_id = 0;

    private Rigidbody _rb;
    private CapsuleCollider _collider;

    public void OnEnable()
    {
        EventManager.joystickMoveEvent += walkAndRun;
        EventManager.touchMoved += lookAround;
    }

    public void OnDisable()
    {
        EventManager.joystickMoveEvent -= walkAndRun;
        EventManager.touchMoved -= lookAround;
    }

    // Start is called before the first frame update
    void Start()
    {
        forward_motion_state_id = Animator.StringToHash("SpeedThreshold");

        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    float xRotation = 0;
    float yOffset = 0;
    Vector3 cameraOffset = Vector3.zero;
    private void lookAround(Vector2 delta)
    {
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
    }

    private void walkAndRun(Vector2 inputDirection)
    {
        if (is_grounded)
        {
            moveWihtSpeed(NORMAL_SPEED, SPEED_MULTIPLIER * inputDirection.y, ACCELERATION);
        }
        else
        {
            if (inputDirection.y > 1.1f) moveWihtSpeed(NORMAL_SPEED, 2 * SPEED_MULTIPLIER, ACCELERATION); else moveWihtSpeed(NORMAL_SPEED, SPEED_MULTIPLIER, ACCELERATION);
        }
    }
}
