using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using UnityEngine.Serialization;


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

	public bool isClimbing;
	private bool _sprintToggle = false;
	private bool _isGrounded;
	private bool isWallInFront;
    private float currentForwardSpeed;
    private Vector3 _origin;
	private Vector3 targetVelocity;
	private Vector3 input;

	private Rigidbody _rb;
	private CapsuleCollider _collider;
	private StateController _animationController;
	private RaycastHit _groundHitInfo;
	private static RaycastHit _wallHitinfo;
	private Coroutine _climbCoroutine;

	private void OnEnable()
	{
        TouchInputManager.InputMain.jump.performed += DoJump;
	}

	private void OnDisable()
	{
		TouchInputManager.InputMain.jump.performed -= DoJump;
	}

	// Start is called once only 
	private void Start()
	{
		Application.targetFrameRate = 60;
		_collider = GetComponent<CapsuleCollider>();
		_rb = GetComponent<Rigidbody>();
		_animationController = GetComponent<StateController>();
		
	}

    // Update is called once per frame
    private void Update()
	{
        input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		WalkAndRun(input);
	}

	private void FixedUpdate()
    {
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, out _groundHitInfo, 
	        _collider.bounds.extents.y + 0.01f, ~_layerMask);
        isWallInFront = Physics.Raycast(_collider.bounds.center - _collider.bounds.extents.y * Vector3.up,
	        transform.forward, out _wallHitinfo, _collider.bounds.extents.z + 0.05f, ~_layerMask);
        
        _origin = _collider.bounds.center - _collider.bounds.extents.y * Vector3.up;
    }

    private void WalkAndRun(Vector2 dirInput)
	{
		MoveWithSpeed(walkSpeed, dirInput, acceleration);
	}

    private void MoveWithSpeed(float speed, Vector2 dirInput, float acceleration)
	{
        var canMove = dirInput.sqrMagnitude != 0;

		if (isClimbing)
			return;
		
		var targetSpeed = _inputSpeedCurve.Evaluate(dirInput.sqrMagnitude) * speed;
		currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeed, acceleration * Time.deltaTime);
		if (canMove) {
			//isMoving = true;
			targetVelocity = speedMultiplier * currentForwardSpeed * transform.forward;
			targetVelocity.y = _rb.linearVelocity.y;
			_rb.linearVelocity = targetVelocity;
		}
		var animState = _inputStateCurve.Evaluate(dirInput.sqrMagnitude);
        _animationController.ChangeMoveState(animState);

		// Rotate towards camera forward direction when moving
		if (dirInput.sqrMagnitude == 0) return;
		
		Vector3 direction = cameraRig.transform.forward;
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
		StartCoroutine(JumpDelayed());
    }

	IEnumerator JumpDelayed(float delay = 0.1f)
	{
		
		yield return new WaitForSeconds(delay);
		if (isClimbing)
		{
			StopClimbing();
			_rb.AddForce(2.2f * transform.up, ForceMode.Impulse);
			yield break;
		}
		
		var jumpDir = _isGrounded ? _groundHitInfo.normal : Vector3.up;
		_rb.AddForce(jumpForce * jumpDir, ForceMode.Impulse);
		StartCoroutine(TryWallClimb());
    }

    private IEnumerator TryWallClimb(float delay = 0.1f)
	{
		while (_isGrounded || !isWallInFront)
		    yield return new WaitForFixedUpdate();
		var x = Vector3.Dot(_wallHitinfo.normal, -transform.forward);
		print($"is wall {x}");
		if(x < 0.5f)
			yield break;
		if(_climbCoroutine != null) StopCoroutine(_climbCoroutine);
		_climbCoroutine = StartCoroutine(WallClimb(climbTimeout));
    }

	private IEnumerator WallClimb(float time)
	{
		isClimbing = true;
		
		print("climb start");
		var normal = _wallHitinfo.normal;
		var climbAngle = Vector3.SignedAngle(transform.forward, -normal, Vector3.up);
		var forwardOnWall = Quaternion.AngleAxis(climbAngle, -normal) * Vector3.Cross(transform.right, normal);
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
			        print("Wall changed");
			        StartCoroutine(WallClimb(climbTimeout));
			        yield break;
		        }
	        }
			timer += Time.fixedDeltaTime;
			
			isWall = Physics.Raycast(_origin, -wallDir, _collider.bounds.extents.z + 0.1f, ~_layerMask);
			forwardOnWall = Quaternion.AngleAxis(Mathf.Atan2(input.x, input.y) * 0.08f * Mathf.Rad2Deg, transform.up) * forwardOnWall;
			var targetRotation = Quaternion.LookRotation(forwardOnWall, normal);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
			
			// Move Upward
			var climbDir = (Vector3.Dot(transform.forward, Vector3.up) + 1f)/2f;
			climbDir = Mathf.Clamp(climbDir, 0.6f, 1f);
			_rb.linearVelocity = climbForce * climbDir * transform.forward;
			
			yield return new WaitForFixedUpdate();
		}

		StopClimbing();
        isClimbing = false;
        print("climb end");
        _animationController.PlayWalkRunAnimation(1f);
    }

	private void StopClimbing()
	{
		if(_climbCoroutine != null)
			StopCoroutine(_climbCoroutine);
		_rb.linearVelocity = 0.5f * _rb.linearVelocity.magnitude * transform.forward;
		
		print("Stop");
		cameraMotion.EndLookFollow();
		isClimbing = false;
		StandOnGround();
	}

	private void StandOnGround()
	{
		var isGround = Physics.Raycast(_origin, Vector3.down, out var groundInfo, _collider.bounds.extents.y + 25f, ~_layerMask);
		var forward = transform.forward;
		forward.y = 0;
		var r = Quaternion.LookRotation(forward.normalized, isGround ? groundInfo.normal : Vector3.up);
		transform.DORotateQuaternion(r, 0.2f);
	}

	private IEnumerator WallClimb1(float time)
	{
		float timer = 0;
		
		// Rotate mesh(not this rb) parallel to surface
		Vector3 normal = _wallHitinfo.normal;
		float angle = Vector3.SignedAngle(transform.up, normal, transform.right);
		Vector3 lastRot = mesh.localEulerAngles;
		// mesh.DOLocalRotate(angle * Vector3.right, 0.2f);
		// mesh.DOLocalMoveZ(0.08f, 0.2f);
		print("climb start");
		isClimbing = true;
		Vector3 forward = Quaternion.AngleAxis(angle, transform.right) * transform.forward;
		// Vector3 forward = Vector3.Cross(normal, transform.right).normalized;
		Debug.DrawRay(_collider.bounds.center, forward * 0.2f, Color.green, 10);
		transform.DORotateQuaternion(Quaternion.LookRotation(forward), 0.1f);

        _animationController.PlayWalkRunAnimation(1.5f);
        while (timer < time && isWallInFront)
		{
			timer += Time.deltaTime;
			if (_isGrounded)
				break;
			// Move Upward
			_rb.linearVelocity = climbForce * Mathf.Sqrt(maxJumpHeight * 2f * 8f) * Vector3.up;
			yield return null;

		}
		if(time <= timer)
			_rb.AddForce((-transform.forward + transform.up).normalized * 2, ForceMode.Impulse);
		else
			_rb.AddForce((transform.forward + transform.up).normalized * 1, ForceMode.Impulse);
		var right = transform.right;
		right.y = 0;
		forward = Vector3.Cross(_isGrounded ? _groundHitInfo.normal : Vector3.up, right).normalized;
		transform.DORotateQuaternion(Quaternion.LookRotation(-forward), 0.1f);
		
        isClimbing = false;
        print("climb end");
        mesh.DOLocalRotate(lastRot, 0.3f);
        mesh.DOLocalMoveZ(0f, 0.2f);
        _animationController.PlayWalkRunAnimation(1f);

    }

}