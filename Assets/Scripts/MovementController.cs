using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class MovementController: MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform ratMesh;
	[SerializeField] private Transform cameraRig;
	[Space(10)]
	[SerializeField] private LayerMask _layerMask;
    private readonly float NORMAL_SPEED = 0.7f;
	private readonly float ACCELERATION = 4f;
	private readonly float SPEED_MULTIPLIER = 1f;
	private readonly float MAX_JUMP_HEIGHT = 0.4f;
	private readonly float CLIMB_TIMEOUT = 0.5f;
	private readonly float CLIMB_FORCE = 0.6f;
	private readonly float TURN_ACC = 10f;

	private Vector3 targetVelocity;

	public bool isMoving = false;
	private bool _isGrounded;
	private bool isWallInFront;

    private float currentForwardSpeed = 0f;
	private float wallClimbTimer = 0f;

	private Rigidbody _rb;
	private CapsuleCollider _collider;
	private AnimationController _animationController;

	// Start is called once only 
	private void Start()
	{
		Application.targetFrameRate = 60;
		_collider = GetComponent<CapsuleCollider>();
		_rb = GetComponent<Rigidbody>();
		_animationController = GetComponent<AnimationController>();
        TouchInputManager.InputMain.jump.performed += ctx => JumpAndFall(ctx);
    }

    // Update is called once per frame
    void Update()
	{

        float inputY = TouchInputManager.InputMain.move.ReadValue<float>();
		WalkAndRun(inputY);

	}

	private static RaycastHit wallHitinfo = new();
	private void FixedUpdate()
    {
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.01f, ~_layerMask);
        isWallInFront = Physics.Raycast(_collider.bounds.center - _collider.bounds.extents.y * Vector3.up, transform.forward, out wallHitinfo, _collider.bounds.extents.z + 0.1f, ~_layerMask);
		WallClimb();
    }

    private void WalkAndRun(float inputY)
	{
		MoveWihtSpeed(NORMAL_SPEED, inputY, ACCELERATION);
	}

    private void MoveWihtSpeed(float speed, float threshold, float acceleration)
	{
        bool canMove = threshold != 0;
		
		currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, threshold * speed, acceleration * Time.deltaTime);
		_animationController.PlayWalkRunAnimation(currentForwardSpeed / speed);
		if (canMove) {
			//isMoving = true;
			targetVelocity = SPEED_MULTIPLIER * currentForwardSpeed * transform.forward;
			targetVelocity.y = _rb.velocity.y;
			_rb.velocity = targetVelocity;
		}

		// Rotate towards camera forward direction when moving
		if(threshold != 0)
		{
			Vector3 direction = cameraRig.transform.forward;
			direction.y = 0;
			Quaternion targetRotation = Quaternion.LookRotation(direction);
			
			_rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, TURN_ACC * Time.deltaTime));
		}
	}
	    
	private void JumpAndFall(InputAction.CallbackContext ctx)
	{
		//Jump only when grounded
		if (!_isGrounded)
			return;
		_rb.velocity = Mathf.Sqrt(MAX_JUMP_HEIGHT * 16f) * Vector3.up;
	}

	void WallClimb()
	{
		bool canClimbWall = isWallInFront && wallClimbTimer < CLIMB_TIMEOUT;
		if (canClimbWall)
		{
			if (_isGrounded)
				return;
			wallClimbTimer += Time.fixedDeltaTime;
			_animationController.PlayClimbingAnimation(true);
			// Rotate mesh(not this rb) parallel to surface
            Vector3 normal = wallHitinfo.normal;
            float angle = Vector3.SignedAngle(transform.up, normal, transform.right);
			ratMesh.localEulerAngles = angle * wallClimbTimer * Vector3.right / CLIMB_TIMEOUT;

			// Move Upward
			_rb.velocity = CLIMB_FORCE * Mathf.Sqrt(MAX_JUMP_HEIGHT * 2f * 8f) * Vector3.up;
		}

		// Stop Climbing
		if (!canClimbWall)
		{
            _animationController.PlayClimbingAnimation(false);
			if(wallClimbTimer > 0)
			{
				_rb.velocity += 0.9f * transform.forward;
			}
            if (wallClimbTimer > CLIMB_TIMEOUT)
			{
				_rb.velocity = -0.6f * Mathf.Sqrt(MAX_JUMP_HEIGHT * 2f * 9.8f) * transform.forward;
			}

			wallClimbTimer = 0f;
            ratMesh.localEulerAngles = 0.00f * wallClimbTimer * Vector3.right / CLIMB_TIMEOUT;
		}
	}

}
