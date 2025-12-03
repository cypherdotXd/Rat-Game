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
    [SerializeField] private Node node;
    
    private NavMeshAgent navAgent;
    private float updateTimer;

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
        UpdateNavDestination();
    }

    private void UpdateNavDestination()
    {
        updateTimer += Time.deltaTime;
        if (!(updateTimer > updateInterval)) return;
        navAgent.SetDestination(target.position);
        updateTimer = 0f;
    }
}
