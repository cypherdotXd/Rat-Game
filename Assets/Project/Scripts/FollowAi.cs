using System;
using System.Collections.Generic;
using Behaviour;
using UnityEngine;
using UnityEngine.AI;

public class FollowAi : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private List<Transform> patrolPoints;
    [SerializeField] private List<Transform> patrolPoints2;
    
    private NavMeshAgent navAgent;
    private float updateTimer;
    // private BehaviourTree bt;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // var s = bt.Run();
        // print(s);
        UpdateNavDestination();
        var d = Vector3.Distance(transform.position, target.position);
        if(d > 0.2f) return;
        Destroy(target.gameObject);
    }

    private void UpdateNavDestination()
    {
        updateTimer += Time.deltaTime;
        if (!(updateTimer > updateInterval)) return;
        navAgent.SetDestination(target.position);
        updateTimer = 0f;
    }
}
