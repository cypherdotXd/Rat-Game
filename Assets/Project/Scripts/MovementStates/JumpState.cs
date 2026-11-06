using UnityEngine;
using MIRA;

public class JumpState : StateBase<MovementController>
{
	private readonly float _jumpForce;
	private readonly float _jumpDelay;
	private float _jumpTimer;
	private bool _jumpExecuted;
	private bool _jumpButtonUp;

	public JumpState(MovementController stateMachine, float jumpForce, float jumpDelay = 0.05f) : base(stateMachine)
	{
		_jumpForce = jumpForce;
		_jumpDelay = jumpDelay;
	}

	public override void EnterState()
	{
		ClearTransitions();
		_jumpTimer = 0f;
		_jumpExecuted = false;
		_jumpButtonUp = false;

		StateMachine.ConsumeJump();

		var landTargetState = StateMachine.LastState is WallClimbState or WireClimbState or JumpState ? StateMachine.IdleState : StateMachine.LastState;
		AddTransition(landTargetState, controller =>
		{
			if (!_jumpExecuted) return false;
			var jumpEnded = controller.Velocity.y <= -0.0001f && controller.GroundHitInfo.distance < 0.4f;
			if (jumpEnded)
				controller.AnimationSyncer.PlayLandingAnimation();
			return jumpEnded;
		});
		AddTransition(StateMachine.WallClimbState, _ => StateMachine.IsWallInFront && !StateMachine.IsGrounded);

		StateMachine.AnimationSyncer.PlayJumpAnimation();
	}

	public override void UpdateState()
	{
		TryTransition();
	}

	public override void FixedUpdateState()
	{
		TryTransition();

		var rb = StateMachine.Rb;
		var animController = StateMachine.AnimationSyncer;
		var moveDir = StateMachine.MoveDir;

		var direction = rb.transform.forward;
		direction.y = 0f;
		if (direction.sqrMagnitude > 0.001f) direction.Normalize();

		if (moveDir.sqrMagnitude > 0.01f)
		{
			var targetHeading = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
			var targetRotation = Quaternion.LookRotation(targetHeading);
			rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 6f * Time.fixedDeltaTime));
			direction = targetHeading;
		}

		_jumpTimer += Time.fixedDeltaTime;
		if (_jumpExecuted)
		{
			rb.AddForce(2.8f * direction, ForceMode.Acceleration);

			if (StateMachine.RatInput != null && !StateMachine.RatInput.IsJumpHeld)
				_jumpButtonUp = true;

			if (!_jumpButtonUp)
			{
				rb.AddForce(0.4f * _jumpForce * (Vector3.up + 0.3f * rb.transform.forward), ForceMode.Force);
			}

			var isFalling = StateMachine.Velocity.y <= -0.5f && StateMachine.GroundHitInfo.distance > 0.46f;
			animController.NotifyFalling(isFalling);
			return;
		}

		if (_jumpTimer < _jumpDelay) return;
		Jump();
	}

	public override void ExitState()
	{
		_jumpTimer = 0f;
		_jumpExecuted = false;
		_jumpButtonUp = false;
	}

	private void Jump()
	{
		_jumpExecuted = true;
		StateMachine.Rb.AddForce(_jumpForce * Vector3.up, ForceMode.Impulse);
	}
}
