using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;


public class MovementController: MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform mesh;
	[SerializeField] private Transform cameraRig;

	[Space(10)]

	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private AnimationCurve _stateSpeedCurve;
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
		MoveWithSpeed(walkSpeed, 1.3f * dirInput, acceleration);
	}

    private void MoveWithSpeed(float speed, Vector2 input, float acceleration)
	{
        var canMove = input.sqrMagnitude != 0;

		if (isClimbing)
			return;
		
		var targetSpeed = _stateSpeedCurve.Evaluate(input.sqrMagnitude) * speed;
		currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, targetSpeed, acceleration * Time.deltaTime);
		if (canMove) {
			//isMoving = true;
			targetVelocity = speedMultiplier * currentForwardSpeed * transform.forward;
			targetVelocity.y = _rb.linearVelocity.y;
			_rb.linearVelocity = targetVelocity;
		}
        _animationController.ChangeMoveState(speedMultiplier * currentForwardSpeed / speed);

		// Rotate towards camera forward direction when moving
		if (input.y == 0) return;
		
		Vector3 direction = cameraRig.transform.forward;
		direction.y = 0;
		Quaternion targetRotation = Quaternion.LookRotation(direction);
		float yAngle = Mathf.Atan2(input.x, input.y);
		targetRotation *= Quaternion.Euler(0, Mathf.Rad2Deg * yAngle, 0);

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
		if(isClimbing)
			StopClimbing();
		var jumpDir = _isGrounded ? _groundHitInfo.normal : Vector3.up;
		_rb.AddForce(jumpForce * jumpDir, ForceMode.Impulse);
		StartCoroutine(TryWallClimb());
    }

    private IEnumerator TryWallClimb(float delay = 0.1f)
	{
		yield return new WaitForSeconds(delay);
		while (!_isGrounded)
		{
			if (isWallInFront)
			{
				if(_climbCoroutine != null)
					StopCoroutine(_climbCoroutine);
				_climbCoroutine = StartCoroutine(WallClimb(climbTimeout));
				break;
			}

            yield return null;
		}
    }

	private IEnumerator WallClimb(float time)
	{
		float timer = 0;
		// Rotate mesh(not this rb) parallel to surface
		
		var normal = _wallHitinfo.normal;
		print("climb start");
		isClimbing = true;
		var climbAngle = Vector3.SignedAngle(transform.forward, -normal, Vector3.up);
		var forwardOnWall = Quaternion.AngleAxis(climbAngle, -normal) * Vector3.Cross(transform.right, normal);
		
		var dir = (transform.position - _wallHitinfo.point).normalized;
		_rb.AddForce(Vector3.up, ForceMode.Impulse);
		var pRotation = transform.rotation;
		var isWall = true;
        _animationController.PlayWalkRunAnimation(1.5f);
        while (timer < time && isWall)
		{
			timer += Time.fixedDeltaTime;
			
			isWall = Physics.Raycast(_origin, -dir, _collider.bounds.extents.z + 0.1f, ~_layerMask);
			forwardOnWall = Quaternion.AngleAxis(Mathf.Atan2(input.x, input.y) * 0.05f * Mathf.Rad2Deg, transform.up) * forwardOnWall;
			var targetRotation = Quaternion.LookRotation(forwardOnWall, normal);
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
			
			// Move Upward
			_rb.linearVelocity = climbForce * Mathf.Sqrt(maxJumpHeight * 2f * 10f) * transform.forward - 0.2f * transform.up;
			yield return new WaitForFixedUpdate();
		}
		
		_rb.AddForce(0.5f * Vector3.forward, ForceMode.Impulse);
		transform.DORotateQuaternion(pRotation, 0.2f);
		
        isClimbing = false;
        print("climb end");
        _animationController.PlayWalkRunAnimation(1f);
    }

	private void StopClimbing()
	{
		if(_climbCoroutine != null)
			StopCoroutine(_climbCoroutine);
		_rb.AddForce(0.8f * transform.up, ForceMode.Impulse);
		isClimbing = false;
		StandOnGround();
	}

	private void StandOnGround()
	{
		var isGround = Physics.Raycast(_origin, Vector3.down, out var groundInfo, _collider.bounds.extents.y + 25f, ~_layerMask);
		var forward = transform.forward;
		forward.y = 0;
		if(isGround)
			print($"stop {groundInfo.collider.name}");
		var r = Quaternion.LookRotation(forward, groundInfo.normal);
		transform.rotation = r;
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