using UnityEngine;
using MIRA;

public class MoveState : StateBase<MovementController>
{
	public MoveState(MovementController stateMachine) : base(stateMachine) { }

	public override void EnterState()
	{
		ClearTransitions();
		AddTransition(StateMachine.IdleState, _ => StateMachine.MoveDir.sqrMagnitude < 0.001f);
		AddTransition(StateMachine.FallingState, c => c.GroundHitInfo.distance > 0.45f);
		AddTransition(StateMachine.JumpState, c => c.HasJumpInput);

		StateMachine.AnimationSyncer.PlayWalkRunAnimation();
		if (StateMachine.CameraMotion != null)
		{
			StateMachine.CameraMotion.SetFov(80f, 0.6f);
			StateMachine.CameraMotion.SetDistance(0.15f, 0.6f);
		}
		StateMachine.Rb.isKinematic = false;
	}

	public override void UpdateState()
	{
		TryTransition();
	}

	public override void FixedUpdateState()
	{
		TryTransition();

		var moveDir = StateMachine.MoveDir;
		var targetHeading = new Vector3(moveDir.x, 0f, moveDir.z);
		if (targetHeading.sqrMagnitude < 0.001f) return;

		var moveThreshold = StateMachine.InputSpeedCurve.Evaluate(Mathf.Clamp01(targetHeading.magnitude));
		StateMachine.AnimationSyncer.ChangeMoveState(moveThreshold * 1.5f);

		var targetRotation = Quaternion.LookRotation(targetHeading.normalized);
		StateMachine.Rb.MoveRotation(Quaternion.Slerp(StateMachine.Rb.rotation, targetRotation, StateMachine.RotationSpeed * Time.fixedDeltaTime));

		var targetSpeed = StateMachine.WalkSpeed * StateMachine.SpeedMultiplier * moveThreshold;
		var targetVelocity = targetHeading.normalized * targetSpeed;

		var currentVel = StateMachine.Rb.linearVelocity;
		var currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

		var lerpFactor = Mathf.Clamp01(StateMachine.Acceleration);
		var newHorizontal = Vector3.Lerp(currentHorizontal, targetVelocity, lerpFactor);
		StateMachine.Rb.linearVelocity = new Vector3(newHorizontal.x, currentVel.y, newHorizontal.z);
	}

	public override void ExitState()
	{
		if (StateMachine.CameraMotion != null)
		{
			StateMachine.CameraMotion.SetFov(60f, 0.6f);
			StateMachine.CameraMotion.SetDistance(0.3f, 0.6f);
		}
	}
}
