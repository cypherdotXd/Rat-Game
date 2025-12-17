using System;
using UnityEngine;

namespace MIRA
{
    
    public abstract class StateMachineBehaviour<T> : MonoBehaviour where T : StateMachineBehaviour<T>
    {
        protected StateBase<T> CurrentState { get; private set; }
    
        protected virtual void Start()
        {
            CurrentState?.EnterState();
        }
    
        public void SwitchState(StateBase<T> newState)
        {
            if (newState == null) return;
            
            Debug.Log("Switching to " + newState.GetType().Name);
            
            CurrentState?.ExitState();
            CurrentState = newState;
            CurrentState.EnterState();
        }
    
        protected void UpdateState()
        {
            CurrentState?.UpdateState();
        }
    
        protected void FixedUpdateState()
        {
            CurrentState?.FixedUpdateState();
        }
    
        // Optional: For debugging
        public string GetCurrentStateName() => CurrentState?.GetType().Name ?? "None";
    }

    public abstract class StateBase<T> where T : StateMachineBehaviour<T>
    {
        public T StateMachine { get; private set; }
        public StateBase(T stateMachine)
        {
            StateMachine = stateMachine;
        }
        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void ExitState();
    }
}
