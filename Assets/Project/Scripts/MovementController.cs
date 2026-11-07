using System;
using UnityEngine;
using MIRA;

public class MovementController : StateMachine<MovementController>
{
	[Header("Dependencies")]
	[SerializeField] private CameraMotionBasic _cameraMotion;
	[SerializeField] private Transform _cameraRig;
	[SerializeField] private Rigidbody _rb;
	[SerializeField] private CapsuleCollider _playerCollider;
	[SerializeField] private StateAnimationSyncer _animationSyncer;
	[SerializeField] private LayerMask _layerMask;
	[SerializeField] private AnimationCurve _inputSpeedCurve;

	[Header("Movement Settings")]
	[SerializeField] private float _walkSpeed = 0.7f;
	[SerializeField] private float _acceleration = 4f;
	[SerializeField] private float _speedMultiplier = 1f;
	[SerializeField] private float _rotationSpeed = 8f;
	[SerializeField] private float _jumpForce = 3.5f;
	[SerializeField] private float _climbTimeout = 50f;
	[SerializeField] private float _climbForce = 1.5f;

	public CameraMotionBasic CameraMotion => _cameraMotion;
	public Transform CameraRig => _cameraRig;
	public Rigidbody Rb => _rb;
	public CapsuleCollider PlayerCollider => _playerCollider;
	public StateAnimationSyncer AnimationSyncer => _animationSyncer;
	public AnimationCurve InputSpeedCurve => _inputSpeedCurve;

	public float WalkSpeed => _walkSpeed;
	public float Acceleration => _acceleration;
	public float SpeedMultiplier => _speedMultiplier;
	public float RotationSpeed => _rotationSpeed;
	public float JumpForce => _jumpForce;
	public float ClimbTimeout => _climbTimeout;
	public float ClimbForce => _climbForce;

	private IRatInput _ratInput;
	public IRatInput RatInput => _ratInput;
	public Vector3 MoveDir => _ratInput != null ? _ratInput.MoveDir : Vector3.zero;
	public bool HasJumpInput => _ratInput != null && _ratInput.Jump;
	public bool IsAttached => CurrentState is global::WallClimbState or global::WireClimbState;

	public Vector3 SurfaceNormal =>
		CurrentState is WallClimbState wallClimb && wallClimb.WallNormal != Vector3.zero ? wallClimb.WallNormal :
		IsGrounded && GroundHitInfo.collider ? GroundHitInfo.normal : Vector3.up;

	public Collider SurfaceCollider =>
		CurrentState is WallClimbState ? WallHitInfo.collider :
		IsGrounded ? GroundHitInfo.collider : null;

	public Vector3 Velocity => _rb ? _rb.linearVelocity : Vector3.zero;
	public Vector3 Origin => _playerCollider ? _playerCollider.bounds.center - _playerCollider.bounds.extents.y * Vector3.up : transform.position;

	[NonSerialized] public RaycastHit GroundHitInfo;
	[NonSerialized] public RaycastHit WallHitInfo;
	public bool IsGrounded { get; private set; }
	public bool IsWallInFront { get; private set; }

	public MoveState MoveState { get; private set; }
	public IdleState IdleState { get; private set; }
	public WallClimbState WallClimbState { get; private set; }
	public JumpState JumpState { get; private set; }
	public FallingState FallingState { get; private set; }
	public WireClimbState WireClimbState { get; private set; }

	public PathClimb ActivePath { get; private set; }
	public float PathT { get; private set; }
	private float _dismountCooldown;

	public bool ConsumeJump()
	{
		if (_ratInput == null || !_ratInput.Jump) return false;
		_ratInput.Jump = false;
		return true;
	}

	public void Initialize()
	{
		if (!_playerCollider) _playerCollider = GetComponent<CapsuleCollider>();
		if (!_rb) _rb = GetComponent<Rigidbody>();
		if (!_animationSyncer) _animationSyncer = GetComponent<StateAnimationSyncer>();

		_ratInput = GetComponent<IRatInput>();
		if (_ratInput == null) _ratInput = gameObject.AddComponent<PlayerInput>();

		if (MoveState == null)
		{
			MoveState = new MoveState(this);
			IdleState = new IdleState(this);
			WallClimbState = new WallClimbState(this, _climbTimeout, _climbForce);
			JumpState = new JumpState(this, _jumpForce);
			FallingState = new FallingState(this);
			WireClimbState = new WireClimbState(this);
		}

		if (CurrentState == null)
			SwitchState(MoveState);
	}

	private void Awake()
	{
#if UNITY_EDITOR
		Cursor.lockState = CursorLockMode.Locked;
#endif
		Application.targetFrameRate = 60;
		Initialize();
	}

	private void Update()
	{
		if (_dismountCooldown > 0f)
			_dismountCooldown -= Time.deltaTime;

		UpdateState();
	}

	private void FixedUpdate() => StepPhysics();

	public void StepPhysics()
	{
		UpdateGroundRaycast();
		IsWallInFront = Physics.Raycast(_playerCollider.bounds.center + 0.5f * _playerCollider.bounds.extents.y * Vector3.up,
			transform.forward, out WallHitInfo, _playerCollider.bounds.extents.z + 0.15f, ~_layerMask, QueryTriggerInteraction.Ignore);
		FixedUpdateState();
	}

	public void UpdateGroundRaycast()
	{
		bool isGround = Physics.SphereCast(_playerCollider.bounds.center, 0.01f, -Vector3.up, out GroundHitInfo, 2f, ~_layerMask);
		IsGrounded = isGround && GroundHitInfo.distance < 0.035f;
	}

	public bool CanMountPath(PathClimb path, out float t)
	{
		t = 0f;
		return _dismountCooldown <= 0f && CurrentState != WireClimbState &&
		       path.TryGetNearestPoint(transform.position, out t, out _, out float dist) &&
		       dist <= path.MountDistance;
	}

	public void MountPath(PathClimb path, float startT)
	{
		ActivePath = path;
		PathT = startT;
		SwitchState(WireClimbState);
	}

	public void SetDismountCooldown(float duration = 0.35f) => _dismountCooldown = duration;

	private void OnTriggerStay(Collider other)
	{
		if ((other.TryGetComponent<PathClimb>(out var path) || (path = other.GetComponentInParent<PathClimb>())) &&
		    CanMountPath(path, out float t))
		{
			MountPath(path, t);
		}
	}
}
