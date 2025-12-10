using System;
using System.Collections.Generic;
using Behaviour;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class FollowAi : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private List<Transform> patrolPoints2;
    
    private NavMeshAgent navAgent;
    private float updateTimer;
    private BehaviourTree bt;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        bt = new("BT");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bt
            .AddChild(new LeafNode("l1", new PatrolStrategy(navAgent, patrolPoints)))
            .AddChild(new LeafNode("l2", new PatrolStrategy(navAgent, patrolPoints2)));
    }

    // Update is called once per frame
    void Update()
    {
        var s = bt.Run();
        // print(s);
        // UpdateNavDestination();
    }

    private void UpdateNavDestination()
    {
        updateTimer += Time.deltaTime;
        if (!(updateTimer > updateInterval)) return;
        navAgent.SetDestination(target.position);
        updateTimer = 0f;
    }
}
