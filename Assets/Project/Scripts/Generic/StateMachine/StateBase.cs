using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MIRA
{
    
    public abstract class StateMachineBehaviour<T> : MonoBehaviour where T : StateMachineBehaviour<T>
    {
        public StateBase<T> CurrentState { get; private set; }
        public StateBase<T> LastState { get; private set; }
    
        protected virtual void Start()
        {
            CurrentState?.EnterState();
        }
    
        public void SwitchState(StateBase<T> newState, float delayTime = -1)
        {
            if (newState == null) return;
            if (delayTime > 0)
            {
                StartCoroutine(SwitchStateRoutine(newState, delayTime));
            }
            else
            {
                // Debug.Log("Switching to " + newState.GetType().Name);
                LastState = CurrentState;
                CurrentState?.ExitState();
                CurrentState = newState;
                CurrentState.EnterState();
            }
        }

        public IEnumerator SwitchStateRoutine(StateBase<T> newState, float delayTime)
        {
            if (newState == null) yield break;
            yield return new WaitForSeconds(delayTime);
            
            // Debug.Log("Switching to " + newState.GetType().Name);
            LastState = CurrentState;
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
        protected List<Transition<T>> transitions = new();

        public StateBase(T stateMachine)
        {
            StateMachine = stateMachine;
        }
        
        public void AddTransition(StateBase<T> toState, Func<T, bool> condition)
        {
            transitions.Add(new Transition<T>(toState, condition));
        }
    
        // Check all transitions and return the state to switch to (or null)
        public bool CheckTransitions(T machine, out StateBase<T> nextState)
        {
            nextState = null;
            foreach (var transition in transitions.Where(t => t.ShouldTransition(machine)))
            {
                nextState = transition.ToState;
                return true;
            }
            return false;
        }

        public void TryTransition(T machine)
        {
            var doTransition = CheckTransitions(machine, out var nextState); 
            if(doTransition) machine.SwitchState(nextState);
        }
        
        public abstract void EnterState();
        public abstract void UpdateState();
        public abstract void FixedUpdateState();
        public abstract void ExitState();
    }
    
    public class Transition<T> where T : StateMachineBehaviour<T>
    {
        public StateBase<T> ToState { get; private set; }
        public Func<T, bool> Condition { get; private set; }
    
        public Transition(StateBase<T> toState, Func<T, bool> condition)
        {
            ToState = toState;
            Condition = condition;
        }
    
        public bool ShouldTransition(T machine)
        {
            return Condition(machine);
        }
    }

    public class Trigger<T> where T : StateMachineBehaviour<T>
    {
        public StateBase<T> ToState { get; private set; }
        public Action<T> Action { get; private set; }
        
        public Trigger(StateBase<T> toState, Action<T> action)
        {
            ToState = toState;
            Action = action;
        }
    }
}
