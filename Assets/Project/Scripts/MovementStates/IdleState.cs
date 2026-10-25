using UnityEngine;
using UnityEngine.InputSystem;
using MIRA;

public class IdleState : StateBase<MovementController>
{
	public IdleState(MovementController stateMachine) : base(stateMachine) { }

	public override void EnterState()
	{
		Debug.Log("Entering IdleState");
		ClearTransitions();
		AddTransition(StateMachine.MoveState, _ => StateMachine.MoveInput.sqrMagnitude > 0.001f);

		StateMachine.AnimationSyncer.PlayWalkRunAnimation(0, 0.4f);
		StateMachine.JumpAction.performed += SwitchToJumpState;
	}

	public override void UpdateState()
	{
		TryTransition();
	}

	public override void ExitState()
	{
		StateMachine.JumpAction.performed -= SwitchToJumpState;
	}
	
	private void SwitchToJumpState(InputAction.CallbackContext _)
	{
		StateMachine.SwitchState(StateMachine.JumpState);
	}
}
