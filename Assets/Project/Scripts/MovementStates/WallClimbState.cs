using DG.Tweening;
using UnityEngine;
using MIRA;

public class WallClimbState : StateBase<MovementController>
{
	private float _timer;
	private readonly float _timeOut;
	private readonly float _climbForce;
	private bool _currentWallExists = true;
	private bool _newWallInFront;
	private Vector3 _wallDir;
	private Vector3 _lateralDirection;
	private Vector3 _wallNormal;
	private readonly LayerMask _layerMask;

	public WallClimbState(MovementController machine, float timeOut, float climbForce = 1.5f) : base(machine)
	{
		_timeOut = timeOut;
		_climbForce = climbForce;
		_layerMask = LayerMask.GetMask("Dynamic Object");
	}

	public override void EnterState()
	{
		Debug.Log("Entering Wall Climb");
		ClearTransitions();
		_timer = 0f;
		_currentWallExists = true;
		_newWallInFront = false;

		var climber = StateMachine.Rb.transform;
		var wallHitInfo = StateMachine.WallHitInfo;
		_wallDir = (climber.position - wallHitInfo.point).normalized;
		_wallNormal = wallHitInfo.normal;
		var climbAngle = Vector3.SignedAngle(climber.forward, -_wallNormal, Vector3.up);
		_lateralDirection = Quaternion.AngleAxis(climbAngle, -_wallNormal) * Vector3.Cross(climber.right, _wallNormal);

		AddTransition(StateMachine.MoveState,
			_ => _timer > _timeOut || (!_currentWallExists && !_newWallInFront));

		StateMachine.AnimationSyncer.PlayWalkRunAnimation(1.5f);
		StateMachine.AnimationSyncer.SetClimbing(true);
		StateMachine.CameraMotion.StartDirectionFollow();
	}

	public override void FixedUpdateState()
	{
		TryTransition();

		var climber = StateMachine.Rb.transform;
		var rb = StateMachine.Rb;
		var collider = StateMachine.PlayerCollider;
		var origin = StateMachine.Origin;
		var input = StateMachine.MoveInput;

		_newWallInFront = Physics.Raycast(collider.bounds.center - 0.5f * collider.bounds.extents.y * Vector3.up,
			climber.forward, out var newWallHitInfo, collider.bounds.extents.z + 0.05f, ~_layerMask);
		if (_newWallInFront)
		{
			var diff = Vector3.Dot(newWallHitInfo.normal, _wallNormal);
			if (diff < 0.8f)
				ChangeWall(newWallHitInfo);
		}

		_timer += Time.fixedDeltaTime;
		_currentWallExists = Physics.Raycast(origin, -_wallDir, collider.bounds.extents.z + 0.2f, ~_layerMask);

		_lateralDirection = Quaternion.AngleAxis(Mathf.Atan2(input.x, 1) * 0.08f * Mathf.Rad2Deg, climber.up) * _lateralDirection;
		var targetRotation = Quaternion.LookRotation(_lateralDirection, _wallNormal);
		climber.rotation = Quaternion.Slerp(climber.rotation, targetRotation, 15f * Time.fixedDeltaTime);

		// Move Upward
		var climbDir = (-Vector3.Dot(Vector3.up, climber.forward) + 1f) / 2f;
		climbDir = Mathf.Clamp(climbDir, 0.8f, 1f);
		var moveDir = climber.forward - 0.4f * climber.up;
		rb.linearVelocity = _climbForce * climbDir * moveDir;
	}

	public override void ExitState()
	{
		StopClimbing();
		StateMachine.AnimationSyncer.SetClimbing(false);
	}

	private void StopClimbing()
	{
		var cameraMotion = StateMachine.CameraMotion;
		var cameraRig = StateMachine.CameraRig;
		var rb = StateMachine.Rb;

		rb.isKinematic = false;
		cameraMotion.EndDirectionFollow();
		var forward = cameraRig.forward;
		forward.y = 0;
		var r = Quaternion.LookRotation(forward.normalized, StateMachine.GroundHitInfo.collider ? StateMachine.GroundHitInfo.normal : Vector3.up);
		StateMachine.Rb.transform.DORotateQuaternion(r, 0.2f);
	}

	private void ChangeWall(RaycastHit newWallHitInfo)
	{
		Debug.Log("Wall change");
		var climber = StateMachine.Rb.transform;
		_timer = 0f;
		_wallNormal = newWallHitInfo.normal;
		_wallDir = (climber.position - newWallHitInfo.point).normalized;
		var climbAngle = Vector3.SignedAngle(climber.forward, -_wallNormal, Vector3.up);
		_lateralDirection = Quaternion.AngleAxis(climbAngle, -_wallNormal) * Vector3.Cross(climber.right, _wallNormal);
	}
}
