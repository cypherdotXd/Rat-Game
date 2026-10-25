using UnityEngine;
using UnityEngine.InputSystem;
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
		Debug.Log("Entering Jump State");
		ClearTransitions();
		_jumpTimer = 0f;
		_jumpExecuted = false;
		_jumpButtonUp = false;

		AddTransition(StateMachine.LastState, controller =>
		{
			if (!_jumpExecuted) return false;
			var jumpEnded = controller.Velocity.y <= -0.0001f && controller.GroundHitInfo.distance < 0.4f;
			if (jumpEnded)
				controller.AnimationSyncer.PlayLandingAnimation();
			return jumpEnded;
		});
		AddTransition(StateMachine.WallClimbState, _ => StateMachine.IsWallInFront && !StateMachine.IsGrounded);

		StateMachine.AnimationSyncer.PlayJumpAnimation();
		StateMachine.JumpAction.canceled += OnJumpCancelAction;
	}

	public override void UpdateState()
	{
		TryTransition();
	}

	public override void FixedUpdateState()
	{
		var rb = StateMachine.Rb;
		var animController = StateMachine.AnimationSyncer;
		var input = StateMachine.MoveInput;

		var direction = new Vector3(StateMachine.CameraRig.forward.x, 0, StateMachine.CameraRig.forward.z);
		var targetRotation = Quaternion.LookRotation(direction);
		var yAngle = Vector2.SignedAngle(input, Vector2.up);
		targetRotation *= Quaternion.Euler(0, yAngle, 0);

		_jumpTimer += Time.fixedDeltaTime;
		if (_jumpExecuted)
		{
			if (input.sqrMagnitude > 0.1f)
			{
				rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, 6 * Time.fixedDeltaTime));
			}
			rb.AddForce(2.8f * direction, ForceMode.Acceleration);
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
		StateMachine.JumpAction.canceled -= OnJumpCancelAction;
		_jumpTimer = 0f;
		_jumpExecuted = false;
		_jumpButtonUp = false;
	}

	private void Jump()
	{
		_jumpExecuted = true;
		StateMachine.Rb.AddForce(_jumpForce * Vector3.up, ForceMode.Impulse);
	}

	private void OnJumpCancelAction(InputAction.CallbackContext _)
	{
		_jumpButtonUp = true;
	}
}
