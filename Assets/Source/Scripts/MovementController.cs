using System.Collections;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;


public class MovementController: MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform ratMesh;
	[SerializeField] private Transform cameraRig;

	[Space(10)]

	[SerializeField] private LayerMask _layerMask;
    [SerializeField] float walkSpeed = 0.7f;
	[SerializeField] float acceleration = 4f;
	[SerializeField] float speedMultiplier = 1f;
	[SerializeField] float maxJumpHeight = 0.4f;
	[SerializeField] float climbTimeout = 0.5f;
	[SerializeField] float climbForce = 0.6f;
	[SerializeField] float turnAcceleration = 10f;

	private Vector3 targetVelocity;

	public bool isMoving = false;
	public bool isClimbing = false;
	private bool _sprintToggle = false;
	private bool _isGrounded;
	private bool isWallInFront;

    private float currentForwardSpeed = 0f;

	private Rigidbody _rb;
	private CapsuleCollider _collider;
	private StateController _animationController;

	// Start is called once only 
	private void Start()
	{
		Application.targetFrameRate = 60;
		_collider = GetComponent<CapsuleCollider>();
		_rb = GetComponent<Rigidbody>();
		_animationController = GetComponent<StateController>();

        TouchInputManager.InputMain.jump.performed += ctx => DoJump(ctx);
		TouchInputManager.InputMain.sprint.performed += ctx => ToggleSprint(ctx);
    }

    // Update is called once per frame
    void Update()
	{

        Vector2 input = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		WalkAndRun(input);

	}

	private static RaycastHit wallHitinfo = new();
	private void FixedUpdate()
    {
        _isGrounded = Physics.Raycast(_collider.bounds.center, -transform.up, _collider.bounds.extents.y + 0.01f, ~_layerMask);
        isWallInFront = Physics.Raycast(_collider.bounds.center - _collider.bounds.extents.y * Vector3.up, transform.forward, out wallHitinfo, _collider.bounds.extents.z + 0.05f, ~_layerMask);
		if(isWallInFront)
			Debug.DrawRay(_collider.bounds.center - _collider.bounds.extents.y * Vector3.up, transform.forward, Color.cyan, 6);
    }

    private void WalkAndRun(Vector2 input)
	{
		
		MoveWihtSpeed(walkSpeed, input, acceleration);
	}

    private void MoveWihtSpeed(float speed, Vector2 input, float acceleration)
	{
        bool canMove = input.sqrMagnitude != 0;

		if (isClimbing)
			return;


		currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, input.sqrMagnitude * speed, acceleration * Time.deltaTime);
		if (canMove) {
			//isMoving = true;
			targetVelocity = speedMultiplier * currentForwardSpeed * transform.forward;
			targetVelocity.y = _rb.velocity.y;
			_rb.velocity = targetVelocity;
		}
        _animationController.ChangeMoveState(speedMultiplier * currentForwardSpeed / speed);

		// Rotate towards camera forward direction when moving
		if(input.y != 0)
		{
			Vector3 direction = cameraRig.transform.forward;
			direction.y = 0;
			Quaternion targetRotation = Quaternion.LookRotation(direction);
			float yAngle = Mathf.Atan2(input.x, input.y);
			targetRotation *= Quaternion.Euler(0, Mathf.Rad2Deg * yAngle, 0);

            _rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, turnAcceleration * Time.deltaTime));
		}
	}

    void ToggleSprint(InputAction.CallbackContext _)
	{
		_sprintToggle = !_sprintToggle;
		speedMultiplier = _sprintToggle ? 1.3f : 1;
    }


    private void DoJump(InputAction.CallbackContext _)
	{
		//Jump only when grounded
		if (!_isGrounded)
			return;
		if (_animationController.PlayJumpAnimation())
		{
			StartCoroutine(JumpDelayed(0.1f));
		}   
    }

	IEnumerator JumpDelayed(float delay = 0.1f)
	{
		yield return new WaitForSeconds(delay);
		_rb.velocity = Mathf.Sqrt(maxJumpHeight * 16f) * Vector3.up;
        StartCoroutine(TryWallClimb());
    }

    private IEnumerator TryWallClimb(float delay = 0.1f)
	{
		yield return new WaitForSeconds(delay);
		while (!_isGrounded)
		{
			if (isWallInFront)
			{
				yield return StartCoroutine(WallClimb(climbTimeout));
				print(wallHitinfo.distance);
				break;
			}

            yield return null;
		}
    }

	private IEnumerator WallClimb(float time)
	{
		float timer = 0;
		
		// Rotate mesh(not this rb) parallel to surface
		Vector3 normal = wallHitinfo.normal;
		float angle = Vector3.SignedAngle(transform.up, normal, transform.right);
		Vector3 lastRot = ratMesh.localEulerAngles;
		ratMesh.DOLocalRotate(angle * Vector3.right, 0.2f);
		ratMesh.DOLocalMoveZ(0.08f, 0.2f);
		print("climb start");
		isClimbing = true;

        _animationController.PlayWalkRunAnimation(1.5f);
        while (timer < time && isWallInFront)
		{
			timer += Time.deltaTime;
			if (_isGrounded)
				break;

			// Move Upward
			_rb.velocity = climbForce * Mathf.Sqrt(maxJumpHeight * 2f * 8f) * Vector3.up;
			transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-wallHitinfo.normal), 12 * Time.deltaTime);
			yield return null;

		}
		if(time <= timer)
			_rb.AddForce((-transform.forward + transform.up).normalized * 2, ForceMode.Impulse);
		else
			_rb.AddForce((transform.forward + transform.up).normalized * 1, ForceMode.Impulse);

        isClimbing = false;
        print("climb end");
        ratMesh.DOLocalRotate(lastRot, 0.3f);
        ratMesh.DOLocalMoveZ(0f, 0.2f);
        _animationController.PlayWalkRunAnimation(1f);

    }

}
