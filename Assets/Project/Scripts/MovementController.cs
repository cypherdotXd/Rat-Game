using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using MIRA;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;

public class MovementController : StateMachineBehaviour<MovementController>
{
	[Header("References")]
	[SerializeField] private Transform mesh;

	[Space(10)]
	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private AnimationCurve _inputStateCurve;

	public CameraMotion cameraMotion;
	public AnimationCurve inputSpeedCurve;
	public Transform cameraRig;
    public float walkSpeed = 0.7f;
	public float acceleration = 4f;
	public Rigidbody rb;
	public CapsuleCollider playerCollider;
    public Vector3 origin;
	public RaycastHit groundHitInfo;
	public RaycastHit wallHitinfo;
	public bool isGrounded;
	public bool isWallInFront;
	
	[SerializeField] float speedMultiplier = 1f;
	[SerializeField] float maxJumpHeight = 0.4f;
	[SerializeField] float jumpForce = 0.4f;
	[SerializeField] float climbTimeout = 0.5f;
	[SerializeField] float climbForce = 0.6f;
	[SerializeField] float turnAcceleration = 10f;

	public StateController animationController;
	public bool isClimbingPath;
    private float currentForwardSpeed;
	private Vector3 targetVelocity;
	private Vector3 input;
	private PathClimb _pathClimb;
	private Coroutine _climbCoroutine;

	private void Awake()
	{
		Application.targetFrameRate = 60;
		playerCollider = GetComponent<CapsuleCollider>();
		// rb = GetComponent<Rigidbody>();
		animationController = GetComponent<StateController>();
	}

	// Start is called once only 
	private void Start()
	{
		// State : IDLE, WALK, SPRINT, WALL CLIMB
		var moveState = new MoveState(this);
		SwitchState(moveState);
	}

    // Update is called once per frame
    private void Update()
    {
        // MoveWithSpeed(walkSpeed, input, acceleration);
        UpdateState();
	}

	private void FixedUpdate()
    {
	    FixedUpdateState();
	    
        isGrounded = Physics.Raycast(playerCollider.bounds.center, -transform.up, out groundHitInfo, 
	        playerCollider.bounds.extents.y + 0.01f, ~_layerMask);
        isWallInFront = Physics.Raycast(playerCollider.bounds.center - 0.5f * playerCollider.bounds.extents.y * Vector3.up,
	        transform.forward, out wallHitinfo, playerCollider.bounds.extents.z + 0.05f, ~_layerMask, QueryTriggerInteraction.Collide);
        
        origin = playerCollider.bounds.center - playerCollider.bounds.extents.y * Vector3.up;
    }

	public void NotifyClimbPathBegin(PathClimb pathClimb)
	{
		print("Climb");
		_pathClimb = pathClimb;
		isClimbingPath = true;
	}

	public void ClimbPath()
	{
		print("climbing Start");
		isClimbingPath = _pathClimb.ClimbIncrementally(transform, 0.1f * Time.deltaTime * input.y);
		rb.isKinematic = isClimbingPath;
		animationController.ChangeMoveState(input.y);
	}

    
}

[Serializable]
public struct SurroundContextData
{
	public RaycastHit GroundHitInfo;
	public RaycastHit WallHitInfo;
} 

public class MoveState : StateBase<MovementController>
{
	private float _speedMultiplier = 1f;
	private float _currentForwardSpeed;
	private Vector3 _targetVelocity;
	private MovementController _context;

	public MoveState(MovementController stateMachine) : base(stateMachine)
	{
		_context = stateMachine;
	}

	public override void EnterState()
	{
		Debug.Log("Entering Move State");
		InputSystem.actions.FindAction("jump").performed += SwitchToJumpState;
		AddTransition(new IdleState(_context), _ =>
		{
			var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
			return input.sqrMagnitude < 0.001f;
		});
		AddTransition(new WallClimbState(_context, 50), _ => _context.isWallInFront);
		
		_context.rb.isKinematic = false;
	}

	public override void UpdateState()
	{
		TryTransition(_context);
		
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		var targetSpeed = _context.inputSpeedCurve.Evaluate(input.sqrMagnitude) * _context.walkSpeed;
		var lookDirection = _context.cameraRig.forward;
		
		if (input.sqrMagnitude < 0.01f) return;
		
		_currentForwardSpeed = Mathf.Lerp(_currentForwardSpeed, targetSpeed, _context.acceleration * Time.deltaTime);
		_targetVelocity = _speedMultiplier * _currentForwardSpeed * _context.rb.transform.forward;
		_targetVelocity.y = _context.rb.linearVelocity.y;
		_context.rb.linearVelocity = _targetVelocity;
		// Debug.Log(_targetVelocity);
		
		// Rotate towards camera forward direction when moving
		
		var direction = lookDirection;
		direction.y = 0;
		var targetRotation = Quaternion.LookRotation(direction);
		var yAngle = Vector2.SignedAngle(input, Vector2.up);
		targetRotation *= Quaternion.Euler(0, yAngle, 0);

		_context.rb.MoveRotation(Quaternion.Slerp(_context.rb.transform.rotation, targetRotation, 10 * Time.deltaTime));
	}

	public override void FixedUpdateState()
	{
		
	}

	public override void ExitState()
	{
		InputSystem.actions.FindAction("jump").performed -= SwitchToJumpState;
	}
	
	private void SwitchToJumpState(InputAction.CallbackContext _)
	{
		_context.SwitchState(new JumpState(_context, 3.5f));
	}
}

public class IdleState : StateBase<MovementController>
{
	private float _currentForwardSpeed;
	private MovementController _context;
	
	public IdleState(MovementController stateMachine) : base(stateMachine)
	{
		_context = stateMachine;
	}

	public override void EnterState()
	{
		Debug.Log("Entering IdleState");
		AddTransition(new MoveState(_context), _ =>
		{
			var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
			return input.sqrMagnitude > 0.001f;
		});
	}

	public override void UpdateState()
	{

		// rb.linearVelocity = Vector3.zero;
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		TryTransition(_context);

	}

	public override void FixedUpdateState()
	{
		
	}

	public override void ExitState()
	{
	}
}

public class WallClimbState : StateBase<MovementController>
{
	private float _timer;
	private readonly float _timeOut;
	private readonly float _climbForce;
	private bool _currentWallExists = true;
	private bool _newWallInFront;
	private bool _isClimbing;
	private Vector3 _wallDir;
	private Vector3 _lateralDirection;
	private Vector3 _wallNormal;
	private readonly LayerMask layerMask;

	private readonly MovementController _context;

	public WallClimbState(MovementController context, float timeOut, float climbForce = 1.5f) : base(context)
	{
		_context = context;
		_timeOut = timeOut;
		_climbForce = climbForce;
		
		layerMask = LayerMask.GetMask("Dynamic Object");
	}

	public override void EnterState()
	{
		Debug.Log("Entering Wall Climb");

		var climber = _context.rb.transform;
		var wallHitinfo = _context.wallHitinfo;
		_wallDir = (climber.position - wallHitinfo.point).normalized;
		_wallNormal = wallHitinfo.normal;
		var climbAngle = Vector3.SignedAngle(climber.forward, -_wallNormal, Vector3.up);
		_lateralDirection = Quaternion.AngleAxis(climbAngle, -_wallNormal) * Vector3.Cross(climber.right, _wallNormal);
		
		AddTransition(_context.LastState, _ => _timer > _timeOut || (!_currentWallExists && !_newWallInFront));
		AddTransition(new WallClimbState(_context, _timeOut), machine =>
		{
			return false;
			if (!_isClimbing) return false;
			var collider = machine.playerCollider;
			var newWallInFront = Physics.Raycast(collider.bounds.center - 0.5f * collider.bounds.extents.y * Vector3.up,
				climber.forward, out var newWallHitInfo, collider.bounds.extents.z + 0.1f, ~layerMask);
			if (!newWallInFront) return false;
			
			var diff = Vector3.Dot(newWallHitInfo.normal, _wallNormal);
			return diff < 0.9f;
		});
		
		var cameraMotion = _context.cameraMotion;
		// animationController.PlayWalkRunAnimation(1.5f);
		cameraMotion.StartLookFollow();
	}

	public override void UpdateState()
	{
		
	}

	public override void FixedUpdateState()
	{
		TryTransition(_context);

		var climber = _context.rb.transform;
		var rb = _context.rb;
		var collider = _context.playerCollider;
		var _origin = _context.origin;
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		// Debug.DrawRay(wallHitInfo.point, climber.forward, Color.red, 6);
		
		_newWallInFront = Physics.Raycast(collider.bounds.center - 0.5f * collider.bounds.extents.y * Vector3.up,
			climber.forward, out var newWallHitInfo, collider.bounds.extents.z + 0.05f, ~layerMask);
		if (_newWallInFront)
		{
			var diff = Vector3.Dot(newWallHitInfo.normal, _wallNormal);
			if (diff < 0.8f)
				ChangeWall(newWallHitInfo);
		}
		
		_timer += Time.fixedDeltaTime;
		_currentWallExists = Physics.Raycast(_origin, -_wallDir, collider.bounds.extents.z + 0.1f, ~layerMask);
		
		_lateralDirection = Quaternion.AngleAxis(Mathf.Atan2(input.x, 1) * 0.08f * Mathf.Rad2Deg, climber.up) * _lateralDirection;
		var targetRotation = Quaternion.LookRotation(_lateralDirection, _wallNormal);
		climber.rotation = Quaternion.Slerp(climber.rotation, targetRotation, 15f * Time.fixedDeltaTime);
		var wallAlignment = Vector3.Dot(rb.transform.up, _wallNormal);
		_isClimbing = wallAlignment > 0.98f;
		
		// Move Upward
		var climbDir = (-Vector3.Dot(Vector3.up, climber.forward) + 1f)/2f;
		climbDir = Mathf.Clamp(climbDir, 0.8f, 1f);
		var moveDir = climber.forward - 0.4f * climber.up;
		rb.linearVelocity = _climbForce * climbDir * moveDir;
	}
	
	public override void ExitState()
	{
		// StopClimbing();
		// _isClimbing = false;
		// animationController.PlayWalkRunAnimation(1f);
	}

	private void StopClimbing()
	{
		var cameraMotion = _context.cameraMotion;
		var cameraRig = _context.cameraRig;
		var rb = _context.rb;
		
		rb.isKinematic = false;
		cameraMotion.EndLookFollow();
		_isClimbing = false;
		var forward = cameraRig.forward;
		forward.y = 0;
		// rb.linearVelocity = 0.5f * rb.linearVelocity.magnitude * transform.forward;
		// var isGround = Physics.Raycast(_origin, Vector3.down, out var groundInfo, _collider.bounds.extents.y + 25f, ~_layerMask);
		// var forward = forwardDir ?? transform.forward;
		// forward.y = 0;
		// var r = Quaternion.LookRotation(forward.normalized, isGround ? groundInfo.normal : Vector3.up);
		// transform.DORotateQuaternion(r, 0.2f);
	}

	private void ChangeWall(RaycastHit newWallHitInfo)
	{
		Debug.Log("Wall change");
		var climber = _context.rb.transform;
		_timer = 0;
		_wallNormal = newWallHitInfo.normal;
		_wallDir = (climber.position - newWallHitInfo.point).normalized;
		var climbAngle = Vector3.SignedAngle(climber.forward, -_wallNormal, Vector3.up);
		_lateralDirection = Quaternion.AngleAxis(climbAngle, -_wallNormal) * Vector3.Cross(climber.right, _wallNormal);
	}
	
}

public class JumpState : StateBase<MovementController>
{
	private readonly MovementController _machine;
	private readonly float jumpForce;
	
	public JumpState(MovementController stateMachine, float jumpForce) : base(stateMachine)
	{
		_machine = stateMachine;
		this.jumpForce = jumpForce;
	}

	public override void EnterState()
	{
		Debug.Log("Entering Jump State");
		AddTransition(_machine.LastState, controller => controller.isGrounded);
		var rb = _machine.rb;
		rb.AddForce(jumpForce * Vector3.up, ForceMode.Impulse);
	}

	public override void UpdateState()
	{
		TryTransition(_machine);
	}

	public override void FixedUpdateState()
	{
		
	}

	public override void ExitState()
	{
		
	}
	
	// IEnumerator Jump(float delay = 0.1f)
	// {
	// 	yield return new WaitForSeconds(delay);
	// 	if (isClimbingWall)
	// 	{
	// 		StopClimbing();
	// 		rb.AddForce(0.6f * transform.up, ForceMode.Impulse);
	// 		yield break;
	// 	}
	// 	
	// 	var jumpDir = isGrounded ? groundHitInfo.normal : Vector3.up;
	// 	rb.AddForce(jumpForce * jumpDir, ForceMode.Impulse);
	// 	// StartCoroutine(TryWallClimb(0.6f));
	// }
}
