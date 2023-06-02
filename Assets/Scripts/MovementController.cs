using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;


public class MovementController: MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform rat_mesh;
	[SerializeField] private Transform camera_rig;
	[SerializeField] private LayerMask layer_mask;
	private readonly float NORMAL_SPEED = 0.7f;
	private readonly float ACCELERATION = 4f;
	private readonly float SPEED_MULTIPLIER = 1f;
	private readonly float MAX_JUMP_HEIGHT = 0.4f;
	private readonly float CLIMB_TIMEOUT = 0.4f;
	private readonly float CLIMB_FORCE = 0.6f;
	private readonly float TURN_ACC = 2f;

	private Vector3 targetVelocity;

	public static float move_anim_state = 0;
	public static bool is_moving = false;
	public static bool is_wall_infront = false;
	public static bool is_grounded = false;
	public static bool is_landing = false;
    public static bool is_jumping = false;
	public static bool is_falling = false;
	public static bool is_climbing = false;

	private float current_forward_speed = 0f;
	private float wall_climb_timer = 0f;


	private Rigidbody _rb;
	private CapsuleCollider _collider;

	// Start is called once only 
	private void Start()
	{
		//inits
		is_jumping = false;
		is_falling = false;
		_collider = GetComponent<CapsuleCollider>();
		_rb = GetComponent<Rigidbody>();
        TouchInputManager.InputMain.jump.performed += ctx => JumpAndFall(ctx);
    }

    // Update is called once per frame
    void Update()
	{
		// setting animation states and flags
		is_falling = !is_grounded && _rb.velocity.y < 0.0f;
		is_jumping = !is_grounded && _rb.velocity.y > 0.0f;
		is_climbing = is_wall_infront && wall_climb_timer < CLIMB_TIMEOUT && !is_falling && is_jumping;

		float inputY = TouchInputManager.InputMain.move.ReadValue<float>();
		WalkAndRun(inputY);

	}

	private RaycastHit wall_hitinfo = new RaycastHit();
	private void FixedUpdate()
	{

		WallClimb();

		//Raycasts
		is_wall_infront = Physics.Raycast(_collider.bounds.center, transform.forward, out wall_hitinfo, _collider.bounds.extents.z + 0.1f, ~layer_mask);
		is_grounded = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.01f, ~layer_mask);
		is_landing = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.2f, ~layer_mask);

    }

	// movement logic
    private void MoveWihtSpeed(float speed, float threshold, float acceleration)
	{
		//move if grounded
		bool can_move = !is_wall_infront;

		float targetSpeed, targetAcceleration;
		move_anim_state = Mathf.Lerp(move_anim_state, threshold, acceleration * Time.deltaTime);
		if (can_move || threshold < 0f) {
			if (!is_grounded)
			{
				targetSpeed = 0.1f * speed;
				targetAcceleration = 1.7f;
			}
			else
			{
				targetSpeed = speed;
				targetAcceleration = acceleration;
			}
			current_forward_speed = Mathf.Lerp(current_forward_speed, threshold * targetSpeed, targetAcceleration * Time.deltaTime);
			is_moving = true;
			targetVelocity = SPEED_MULTIPLIER * current_forward_speed * transform.forward;
			targetVelocity.y = _rb.velocity.y;
			_rb.velocity = targetVelocity;
		}
		if(threshold != 0)
		{
			//rotate player towards camera forward direction
			float yRotation = camera_rig.eulerAngles.y;
			Quaternion targetRotation = Quaternion.Euler(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Abs(threshold) * TURN_ACC * Time.deltaTime);
		}
	}

	// walk and run according to the input
	private void WalkAndRun(float inputY)
	{
		MoveWihtSpeed(NORMAL_SPEED, inputY, ACCELERATION);
	}

	// jump
	private void JumpAndFall(InputAction.CallbackContext ctx)
	{
		//jump only when on ground
		if (is_grounded || is_landing)
		{
			is_jumping = true;
			_rb.velocity = Mathf.Sqrt(MAX_JUMP_HEIGHT * 16f) * Vector3.up;
		}
	}

	// climb wall
	void WallClimb()
	{
		// try to climb wall if player in front of the wall
		if (is_climbing)
		{
			wall_climb_timer += Time.deltaTime;
			// calculate and apply perpendicular angle between wall and player mesh
			Vector3 normal = wall_hitinfo.normal;
			float angle = Vector3.SignedAngle(transform.up, normal, transform.right);
			rat_mesh.localEulerAngles = Vector3.right * angle;
			// apply upward force
			_rb.velocity = CLIMB_FORCE * Mathf.Sqrt(MAX_JUMP_HEIGHT * 2f * 8f) * Vector3.up;
		}
		// stop climbing wall
		if (!is_climbing)
		{
			if (wall_climb_timer > CLIMB_TIMEOUT)
			{
				_rb.velocity = -0.6f * Mathf.Sqrt(MAX_JUMP_HEIGHT * 2f * 9.8f) * transform.forward;
			}
			wall_climb_timer = 0f;                

			rat_mesh.localEulerAngles = Vector3.right * 0f;
		}
	}

}
