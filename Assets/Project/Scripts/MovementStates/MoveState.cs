using UnityEngine;
using UnityEngine.InputSystem;
using MIRA;

public class MoveState : StateBase<MovementController>
{
	public MoveState(MovementController stateMachine) : base(stateMachine) { }

	public override void EnterState()
	{
		ClearTransitions();
		AddTransition(StateMachine.IdleState, _ => StateMachine.MoveInput.sqrMagnitude < 0.001f);
		AddTransition(StateMachine.FallingState, c => c.GroundHitInfo.distance > 0.45f);

		if (StateMachine.JumpAction != null)
			StateMachine.JumpAction.performed += OnJumpPerformed;

		StateMachine.AnimationSyncer.PlayWalkRunAnimation();
		StateMachine.CameraMotion.SetFov(80f, 0.6f);
		StateMachine.CameraMotion.SetDistance(0.15f, 0.6f);
		StateMachine.Rb.isKinematic = false;
	}

	public override void UpdateState()
	{
		TryTransition();
	}

	public override void FixedUpdateState()
	{
		TryTransition();

		var input = StateMachine.MoveInput;
		if (input.sqrMagnitude < 0.001f) return;

		// Compute camera-relative movement direction on the horizontal plane using camera yaw
		var camRotation = Quaternion.Euler(0f, StateMachine.CameraRig.eulerAngles.y, 0f);
		var camForward = camRotation * Vector3.forward;
		var camRight = camRotation * Vector3.right;
		var moveDir = (camForward * input.y + camRight * input.x).normalized;
		if (moveDir == Vector3.zero) return;

		var moveThreshold = StateMachine.InputSpeedCurve.Evaluate(input.sqrMagnitude);
		StateMachine.AnimationSyncer.ChangeMoveState(moveThreshold * 1.5f);

		// Smooth rotation towards movement direction
		var targetRotation = Quaternion.LookRotation(moveDir);
		StateMachine.Rb.MoveRotation(Quaternion.Slerp(StateMachine.Rb.rotation, targetRotation, StateMachine.RotationSpeed * Time.fixedDeltaTime));

		// Calculate target velocity and smoothly update horizontal velocity while preserving vertical physics
		var targetSpeed = StateMachine.WalkSpeed * StateMachine.SpeedMultiplier * moveThreshold;
		var targetVelocity = moveDir * targetSpeed;

		var currentVel = StateMachine.Rb.linearVelocity;
		var currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

		var lerpFactor = Mathf.Clamp01(StateMachine.Acceleration);
		var newHorizontal = Vector3.Lerp(currentHorizontal, targetVelocity, lerpFactor);
		StateMachine.Rb.linearVelocity = new Vector3(newHorizontal.x, currentVel.y, newHorizontal.z);
	}

	public override void ExitState()
	{
		if (StateMachine.JumpAction != null)
			StateMachine.JumpAction.performed -= OnJumpPerformed;

		StateMachine.CameraMotion.SetFov(60f, 0.6f);
		StateMachine.CameraMotion.SetDistance(0.3f, 0.6f);
	}

	private void OnJumpPerformed(InputAction.CallbackContext _)
	{
		StateMachine.SwitchState(StateMachine.JumpState);
	}
}
