using System;
using UnityEngine;
using UnityEngine.InputSystem;
using MIRA;

// This will work as a central manager, dependencies provider.
public class MovementController : StateMachine<MovementController>
{
	[Header("Deps")]
	[SerializeField] private Transform _mesh;
	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private AnimationCurve _inputSpeedCurve;
	[SerializeField] private CameraMotionBasic _cameraMotion;
	[SerializeField] private Transform _cameraRig;
	[SerializeField] private Rigidbody _rb;
	[SerializeField] private CapsuleCollider _playerCollider;
	[SerializeField] private StateAnimationSyncer _animationSyncer;

	[Header("Movement Settings")]
	[SerializeField] private float _walkSpeed = 0.7f;
	[SerializeField] private float _acceleration = 4f;
	[SerializeField] private float _speedMultiplier = 1f;
	[SerializeField] private float _rotationSpeed = 8f;
	[SerializeField] private float _jumpForce = 3.5f;
	[SerializeField] private float _climbTimeout = 50f;
	[SerializeField] private float _climbForce = 1.5f;

	public Transform Mesh => _mesh;
	public LayerMask LayerMask => _layerMask;
	public AnimationCurve InputSpeedCurve => _inputSpeedCurve;
	public CameraMotionBasic CameraMotion => _cameraMotion;
	public Transform CameraRig => _cameraRig;
	public Rigidbody Rb => _rb;
	public CapsuleCollider PlayerCollider => _playerCollider;
	public StateAnimationSyncer AnimationSyncer => _animationSyncer;

	public float WalkSpeed => _walkSpeed;
	public float Acceleration => _acceleration;
	public float SpeedMultiplier => _speedMultiplier;
	public float RotationSpeed => _rotationSpeed;
	public float JumpForce => _jumpForce;
	public float ClimbTimeout => _climbTimeout;
	public float ClimbForce => _climbForce;

	public Vector2 MoveInput { get; private set; }
	public InputAction JumpAction { get; private set; }
	public Vector3 Velocity;
	public Vector3 Origin;
	[NonSerialized] public RaycastHit GroundHitInfo;
	[NonSerialized] public RaycastHit WallHitInfo;
	public bool IsGrounded { get; private set; }
	public bool IsWallInFront { get; private set; }

	public MoveState MoveState { get; private set; }
	public IdleState IdleState { get; private set; }
	public WallClimbState WallClimbState { get; private set; }
	public JumpState JumpState { get; private set; }
	public FallingState FallingState { get; private set; }

	private void Awake()
	{
#if UNITY_EDITOR
		Cursor.lockState = CursorLockMode.Locked;
#endif
		Application.targetFrameRate = 60;
		if (!_playerCollider) _playerCollider = GetComponent<CapsuleCollider>();
		if (!_rb) _rb = GetComponent<Rigidbody>();
		if (!_animationSyncer) _animationSyncer = GetComponent<StateAnimationSyncer>();

		JumpAction = InputSystem.actions.FindAction("jump");

		MoveState = new MoveState(this);
		IdleState = new IdleState(this);
		WallClimbState = new WallClimbState(this, _climbTimeout, _climbForce);
		JumpState = new JumpState(this, _jumpForce);
		FallingState = new FallingState(this);
	}

	private void Start()
	{
		SwitchState(MoveState);
	}

	private void Update()
	{
		MoveInput = TouchInputManager.InputMain.move.ReadValue<Vector2>();
		UpdateState();
		Velocity = _rb.linearVelocity;
	}

	private void FixedUpdate()
	{
		UpdateGroundRaycast();
		FixedUpdateState();

		IsWallInFront = Physics.Raycast(_playerCollider.bounds.center + 0.5f * _playerCollider.bounds.extents.y * Vector3.up,
			transform.forward, out WallHitInfo, _playerCollider.bounds.extents.z + 0.15f, ~_layerMask, QueryTriggerInteraction.Collide);
		Debug.DrawRay(_playerCollider.bounds.center + 0.5f * _playerCollider.bounds.extents.y * Vector3.up, 
			transform.forward * (_playerCollider.bounds.extents.z + 0.15f), Color.chartreuse);

		Origin = _playerCollider.bounds.center - _playerCollider.bounds.extents.y * Vector3.up;
	}

	public void UpdateGroundRaycast()
	{
		var isGround = Physics.SphereCast(_playerCollider.bounds.center, 0.01f, -Vector3.up, out GroundHitInfo, 2f, ~_layerMask);
		IsGrounded = isGround && GroundHitInfo.distance < 0.035f;
		Debug.DrawLine(_playerCollider.bounds.center,
			isGround ? GroundHitInfo.point : _playerCollider.bounds.center + Vector3.down,
			isGround ? Color.red : Color.green, 
			3);
	}
}
