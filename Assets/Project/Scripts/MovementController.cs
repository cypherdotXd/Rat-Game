using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using MIRA;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

public class MovementController : StateMachineBehaviour<MovementController>
{
	[Header("References")]
	[SerializeField] private Transform mesh;

	[Space(10)]
	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private AnimationCurve _inputStateCurve;

	public Vector3 velocity;
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
	public bool isGrounded = true;
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
        velocity = rb.linearVelocity;
	}

	private void FixedUpdate()
    {
	    FixedUpdateState();
	    
        // var isGround = Physics.Raycast(playerCollider.bounds.center, -transform.up, out groundHitInfo, playerCollider.bounds.extents.y + 1f, ~_layerMask);
        var isGround = Physics.SphereCast(playerCollider.bounds.center + 0.02f * Vector3.up, 0.05f, -Vector3.up, out groundHitInfo, 50f, ~_layerMask);
        // Debug.Log($"isGround: {isGround}, distance: {groundHitInfo.distance}");
        isGrounded = isGround && groundHitInfo.distance < 0.035f;
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
public struct SurroundmachineData
{
	public RaycastHit GroundHitInfo;
	public RaycastHit WallHitInfo;
}

public class MoveState : StateBase<MovementController>
{
	private float _speedMultiplier = 1f;
	private float _currentForwardSpeed;
	private Vector3 _targetVelocity;
	private MovementController _machine;

	public MoveState(MovementController stateMachine) : base(stateMachine)
	{
		_machine = stateMachine;
	}

	public override void EnterState()
	{
		Debug.Log("Entering Move State");
		InputSystem.actions.FindAction("jump").performed += SwitchToJumpState;
		AddTransition(new IdleState(_machine), _ =>
		{
			var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
			return input.sqrMagnitude < 0.001f;
		});
		AddTransition(new FallingState(_machine), context => context.groundHitInfo.distance > 0.45f);
		var animationController = _machine.animationController;
		animationController.PlayWalkRunAnimation();
		var cameraMotion = _machine.cameraMotion;
		cameraMotion.SetFov(80, 0.6f);
		cameraMotion.SetDistance(0.15f, 0.6f);
		_machine.rb.isKinematic = false;
	}

	public override void UpdateState()
	{
		
	}

	public override void FixedUpdateState()
	{
		TryTransition(_machine);
		
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		var lookDirection = _machine.cameraRig.forward;
		var animationController = _machine.animationController;
		
		if (input.sqrMagnitude < 0.01f) return;
		animationController.ChangeMoveState(input.sqrMagnitude * 1.5f);
		
		// Rotate towards camera forward direction when moving
		var direction = lookDirection;
		direction.y = 0;
		var targetRotation = Quaternion.LookRotation(direction);
		var yAngle = Vector2.SignedAngle(input, Vector2.up);
		targetRotation *= Quaternion.Euler(0, yAngle, 0);
		_machine.rb.MoveRotation(Quaternion.Slerp(_machine.rb.transform.rotation, targetRotation, 10 * Time.deltaTime));
				
		_targetVelocity = _speedMultiplier * _machine.walkSpeed * input.sqrMagnitude * _machine.rb.transform.forward;
		_targetVelocity.y = _machine.rb.linearVelocity.y;

		if (_machine.rb.linearVelocity.sqrMagnitude > _targetVelocity.sqrMagnitude) return;
		
		var forceDir = targetRotation * Vector3.forward;
		var force = Mathf.Clamp(Vector3.Distance(_machine.rb.linearVelocity, _targetVelocity), 0, 3);
		_machine.rb.AddForce(_machine.acceleration * force * forceDir, ForceMode.VelocityChange);

	}

	public override void ExitState()
	{
		InputSystem.actions.FindAction("jump").performed -= SwitchToJumpState;
		var cameraMotion = _machine.cameraMotion;
		cameraMotion.SetFov(60, 0.6f);
		cameraMotion.SetDistance(0.3f, 0.6f);
	}
	
	private void SwitchToJumpState(InputAction.CallbackContext _)
	{
		_machine.SwitchState(new JumpState(_machine, 3.5f));
	}
}

public class IdleState : StateBase<MovementController>
{
	private float _currentForwardSpeed;
	private MovementController _machine;
	
	public IdleState(MovementController stateMachine) : base(stateMachine)
	{
		_machine = stateMachine;
	}

	public override void EnterState()
	{
		Debug.Log("Entering IdleState");
		AddTransition(new MoveState(_machine), _ =>
		{
			var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
			return input.sqrMagnitude > 0.001f;
		});
		var animationController = _machine.animationController;
		animationController.PlayWalkRunAnimation(0, 0.4f);
		InputSystem.actions.FindAction("jump").performed += SwitchToJumpState;
	}

	public override void UpdateState()
	{

		// rb.linearVelocity = Vector3.zero;
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		TryTransition(_machine);

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
		_machine.SwitchState(new JumpState(_machine, 3.5f));
	}
}

public class WallClimbState : StateBase<MovementController>
{
	private float _timer;
	private readonly float _timeOut;
	private readonly float _climbForce;
	private bool _currentWallExists = true;
	private bool _newWallInFront;
	private Vector3 _wallDir;
	private Vector3 _lateralDirection;
	private Vector3 _wallNormal;
	private readonly LayerMask layerMask;

	private readonly MovementController _machine;

	public WallClimbState(MovementController machine, float timeOut, float climbForce = 1.5f) : base(machine)
	{
		_machine = machine;
		_timeOut = timeOut;
		_climbForce = climbForce;
		
		layerMask = LayerMask.GetMask("Dynamic Object");
	}

	public override void EnterState()
	{
		Debug.Log("Entering Wall Climb");

		var climber = _machine.rb.transform;
		var wallHitinfo = _machine.wallHitinfo;
		var animationController = _machine.animationController;
		_wallDir = (climber.position - wallHitinfo.point).normalized;
		_wallNormal = wallHitinfo.normal;
		var climbAngle = Vector3.SignedAngle(climber.forward, -_wallNormal, Vector3.up);
		_lateralDirection = Quaternion.AngleAxis(climbAngle, -_wallNormal) * Vector3.Cross(climber.right, _wallNormal);
		
		AddTransition(new MoveState(_machine), _ => _timer > _timeOut || (!_currentWallExists && !_newWallInFront));
		
		var cameraMotion = _machine.cameraMotion;
		animationController.PlayWalkRunAnimation(1.5f);
		cameraMotion.StartLookFollow();
	}

	public override void UpdateState()
	{
		
	}

	public override void FixedUpdateState()
	{
		TryTransition(_machine);

		var climber = _machine.rb.transform;
		var rb = _machine.rb;
		var collider = _machine.playerCollider;
		var _origin = _machine.origin;
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
		// _isClimbing = wallAlignment > 0.98f;
		
		// Move Upward
		var climbDir = (-Vector3.Dot(Vector3.up, climber.forward) + 1f)/2f;
		climbDir = Mathf.Clamp(climbDir, 0.8f, 1f);
		var moveDir = climber.forward - 0.4f * climber.up;
		rb.linearVelocity = _climbForce * climbDir * moveDir;
	}
	
	public override void ExitState()
	{
		StopClimbing();
		// animationController.PlayWalkRunAnimation(1f);
	}

	private void StopClimbing()
	{
		var cameraMotion = _machine.cameraMotion;
		var cameraRig = _machine.cameraRig;
		var rb = _machine.rb;
		
		rb.isKinematic = false;
		cameraMotion.EndLookFollow();
		var forward = cameraRig.forward;
		forward.y = 0;
		// rb.linearVelocity = 0.5f * rb.linearVelocity.magnitude * transform.forward;
		forward.y = 0;
		var r = Quaternion.LookRotation(forward.normalized, _machine.groundHitInfo.collider ? _machine.groundHitInfo.normal : Vector3.up);
		_machine.rb.transform.DORotateQuaternion(r, 0.2f);
	}

	private void ChangeWall(RaycastHit newWallHitInfo)
	{
		Debug.Log("Wall change");
		var climber = _machine.rb.transform;
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
	private readonly float _jumpForce;
	private float _jumpTimer;
	private float _jumpDelay;
	private bool _jumpExecuted;
	
	public JumpState(MovementController stateMachine, float jumpForce, float jumpDelay = 0.05f) : base(stateMachine)
	{
		_machine = stateMachine;
		_jumpForce = jumpForce;
		_jumpDelay = jumpDelay;
	}

	public override void EnterState()
	{
		Debug.Log("Entering Jump State");
		var animController =  _machine.animationController;
		AddTransition(_machine.LastState, controller =>
		{
			if (!_jumpExecuted) return false;
			var jumpEnded =  controller.velocity.y <= -0.01f && controller.groundHitInfo.distance < 0.4f;
			if (jumpEnded)
				controller.animationController.PlayLandingAnimation();
			return jumpEnded;
		});
		AddTransition(new WallClimbState(_machine, 50), _ => _machine.isWallInFront && !_machine.isGrounded);
		
		animController.PlayJumpAnimation();
		_jumpTimer = 0;
	}

	public override void UpdateState()
	{
		TryTransition(_machine);
	}

	public override void FixedUpdateState()
	{
		var rb = _machine.rb;
		var animController =  _machine.animationController;
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		var direction = _machine.cameraRig.forward;
		direction.y = 0;
		var targetRotation = Quaternion.LookRotation(direction);
		var yAngle = Vector2.SignedAngle(input, Vector2.up);
		targetRotation *= Quaternion.Euler(0, yAngle, 0);
		_machine.rb.MoveRotation(Quaternion.Slerp(_machine.rb.transform.rotation, targetRotation, 6 * Time.deltaTime));
		if (_jumpExecuted && input.sqrMagnitude > 0.1f)
			rb.AddForce(direction * 0.2f, ForceMode.Force);
		if (_jumpExecuted)
		{
			var isFalling = _machine.velocity.y <= -0.5f && _machine.groundHitInfo.distance > 0.46f;
			animController.NotifyFalling(isFalling);
		}
		if (_jumpExecuted) return;
		_jumpTimer += Time.fixedDeltaTime;
		if (_jumpTimer < _jumpDelay) return;
		_jumpExecuted = true;
		rb.AddForce(_jumpForce * Vector3.up + _jumpForce * 0.3f * rb.transform.forward, ForceMode.Impulse);
	}

	public override void ExitState()
	{
		_jumpTimer = 0;
		_jumpExecuted = false;
	}
}

public class FallingState : StateBase<MovementController>
{
	private readonly MovementController _machine;

	public FallingState(MovementController stateMachine) : base(stateMachine)
	{
		_machine = stateMachine;
	}

	public override void EnterState()
	{
		Debug.Log("Entering Fall State");
		var animController =  _machine.animationController;
		
		AddTransition(_machine.LastState is WallClimbState or JumpState ? new IdleState(_machine) : _machine.LastState, controller => controller.groundHitInfo.distance < 0.4f);
		
		animController.PlayFallingAnimation();
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
		var animController =  _machine.animationController;
		animController.PlayLandingAnimation();
	}
}
