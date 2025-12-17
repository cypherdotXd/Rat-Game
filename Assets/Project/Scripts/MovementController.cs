using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using MIRA;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

public class MovementController : StateMachineBehaviour<MovementController>
{
	[Header("References")]
	[SerializeField] private Transform mesh;
	[SerializeField] private CameraMotion cameraMotion;

	[Space(10)]

	[SerializeField] private LayerMask _layerMask;
	public AnimationCurve inputSpeedCurve;
	[SerializeField] private AnimationCurve _inputStateCurve;
	public Transform cameraRig;
    public float walkSpeed = 0.7f;
	public float acceleration = 4f;
	public Rigidbody rb;
	[SerializeField] float speedMultiplier = 1f;
	[SerializeField] float maxJumpHeight = 0.4f;
	[SerializeField] float jumpForce = 0.4f;
	[SerializeField] float climbTimeout = 0.5f;
	[SerializeField] float climbForce = 0.6f;
	[SerializeField] float turnAcceleration = 10f;

	public StateController animationController;
	public bool isClimbingWall;
	public bool isClimbingPath;
	private bool _isGrounded;
	private bool isWallInFront;
    private float currentForwardSpeed;
    private Vector3 _origin;
	private Vector3 targetVelocity;
	private Vector3 input;
	private PathClimb _pathClimb;
	private CapsuleCollider _collider;
	private RaycastHit _groundHitInfo;
	private RaycastHit _wallHitinfo;
	private Coroutine _climbCoroutine;

	private void Awake()
	{
		Application.targetFrameRate = 60;
		_collider = GetComponent<CapsuleCollider>();
		// rb = GetComponent<Rigidbody>();
		animationController = GetComponent<StateController>();
	}

	private void OnEnable()
	{
        InputSystem.actions.FindAction("jump").performed += DoJump;
        // CurrentState.OnUpdateState += (state) =>
        // {
	       //  // var h = _rb.linearVelocity.magnitude / (speed * speedMultiplier * curve.Evaluate(1));
	       //  var h = MoveContextData.Direction.magnitude;
	       //  animationController.ChangeMoveState(h);
        // };
	}

	private void OnDisable()
	{
		InputSystem.actions.FindAction("jump").performed -= DoJump;
	}

	// Start is called once only 
	private void Start()
	{
		// State : IDLE, WALK, SPRINT, WALL CLIMB
		var idleState = new IdleState(this);
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
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, out _groundHitInfo, 
	        _collider.bounds.extents.y + 0.01f, ~_layerMask);
        isWallInFront = Physics.Raycast(_collider.bounds.center - 0.5f * _collider.bounds.extents.y * Vector3.up,
	        transform.forward, out _wallHitinfo, _collider.bounds.extents.z + 0.05f, ~_layerMask, QueryTriggerInteraction.Collide);
        
        _origin = _collider.bounds.center - _collider.bounds.extents.y * Vector3.up;
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

    private void MoveWithSpeed(float speed, Vector2 dirInput, float acceleration)
	{
        var canMove = dirInput.sqrMagnitude != 0;

	    // print(isClimbingPath);
        if (isClimbingPath)
        {
	        ClimbPath();
	        return;
        }
        
		if (isClimbingWall)
			return;
		
		var targetSpeed = inputSpeedCurve.Evaluate(dirInput.sqrMagnitude) * speed;
		currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeed, acceleration * Time.deltaTime);
		if (canMove) {
			//isMoving = true;
			targetVelocity = speedMultiplier * currentForwardSpeed * transform.forward;
			targetVelocity.y = rb.linearVelocity.y;
			rb.linearVelocity = targetVelocity;
		}

		var h = rb.linearVelocity.magnitude / (speed * speedMultiplier * inputSpeedCurve.Evaluate(1));
		var animState = _inputStateCurve.Evaluate(h);
        animationController.ChangeMoveState(animState);

		// Rotate towards camera forward direction when moving
		if (dirInput.sqrMagnitude == 0) return;
		
		Vector3 direction = cameraRig.forward;
		direction.y = 0;
		var targetRotation = Quaternion.LookRotation(direction);
		var yAngle = Vector2.SignedAngle(dirInput, Vector2.up);
		targetRotation *= Quaternion.Euler(0, yAngle, 0);

		rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnAcceleration * Time.deltaTime));
	}

    private void DoJump(InputAction.CallbackContext _)
	{
		//Jump only when grounded
		if (!_isGrounded) return;   
		StartCoroutine(Jump(0));
    }

	IEnumerator Jump(float delay = 0.1f)
	{
		yield return new WaitForSeconds(delay);
		if (isClimbingWall)
		{
			StopClimbing();
			rb.AddForce(0.6f * transform.up, ForceMode.Impulse);
			yield break;
		}
		
		var jumpDir = _isGrounded ? _groundHitInfo.normal : Vector3.up;
		rb.AddForce(jumpForce * jumpDir, ForceMode.Impulse);
		StartCoroutine(TryWallClimb(0.6f));
    }

    private IEnumerator TryWallClimb(float checkTime = 0.1f, float delay = 0.1f)
	{
		yield return new WaitForSeconds(delay);
		while (checkTime > 0)
		{
			if (!_isGrounded && isWallInFront) break;
		    checkTime -= Time.fixedDeltaTime;
		    yield return new WaitForFixedUpdate();
		}
		var limit = Vector3.Dot(_wallHitinfo.normal, -transform.forward);
		if(limit < 0.7f)
			yield break;
		if(_climbCoroutine != null) StopCoroutine(_climbCoroutine);
		_climbCoroutine = StartCoroutine(WallClimb(climbTimeout));
    }

	private IEnumerator WallClimb(float time)
	{
		isClimbingWall = true;
		
		var normal = _wallHitinfo.normal;
		var climbAngle = Vector3.SignedAngle(transform.forward, -normal, Vector3.up);
		var lateralDirection = Quaternion.AngleAxis(climbAngle, -normal) * Vector3.Cross(transform.right, normal);
		
		Debug.DrawRay(_wallHitinfo.point, transform.forward, Color.red, 6);
		var wallDir = (transform.position - _wallHitinfo.point).normalized;
        animationController.PlayWalkRunAnimation(1.5f);
		
		var isWall = true;
		float timer = 0;
		cameraMotion.StartLookFollow();
        while (timer < time && isWall)
        {
	        if (isWallInFront)
	        {
		        var diff = Vector3.Dot(_wallHitinfo.normal, normal);
		        if (diff < 0.9f)
		        {
			        StartCoroutine(WallClimb(climbTimeout));
			        yield break;
		        }
	        }
			timer += Time.fixedDeltaTime;
			
			isWall = Physics.Raycast(_origin, -wallDir, _collider.bounds.extents.z + 0.1f, ~_layerMask);
			
			lateralDirection = Quaternion.AngleAxis(Mathf.Atan2(input.x, 1) * 0.08f * Mathf.Rad2Deg, transform.up) * lateralDirection;
			var targetRotation = Quaternion.LookRotation(lateralDirection, normal);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
			
			// Move Upward
			var climbDir = (-Vector3.Dot(Vector3.up, transform.forward) + 1f)/2f;
			climbDir = Mathf.Clamp(climbDir, 0.8f, 1f);
			rb.linearVelocity = climbForce * climbDir * transform.forward;
			
			yield return new WaitForFixedUpdate();
		}

		StopClimbing();
        isClimbingWall = false;
        animationController.PlayWalkRunAnimation(1f);
    }

	private void StopClimbing()
	{
		if(_climbCoroutine != null)
			StopCoroutine(_climbCoroutine);
		
		rb.isKinematic = false;
		cameraMotion.EndLookFollow();
		isClimbingWall = false;
		var forward = cameraRig.forward;
		forward.y = 0;
		rb.linearVelocity = 0.5f * rb.linearVelocity.magnitude * transform.forward;
		StandOnGround(forward);
	}

	private void StandOnGround(Vector3? forwardDir = null)
	{
		var isGround = Physics.Raycast(_origin, Vector3.down, out var groundInfo, _collider.bounds.extents.y + 25f, ~_layerMask);
		var forward = forwardDir ?? transform.forward;
		forward.y = 0;
		var r = Quaternion.LookRotation(forward.normalized, isGround ? groundInfo.normal : Vector3.up);
		transform.DORotateQuaternion(r, 0.2f);
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
		_context.rb.isKinematic = false;
		Debug.Log("Entering MoveState");
	}

	public override void UpdateState()
	{
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		var targetSpeed = _context.inputSpeedCurve.Evaluate(input.sqrMagnitude) * _context.walkSpeed;
		var lookDirection = _context.cameraRig.forward;
		
		if(input.sqrMagnitude < 0.01f)
			_context.SwitchState(new IdleState(_context));
		
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
		
	}
}

public class WallClimbState : StateBase<MovementController>
{
	public WallClimbState(MovementController context) : base(context)
	{
	}

	public override void EnterState()
	{
		throw new NotImplementedException();
	}

	public override void UpdateState()
	{
		throw new NotImplementedException();
	}

	public override void FixedUpdateState()
	{
		throw new NotImplementedException();
	}

	public override void ExitState()
	{
		throw new NotImplementedException();
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
	}

	public override void UpdateState()
	{
		var rb = _context.rb;
		// rb.linearVelocity = Vector3.zero;
		var input = TouchInputManager.InputMain.move.ReadValue<Vector2>();

		if(input.sqrMagnitude > 0.01f)
			_context.SwitchState(new MoveState(_context));
	}

	public override void FixedUpdateState()
	{
		
	}

	public override void ExitState()
	{
	}
}