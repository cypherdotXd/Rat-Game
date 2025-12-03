using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform objectThrowPoint;
    [SerializeField] private float height = 1;
    [SerializeField] private GameObject[] throwables = new GameObject[5];
    [SerializeField] private Animator animator;
    // [SerializeField] private Animator faceAnimator;

    NavMeshAgent navAgent;
    private Vector3 random = Vector3.zero;
    private int moveAnimthresholdId;
    private int triggerJumpId;
    private int triggerThrowId;

    private void OnEnable()
    {
        // animator.

    }

    // Start is called before the first frame update
    void Start()
    {
        moveAnimthresholdId = Animator.StringToHash("SpeedThreshold");
        triggerJumpId = Animator.StringToHash("TriggerJump");
        triggerThrowId = Animator.StringToHash("TriggerThrow");
        
        navAgent = GetComponent<NavMeshAgent>();
        // ThrowObjectAtTarget(throwables[0].GetComponent<Rigidbody>(), player.position, 1f);
    }

    float timer = 0f;
    // Update is called once per frame
    void Update()
    {
        //animator.SetFloat(moveAnimthresholdId, navAgent.velocity.magnitude, 0.1f, Time.deltaTime);
        // animator.SetFloat(moveAnimthresholdId, navAgent.velocity.magnitude);
        // timer += Time.deltaTime;
        // if (!(timer > 3f)) return;
        // navAgent.SetDestination(player.position);
        // timer = 0f;
    }

    [ContextMenu("Throw")]
    public void Throw()
    {
        var throwable = throwables[0];
        var thing = Instantiate(throwable, objectThrowPoint.position, objectThrowPoint.rotation).GetComponent<Rigidbody>();
        thing.gameObject.SetActive(true);
        ThrowObjectAtTarget(thing, player.position, height);
    }
    
    private void ThrowObjectAtTarget(Rigidbody thing, Vector3 targetPosition, float h)
    {
        thing.isKinematic = false;
        thing.position = objectThrowPoint.position;
        float g = Physics.gravity.y;

        float Sx = targetPosition.x - thing.transform.position.x;
        float Sy = thing.transform.position.y - targetPosition.y;
        float Sz = targetPosition.z - thing.transform.position.z;

        Vector3 vel = new Vector3(Sx, 0, Sz) / (Mathf.Sqrt(-2 * (h + Sy) / g) + Mathf.Sqrt(-2 * h / g)) + (Mathf.Sqrt(-2 * g * h)) * Vector3.up;
        thing.useGravity = true;
        thing.linearVelocity = vel;
    }
}
