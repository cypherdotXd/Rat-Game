using UnityEngine;
using MIRA;

public class WireClimbState : StateBase<MovementController>
{
	private PathClimb _path;
	private float _currentT;
	private Vector3 _currentFacing;

	public WireClimbState(MovementController machine) : base(machine) { }

	public override void EnterState()
	{
		ClearTransitions();
		_path = StateMachine.ActivePath;
		_currentT = StateMachine.PathT;

		StateMachine.Rb.isKinematic = true;
		StateMachine.Rb.linearVelocity = Vector3.zero;

		_path.Evaluate(_currentT, out Vector3 pos, out Vector3 tangent, out Vector3 up);
		_currentFacing = tangent;
		StateMachine.Rb.position = pos;
		StateMachine.Rb.rotation = Quaternion.LookRotation(_currentFacing, up);

		StateMachine.AnimationSyncer.PlayWalkRunAnimation(0f);
	}

	public override void FixedUpdateState()
	{
		if (!_path)
		{
			DismountToFalling();
			return;
		}

		if (StateMachine.ConsumeJump())
		{
			DismountWithJump();
			return;
		}

		var moveDir = StateMachine.MoveDir;
		_path.Evaluate(_currentT, out Vector3 pos, out Vector3 tangent, out Vector3 up);

		float moveAmount = 0f;
		if (moveDir.sqrMagnitude > 0.001f)
		{
			float dot = Vector3.Dot(moveDir.normalized, tangent.normalized);
			moveAmount = dot * Mathf.Clamp01(moveDir.magnitude);
		}

		// Advance along spline
		float length = Mathf.Max(_path.Length, 0.1f);
		float speed = StateMachine.WalkSpeed * StateMachine.SpeedMultiplier;
		float deltaT = (speed * moveAmount / length) * Time.fixedDeltaTime;
		_currentT += deltaT;

		// Check if reached end on open splines
		if (!_path.Closed && (_currentT <= 0f || _currentT >= 1f))
		{
			_currentT = Mathf.Clamp01(_currentT);
			_path.Evaluate(_currentT, out pos, out tangent, out up);
			StateMachine.Rb.position = pos;
			DismountAtEnd();
			return;
		}

		if (_path.Closed)
			_currentT = Mathf.Repeat(_currentT, 1f);

		// Update facing and rotation
		if (Mathf.Abs(moveAmount) > 0.05f)
			_currentFacing = moveAmount > 0f ? tangent : -tangent;

		var targetRotation = Quaternion.LookRotation(_currentFacing, up);
		StateMachine.Rb.MoveRotation(Quaternion.Slerp(StateMachine.Rb.rotation, targetRotation, StateMachine.RotationSpeed * Time.fixedDeltaTime));
		StateMachine.Rb.MovePosition(pos);

		// Update motion animation
		StateMachine.AnimationSyncer.ChangeMoveState(Mathf.Abs(moveAmount) * 1.5f);
	}

	public override void ExitState()
	{
		StateMachine.Rb.isKinematic = false;
	}

	private void DismountWithJump()
	{
		StateMachine.SetDismountCooldown(0.35f);
		StateMachine.Rb.isKinematic = false;
		StateMachine.SwitchState(StateMachine.JumpState);
	}

	private void DismountAtEnd()
	{
		StateMachine.SetDismountCooldown(0.4f);
		StateMachine.Rb.isKinematic = false;
		StateMachine.Rb.linearVelocity = _currentFacing * StateMachine.WalkSpeed;

		if (StateMachine.IsGrounded)
			StateMachine.SwitchState(StateMachine.MoveState);
		else
			StateMachine.SwitchState(StateMachine.FallingState);
	}

	private void DismountToFalling()
	{
		StateMachine.SetDismountCooldown(0.35f);
		StateMachine.Rb.isKinematic = false;
		StateMachine.SwitchState(StateMachine.FallingState);
	}
}
