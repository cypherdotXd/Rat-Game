using UnityEngine;
using MIRA;

public class IdleState : StateBase<MovementController>
{
	public IdleState(MovementController stateMachine) : base(stateMachine) { }

	public override void EnterState()
	{
		ClearTransitions();
		AddTransition(StateMachine.MoveState, _ => StateMachine.MoveDir.sqrMagnitude > 0.001f);
		AddTransition(StateMachine.JumpState, c => c.HasJumpInput);

		StateMachine.AnimationSyncer.PlayWalkRunAnimation(0, 0.4f);
	}

	public override void UpdateState()
	{
		TryTransition();
	}

	public override void FixedUpdateState()
	{
		TryTransition();
	}

	public override void ExitState()
	{
	}
}
