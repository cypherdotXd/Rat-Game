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
    [SerializeField] private Animator faceAnimator;

    NavMeshAgent navAgent;
    private Animator animator;
    private Vector3 random = Vector3.zero;
    private int moveAnimthresholdId;
    private int triggerJumpId;
    private int triggerThrowId;

    private void OnEnable()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        moveAnimthresholdId = Animator.StringToHash("SpeedThreshold");
        triggerJumpId = Animator.StringToHash("TriggerJump");
        triggerThrowId = Animator.StringToHash("TriggerThrow");
        throwables[0].transform.position = transform.position + 2f * transform.forward + Vector3.up;
        foreach(GameObject throwable in throwables)
        {
            throwable.GetComponent<Rigidbody>().useGravity = false;
        }
        animator = GetComponent<Animator>();
        navAgent = GetComponent<NavMeshAgent>();
    }

    float timer = 0f;
    // Update is called once per frame
    void Update()
    {
        //animator.SetFloat(moveAnimthresholdId, navAgent.velocity.magnitude, 0.1f, Time.deltaTime);
        animator.SetFloat(moveAnimthresholdId, navAgent.velocity.magnitude);
        timer += Time.deltaTime;
        if(timer > 3f)
        {
            navAgent.SetDestination(player.position);
            int i = Random.Range(0, throwables.GetLength(0));
            GameObject thing = Instantiate(throwables[i], objectThrowPoint.position, Quaternion.identity);
            ThrowObjectAtTarget(thing.GetComponent<Rigidbody>(), player.position, height);
            animator.SetBool(triggerThrowId, true);
            //navAgent.destination = player.position;
            timer = 0f;
        }
    }

    void ThrowObjectAtTarget(Rigidbody thing, Vector3 targetPosition, float h)
    {
        float g = Physics.gravity.y;

        float Sx = targetPosition.x - thing.transform.position.x;
        float Sy = thing.transform.position.y - targetPosition.y;
        float Sz = targetPosition.z - thing.transform.position.z;

        Vector3 vel = new Vector3(Sx, 0, Sz) / (Mathf.Sqrt(-2 * (h + Sy) / g) + Mathf.Sqrt(-2 * h / g)) + (Mathf.Sqrt(-2 * g * h)) * Vector3.up;
        thing.useGravity = true;
        thing.linearVelocity = vel;
    }
}
