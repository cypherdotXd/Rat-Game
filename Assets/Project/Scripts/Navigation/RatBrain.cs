using System.Collections.Generic;
using UnityEngine;

public class RatBrain : MonoBehaviour, IRatInput
{
    [SerializeField] private MovementController _controller;
    [SerializeField] private RatGraph _graph;
    [SerializeField] private Transform _target;
    [SerializeField] private RatBrainSettings _settings;
    [SerializeField] private bool _debugLogs = false;

    private List<(RatNode node, RatEdge edge)> _path;
    private int _pathIndex;
    private float _repathTimer;
    private RatNode _currentGoalNode;
    private Vector3 _smoothedMoveDir;
    private bool _jumpRequested;

    public Vector3 MoveDir { get; private set; }

    public bool Jump
    {
        get => _jumpRequested;
        set => _jumpRequested = value;
    }

    public bool Sprint => false;
    public bool IsJumpHeld => false;

    public MovementController Controller => _controller;
    public Transform Target => _target;
    public IReadOnlyList<(RatNode node, RatEdge edge)> CurrentPath => _path;
    public int CurrentPathIndex => _pathIndex;

    public void SetTarget(Transform target) => _target = target;
    public void SetGraph(RatGraph graph) => _graph = graph;

    public void ResetPath()
    {
        _path = null;
        _pathIndex = 0;
        _currentGoalNode = null;
        _smoothedMoveDir = Vector3.zero;
        MoveDir = Vector3.zero;
        _repathTimer = 999f;
    }

    public void Initialize()
    {
        if (!_controller) _controller = GetComponent<MovementController>();
        if (!_graph) _graph = FindFirstObjectByType<RatGraph>();
        if (!_settings) _settings = ScriptableObject.CreateInstance<RatBrainSettings>();
    }

    private void Awake()
    {
        Initialize();
    }

    public void StepBrain(float dt)
    {
        if (!_controller || !_target || !_graph)
        {
            MoveDir = Vector3.zero;
            return;
        }

        _repathTimer += dt;

        float repathInterval = _settings ? _settings.repathInterval : 0.25f;
        float reachRadius = _settings ? _settings.reachRadius : 0.4f;
        float k = _settings ? _settings.inputSmoothingK : 10f;
        float maxTurnRate = _settings ? _settings.maxTurnRate : 8f;

        Vector3 currentPos = _controller.transform.position;
        Vector3 targetPos = _target.position;

        // If already close to player, stop
        float distToTarget = Vector3.Distance(currentPos, targetPos);
        if (distToTarget <= reachRadius)
        {
            MoveDir = Vector3.zero;
            _smoothedMoveDir = Vector3.zero;
            return;
        }

        // Never repath while airborne
        bool isAirborne = !_controller.IsGrounded && !_controller.IsAttached;
        if (!isAirborne && _repathTimer >= repathInterval)
        {
            _repathTimer = 0f;
            ResolveAndRepath(currentPos, targetPos);
        }

        // Follow path
        if (_path != null && _pathIndex < _path.Count)
        {
            var targetWp = _path[_pathIndex].node;
            float distToWp = Vector3.Distance(currentPos, targetWp.Position);

            if (distToWp < reachRadius)
            {
                _pathIndex++;
                if (_pathIndex < _path.Count)
                    targetWp = _path[_pathIndex].node;
            }

            if (_pathIndex < _path.Count)
            {
                Vector3 toWp = targetWp.Position - currentPos;
                Vector3 desiredDir = Vector3.ProjectOnPlane(toWp, _controller.SurfaceNormal).normalized;

                // Turn rate limit + exponential smoothing
                float turnStep = maxTurnRate * dt;
                Vector3 turnCapped = Vector3.RotateTowards(_smoothedMoveDir, desiredDir, turnStep, 0f);
                float lerpFactor = 1f - Mathf.Exp(-k * dt);
                _smoothedMoveDir = Vector3.Lerp(turnCapped, desiredDir, lerpFactor);
                MoveDir = _smoothedMoveDir.normalized;
                return;
            }
        }

        // If path exhausted but target is direct line of sight on same plane
        Vector3 direct = Vector3.ProjectOnPlane(targetPos - currentPos, _controller.SurfaceNormal).normalized;
        _smoothedMoveDir = Vector3.Lerp(_smoothedMoveDir, direct, 1f - Mathf.Exp(-k * dt));
        MoveDir = _smoothedMoveDir.normalized;
    }

    private void FixedUpdate()
    {
        StepBrain(Time.fixedDeltaTime);
    }

    private void ResolveAndRepath(Vector3 currentPos, Vector3 targetPos)
    {
        var targetCollider = _target.GetComponent<Collider>();
        var goalNode = _graph.FindNearestNode(targetPos, targetCollider);
        var startNode = _graph.FindNearestNode(currentPos, _controller.SurfaceCollider);

        if (goalNode == null || startNode == null) return;

        if (goalNode != _currentGoalNode || _path == null || _pathIndex >= _path.Count)
        {
            if (_debugLogs)
                Debug.Log($"[RatBrain] Repathing from {startNode.name} to {goalNode.name}");

            _path = RatPathfinder.FindPath(_graph, startNode, goalNode, _controller.WalkSpeed);
            _pathIndex = 0;
            _currentGoalNode = goalNode;
        }
    }

    private void OnDrawGizmos()
    {
        if (_path == null || _path.Count == 0) return;

        Gizmos.color = Color.magenta;
        for (int i = 0; i < _path.Count - 1; i++)
        {
            if (_path[i].node != null && _path[i + 1].node != null)
                Gizmos.DrawLine(_path[i].node.Position, _path[i + 1].node.Position);
        }

        if (_pathIndex < _path.Count && _path[_pathIndex].node != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_path[_pathIndex].node.Position, 0.2f);
        }
    }
}
