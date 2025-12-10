using System.Collections.Generic;
using UnityEngine;

using Behaviour;
using UnityEngine.AI;


public interface IStrategy
{
    Status Run();
    void Reset();
}

public class PatrolStrategy : IStrategy
{
    private readonly List<Transform> patrolPoints;
    private readonly NavMeshAgent navAgent;
    private int currentIndex;

    public PatrolStrategy(NavMeshAgent navAgent, List<Transform> patrolPoints)
    {
        this.navAgent = navAgent;
        this.patrolPoints = patrolPoints;
    }

    public Status Run()
    {
        if(currentIndex == patrolPoints.Count) return Status.Success;
        var target = patrolPoints[currentIndex];
        navAgent.SetDestination(target.position);
        if(navAgent.remainingDistance <= navAgent.stoppingDistance)
            currentIndex += 1;
        return Status.Running;
    }

    public void Reset()
    {
        throw new System.NotImplementedException();
    }
}
