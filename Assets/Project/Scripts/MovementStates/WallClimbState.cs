using DG.Tweening;
using UnityEngine;
using MIRA;

public class WallClimbState : StateBase<MovementController>
{
	private readonly float _timeOut;
	private readonly float _climbForce;
	private readonly LayerMask _layerMask;

	private float _timer;
	private bool _currentWallExists = true;
	private bool _newWallInFront;
	private Vector3 _lateralDirection;
	private Vector3 _wallNormal;

	public Vector3 WallNormal => _wallNormal;

	public WallClimbState(MovementController machine, float timeOut, float climbForce = 1.5f) : base(machine)
	{
		_timeOut = timeOut;
		_climbForce = climbForce;
		_layerMask = LayerMask.GetMask("Dynamic Object");
	}

	public override void EnterState()
	{
		ClearTransitions();
		_timer = 0f;
		_currentWallExists = true;
		_newWallInFront = false;

		SetWall(StateMachine.WallHitInfo.normal);

		AddTransition(StateMachine.MoveState,
			_ => _timer > _timeOut || (!_currentWallExists && !_newWallInFront));

		StateMachine.AnimationSyncer.PlayWalkRunAnimation(1.5f);
		StateMachine.AnimationSyncer.SetClimbing(true);
		StateMachine.CameraMotion?.StartDirectionFollow();
	}

	public override void FixedUpdateState()
	{
		TryTransition();

		var climber = StateMachine.Rb.transform;
		var rb = StateMachine.Rb;
		var collider = StateMachine.PlayerCollider;

		// Check for upcoming wall angle change (e.g. inner corner or ledge)
		_newWallInFront = Physics.Raycast(collider.bounds.center - 0.5f * collider.bounds.extents.y * Vector3.up,
			climber.forward, out var newHit, collider.bounds.extents.z + 0.05f, ~_layerMask, QueryTriggerInteraction.Ignore);
		if (_newWallInFront && Vector3.Dot(newHit.normal, _wallNormal) < 0.8f)
		{
			_timer = 0f;
			SetWall(newHit.normal);
		}

		_timer += Time.fixedDeltaTime;
		_currentWallExists = Physics.Raycast(StateMachine.Origin, -_wallNormal, collider.bounds.extents.z + 0.25f, ~_layerMask, QueryTriggerInteraction.Ignore);

		// Steer laterally on wall based on move input
		float lateral = Vector3.Dot(StateMachine.MoveDir, climber.right);
		_lateralDirection = Quaternion.AngleAxis(Mathf.Atan2(lateral, 1f) * 0.08f * Mathf.Rad2Deg, climber.up) * _lateralDirection;
		var targetRotation = Quaternion.LookRotation(_lateralDirection, _wallNormal);
		climber.rotation = Quaternion.Slerp(climber.rotation, targetRotation, 15f * Time.fixedDeltaTime);

		// Climb upward along wall surface, slightly pressing into the wall
		var climbDir = Mathf.Clamp((-Vector3.Dot(Vector3.up, climber.forward) + 1f) * 0.5f, 0.8f, 1f);
		rb.linearVelocity = _climbForce * climbDir * (climber.forward - 0.4f * climber.up);
	}

	public override void ExitState()
	{
		StopClimbing();
		StateMachine.AnimationSyncer.SetClimbing(false);
	}

	private void SetWall(Vector3 normal)
	{
		_wallNormal = normal.sqrMagnitude > 0.001f ? normal.normalized : -StateMachine.Rb.transform.forward;
		_lateralDirection = Vector3.ProjectOnPlane(Vector3.up, _wallNormal).normalized;
		if (_lateralDirection.sqrMagnitude < 0.001f)
			_lateralDirection = Vector3.ProjectOnPlane(StateMachine.Rb.transform.forward, _wallNormal).normalized;
	}

	private void StopClimbing()
	{
		var rb = StateMachine.Rb;
		rb.isKinematic = false;
		StateMachine.CameraMotion?.EndDirectionFollow();

		var cam = StateMachine.CameraRig;
		var forward = cam ? cam.forward : rb.transform.forward;
		forward.y = 0;
		if (forward.sqrMagnitude > 0.001f)
		{
			var normal = StateMachine.GroundHitInfo.collider ? StateMachine.GroundHitInfo.normal : Vector3.up;
			var targetRot = Quaternion.LookRotation(forward.normalized, normal);
			rb.transform.DORotateQuaternion(targetRot, 0.2f).SetLink(StateMachine.gameObject);
		}
	}
}
