using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MIRA
{
    public abstract class StateMachine<T> : MonoBehaviour where T : StateMachine<T>
    {
        public StateBase<T> CurrentState { get; private set; }
        public StateBase<T> LastState { get; private set; }

        public event Action<StateBase<T>, StateBase<T>> OnStateChanged;

        private Coroutine _delayedSwitchRoutine;

        public void SwitchState(StateBase<T> newState, float delayTime = -1f)
        {
            if (newState == null) return;

            if (_delayedSwitchRoutine != null)
            {
                StopCoroutine(_delayedSwitchRoutine);
                _delayedSwitchRoutine = null;
            }

            if (delayTime > 0f)
            {
                _delayedSwitchRoutine = StartCoroutine(SwitchStateRoutine(newState, delayTime));
            }
            else
            {
                ExecuteStateSwitch(newState);
            }
        }

        private IEnumerator SwitchStateRoutine(StateBase<T> newState, float delayTime)
        {
            yield return new WaitForSeconds(delayTime);
            _delayedSwitchRoutine = null;
            ExecuteStateSwitch(newState);
        }

        private void ExecuteStateSwitch(StateBase<T> newState)
        {
            LastState = CurrentState;
            CurrentState?.ExitState();

            CurrentState = newState;
            CurrentState.EnterState();

            OnStateChanged?.Invoke(LastState, CurrentState);
        }

        protected void UpdateState() => CurrentState?.UpdateState();
        protected void FixedUpdateState() => CurrentState?.FixedUpdateState();

        protected virtual void OnDisable()
        {
            if (_delayedSwitchRoutine != null)
            {
                StopCoroutine(_delayedSwitchRoutine);
                _delayedSwitchRoutine = null;
            }
        }

        public string GetCurrentStateName() => CurrentState?.GetType().Name ?? "None";
    }

    public abstract class StateBase<T> where T : StateMachine<T>
    {
        public T StateMachine { get; }
        protected readonly List<Transition<T>> transitions = new();

        protected StateBase(T stateMachine)
        {
            StateMachine = stateMachine;
        }

        public void AddTransition(StateBase<T> toState, Func<T, bool> condition)
        {
            transitions.Add(new Transition<T>(toState, condition));
        }

        public void ClearTransitions()
        {
            transitions.Clear();
        }

        public bool CheckTransitions(out StateBase<T> nextState)
        {
            nextState = null;
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].ShouldTransition(StateMachine))
                {
                    nextState = transitions[i].ToState;
                    return true;
                }
            }
            return false;
        }

        public void TryTransition()
        {
            if (CheckTransitions(out var nextState))
            {
                StateMachine.SwitchState(nextState);
            }
        }

        public virtual void EnterState() { }
        public virtual void UpdateState() { }
        public virtual void FixedUpdateState() { }
        public virtual void ExitState() { }
    }

    public class Transition<T> where T : StateMachine<T>
    {
        public StateBase<T> ToState { get; }
        public Func<T, bool> Condition { get; }

        public Transition(StateBase<T> toState, Func<T, bool> condition)
        {
            ToState = toState;
            Condition = condition;
        }

        public bool ShouldTransition(T machine) => Condition(machine);
    }
}
