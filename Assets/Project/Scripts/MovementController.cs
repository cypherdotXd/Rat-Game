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
        MoveWithSpeed(walkSpeed, input, acceleration);
	}

	private void FixedUpdate()
    {
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, out _groundHitInfo, 
	        _collider.bounds.extents.y + 0.01f, ~_layerMask);
        isWallInFront = Physics.Raycast(_collider.bounds.center - _collider.bounds.extents.y * Vector3.up,
	        transform.forward, out _wallHitinfo, _collider.bounds.extents.z + 0.05f, ~_layerMask);
        
        _origin = _collider.bounds.center - _collider.bounds.extents.y * Vector3.up;
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

		var h = _rb.linearVelocity.magnitude / (speed * speedMultiplier * _inputSpeedCurve.Evaluate(1));
		var animState = _inputStateCurve.Evaluate(h);
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
		if(x < 0.5f)
			yield break;
		if(_climbCoroutine != null) StopCoroutine(_climbCoroutine);
		_climbCoroutine = StartCoroutine(WallClimb(climbTimeout));
    }

	private IEnumerator WallClimb(float time)
	{
		isClimbing = true;
		
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
			
			lateralDirection = Quaternion.AngleAxis(Mathf.Atan2(input.x, input.y) * 0.08f * Mathf.Rad2Deg, transform.up) * lateralDirection;
			var targetRotation = Quaternion.LookRotation(lateralDirection, normal);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
			
			// Move Upward
			var climbDir = (-Vector3.Dot(Vector3.up, transform.forward) + 1f)/2f;
			print(climbDir);
			climbDir = Mathf.Clamp(climbDir, 0.8f, 1f);
			_rb.linearVelocity = climbForce * climbDir * transform.forward;
			
			yield return new WaitForFixedUpdate();
		}

		StopClimbing();
        isClimbing = false;
        _animationController.PlayWalkRunAnimation(1f);
    }

	private void StopClimbing()
	{
		if(_climbCoroutine != null)
			StopCoroutine(_climbCoroutine);
		_rb.linearVelocity = 0.5f * _rb.linearVelocity.magnitude * transform.forward;
		
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
	

}