using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.Serialization;
using UnityEngine.Splines;


public class MovementController: MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform mesh;
	[SerializeField] private Transform cameraRig;
	[SerializeField] private CameraMotion cameraMotion;

	[Space(10)]

	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private AnimationCurve _inputSpeedCurve;
	[SerializeField] private AnimationCurve _inputStateCurve;
    [SerializeField] float walkSpeed = 0.7f;
	[SerializeField] float acceleration = 4f;
	[SerializeField] float speedMultiplier = 1f;
	[SerializeField] float maxJumpHeight = 0.4f;
	[SerializeField] float jumpForce = 0.4f;
	[SerializeField] float climbTimeout = 0.5f;
	[SerializeField] float climbForce = 0.6f;
	[SerializeField] float turnAcceleration = 10f;

	public bool isClimbingWall;
	public bool isClimbingPath;
	private bool _isGrounded;
	private bool isWallInFront;
    private float currentForwardSpeed;
    private Vector3 _origin;
	private Vector3 targetVelocity;
	private Vector3 input;
	private PathClimb _pathClimb;
	private Rigidbody _rb;
	private CapsuleCollider _collider;
	private StateController _animationController;
	private RaycastHit _groundHitInfo;
	private static RaycastHit _wallHitinfo;
	private Coroutine _climbCoroutine;

	private void Awake()
	{
		Application.targetFrameRate = 60;
		_collider = GetComponent<CapsuleCollider>();
		_rb = GetComponent<Rigidbody>();
		_animationController = GetComponent<StateController>();
	}

	private void OnEnable()
	{
        InputSystem.actions.FindAction("jump").performed += DoJump;
	}

	private void OnDisable()
	{
		InputSystem.actions.FindAction("jump").performed -= DoJump;
	}

	// Start is called once only 
	private void Start()
	{
		
	}

    // Update is called once per frame
    private void Update()
    {
        input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
        
        MoveWithSpeed(walkSpeed, input, acceleration);
	}

	private void FixedUpdate()
    {
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, out _groundHitInfo, 
	        _collider.bounds.extents.y + 0.01f, ~_layerMask);
        isWallInFront = Physics.Raycast(_collider.bounds.center - 0.5f * _collider.bounds.extents.y * Vector3.up,
	        transform.forward, out _wallHitinfo, _collider.bounds.extents.z + 0.05f, ~_layerMask, QueryTriggerInteraction.Collide);

        
        _origin = _collider.bounds.center - _collider.bounds.extents.y * Vector3.up;
    }

	public void ClimbPath(PathClimb pathClimb)
	{
		print("Climb");
		_pathClimb = pathClimb;
		isClimbingPath = true;
	}

    private void MoveWithSpeed(float speed, Vector2 dirInput, float acceleration)
	{
        var canMove = dirInput.sqrMagnitude != 0;

	    // print(isClimbingPath);
        if (isClimbingPath)
        {
	        print("climbing Start");
	        isClimbingPath = _pathClimb.ClimbIncrementally(transform, 0.1f * Time.deltaTime * input.y);
	        _rb.isKinematic = isClimbingPath;
	        _animationController.ChangeMoveState(input.y);
	        return;
        }
        
		if (isClimbingWall)
			return;
		
		var targetSpeed = _inputSpeedCurve.Evaluate(dirInput.sqrMagnitude) * speed;
		currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeed, acceleration * Time.deltaTime);
		if (canMove) {
			//isMoving = true;
			targetVelocity = speedMultiplier * currentForwardSpeed * transform.forward;
			targetVelocity.y = _rb.linearVelocity.y;
			_rb.linearVelocity = targetVelocity;
		}

		var h = _rb.linearVelocity.magnitude / (speed * speedMultiplier * _inputSpeedCurve.Evaluate(1));
		var animState = _inputStateCurve.Evaluate(h);
        _animationController.ChangeMoveState(animState);

		// Rotate towards camera forward direction when moving
		if (dirInput.sqrMagnitude == 0) return;
		
		Vector3 direction = cameraRig.forward;
		direction.y = 0;
		var targetRotation = Quaternion.LookRotation(direction);
		var yAngle = Vector2.SignedAngle(dirInput, Vector2.up);
		targetRotation *= Quaternion.Euler(0, yAngle, 0);

		_rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnAcceleration * Time.deltaTime));
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
			_rb.AddForce(0.6f * transform.up, ForceMode.Impulse);
			yield break;
		}
		
		var jumpDir = _isGrounded ? _groundHitInfo.normal : Vector3.up;
		_rb.AddForce(jumpForce * jumpDir, ForceMode.Impulse);
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
        _animationController.PlayWalkRunAnimation(1.5f);
		
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
			_rb.linearVelocity = climbForce * climbDir * transform.forward;
			
			yield return new WaitForFixedUpdate();
		}

		StopClimbing();
        isClimbingWall = false;
        _animationController.PlayWalkRunAnimation(1f);
    }

	private void StopClimbing()
	{
		if(_climbCoroutine != null)
			StopCoroutine(_climbCoroutine);
		
		_rb.isKinematic = false;
		cameraMotion.EndLookFollow();
		isClimbingWall = false;
		var forward = cameraRig.forward;
		forward.y = 0;
		_rb.linearVelocity = 0.5f * _rb.linearVelocity.magnitude * transform.forward;
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