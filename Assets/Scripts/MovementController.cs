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
    private readonly float NORMAL_SPEED = 0.7f;
	private readonly float ACCELERATION = 4f;
	private readonly float SPEED_MULTIPLIER = 1f;
	private readonly float MAX_JUMP_HEIGHT = 0.4f;
	private readonly float CLIMB_TIMEOUT = 0.5f;
	private readonly float CLIMB_FORCE = 0.6f;
	private readonly float TURN_ACC = 10f;

	private Vector3 targetVelocity;

	public bool isMoving = false;
	public bool isClimbing = false;
	private bool _isGrounded;
	private bool isWallInFront;

    private float currentForwardSpeed = 0f;

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
        TouchInputManager.InputMain.jump.performed += ctx => DoJump(ctx);
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
    }

    private void WalkAndRun(float inputY)
	{
		MoveWihtSpeed(NORMAL_SPEED, inputY, ACCELERATION);
	}

    private void MoveWihtSpeed(float speed, float threshold, float acceleration)
	{
        bool canMove = threshold != 0;

		if (isClimbing)
			return;

        currentForwardSpeed = Mathf.Lerp(currentForwardSpeed, threshold * speed, acceleration * Time.deltaTime);
		_animationController.ChangeMoveState(currentForwardSpeed / speed);
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
	    
	private void DoJump(InputAction.CallbackContext _)
	{
		//Jump only when grounded
		if (!_isGrounded)
			return;
		_rb.velocity = Mathf.Sqrt(MAX_JUMP_HEIGHT * 16f) * Vector3.up;

		StartCoroutine(TryWallClimb());
    }

	private IEnumerator TryWallClimb(float delay = 0.1f)
	{
		yield return new WaitForSeconds(delay);
		while (!_isGrounded)
		{
			print("check");
			if (isWallInFront)
			{
				yield return StartCoroutine(WallClimb(CLIMB_TIMEOUT));
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
        print("climb start");
		isClimbing = true;

        _animationController.PlayWalkRunAnimation(1.5f);
        while (timer < time && isWallInFront)
		{
			timer += Time.deltaTime;
			if (_isGrounded)
				break;

			// Move Upward
			_rb.velocity = CLIMB_FORCE * Mathf.Sqrt(MAX_JUMP_HEIGHT * 2f * 8f) * Vector3.up;

			yield return null;

		}
		isClimbing = false;
        print("climb end");
		_rb.AddForce((transform.forward + transform.up).normalized * 1, ForceMode.Impulse);
        ratMesh.DOLocalRotate(lastRot, 0.3f);
        _animationController.PlayWalkRunAnimation(1f);

    }

}
