using UnityEngine;
using MIRA;

public class FallingState : StateBase<MovementController>
{
	public FallingState(MovementController stateMachine) : base(stateMachine) { }

	public override void EnterState()
	{
		Debug.Log("Entering Fall State");
		ClearTransitions();

		var targetState = StateMachine.LastState is WallClimbState or JumpState ? StateMachine.IdleState : StateMachine.LastState;
		AddTransition(targetState, controller => controller.GroundHitInfo.distance < 0.4f);

		StateMachine.AnimationSyncer.PlayFallingAnimation();
	}

	public override void UpdateState()
	{
		TryTransition();
	}

	public override void ExitState()
	{
		StateMachine.AnimationSyncer.PlayLandingAnimation();
	}
}
