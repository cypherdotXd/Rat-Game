using System.Collections.Generic;
using UnityEngine;

public enum JumpExecutionState
{
    NONE,
    APPROACH,
    ALIGN,
    LAUNCH,
    AIR,
    LAND
}

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

    private JumpExecutionState _jumpState = JumpExecutionState.NONE;
    private float _jumpStateTimer;

    private float _stuckTimer;
    private float _stuckCheckTimer;
    private Vector3 _lastStuckCheckPos;

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
    public JumpExecutionState CurrentJumpState => _jumpState;

    public void SetTarget(Transform target) => _target = target;
    public void SetGraph(RatGraph graph) => _graph = graph;

    public void ResetPath()
    {
        _path = null;
        _pathIndex = 0;
        _currentGoalNode = null;
        _smoothedMoveDir = Vector3.zero;
        MoveDir = Vector3.zero;
        _jumpState = JumpExecutionState.NONE;
        _jumpStateTimer = 0f;
        _stuckTimer = 0f;
        _repathTimer = 999f;
    }

    public void ForceRepath()
    {
        _repathTimer = 999f;
        _path = null;
        _pathIndex = 0;
        _currentGoalNode = null;
    }

    public void Initialize()
    {
        if (!_controller) _controller = GetComponent<MovementController>();
        if (!_graph) _graph = Object.FindAnyObjectByType<RatGraph>();
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

        Vector3 currentPos = _controller.transform.position;
        Vector3 targetPos = _target.position;

        CheckStuck(dt, currentPos);

        // If currently executing a jump edge, run jump state machine
        if (_jumpState != JumpExecutionState.NONE)
        {
            StepJumpEdge(dt, currentPos);
            return;
        }

        _repathTimer += dt;

        float repathInterval = _settings ? _settings.repathInterval : 0.25f;
        float reachRadius = _settings ? _settings.reachRadius : 0.4f;
        float k = _settings ? _settings.inputSmoothingK : 10f;
        float maxTurnRate = _settings ? _settings.maxTurnRate : 8f;

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
            var step = _path[_pathIndex];
            var targetWp = step.node;
            var edge = step.edge;

            // Check if this step is a JUMP edge
            if (edge != null && (edge.type == EdgeType.JUMP_ATTACH || edge.type == EdgeType.JUMP_GAP))
            {
                _jumpState = JumpExecutionState.APPROACH;
                _jumpStateTimer = 0f;
                StepJumpEdge(dt, currentPos);
                return;
            }

            float distToWp = Vector3.Distance(currentPos, targetWp.Position);
            if (distToWp < reachRadius)
            {
                _pathIndex++;
                if (_pathIndex < _path.Count)
                {
                    targetWp = _path[_pathIndex].node;
                    edge = _path[_pathIndex].edge;

                    if (edge != null && (edge.type == EdgeType.JUMP_ATTACH || edge.type == EdgeType.JUMP_GAP))
                    {
                        _jumpState = JumpExecutionState.APPROACH;
                        _jumpStateTimer = 0f;
                        StepJumpEdge(dt, currentPos);
                        return;
                    }
                }
            }

            if (_pathIndex < _path.Count)
            {
                Vector3 toWp = targetWp.Position - currentPos;
                Vector3 desiredDir = Vector3.ProjectOnPlane(toWp, _controller.SurfaceNormal).normalized;

                float turnStep = maxTurnRate * dt;
                Vector3 turnCapped = Vector3.RotateTowards(_smoothedMoveDir, desiredDir, turnStep, 0f);
                float lerpFactor = 1f - Mathf.Exp(-k * dt);
                _smoothedMoveDir = Vector3.Lerp(turnCapped, desiredDir, lerpFactor);
                MoveDir = _smoothedMoveDir.normalized;
                return;
            }
        }

        // If path exhausted but target is in sight
        Vector3 direct = Vector3.ProjectOnPlane(targetPos - currentPos, _controller.SurfaceNormal).normalized;
        _smoothedMoveDir = Vector3.Lerp(_smoothedMoveDir, direct, 1f - Mathf.Exp(-k * dt));
        MoveDir = _smoothedMoveDir.normalized;
    }

    private void StepJumpEdge(float dt, Vector3 currentPos)
    {
        if (_path == null || _pathIndex >= _path.Count)
        {
            _jumpState = JumpExecutionState.NONE;
            return;
        }

        var step = _path[_pathIndex];
        var edge = step.edge;
        var targetWp = step.node;
        var fromNode = _pathIndex > 0 ? _path[_pathIndex - 1].node : null;

        Vector3 takeoffPos = edge != null && edge.jumpParams.takeoffPos != Vector3.zero
            ? edge.jumpParams.takeoffPos
            : (fromNode != null ? fromNode.Position : currentPos);

        Vector3 heading = edge != null && edge.jumpParams.heading != Vector3.zero
            ? edge.jumpParams.heading
            : (targetWp.Position - takeoffPos);
        heading.y = 0f;
        if (heading.sqrMagnitude > 0.001f) heading.Normalize();

        _jumpStateTimer += dt;
        float takeoffTolerance = _settings ? _settings.takeoffTolerance : 0.25f;
        float alignAngle = _settings ? _settings.alignAngle : 15f;
        float jumpTimeout = _settings ? _settings.jumpTimeout : 2.5f;

        switch (_jumpState)
        {
            case JumpExecutionState.APPROACH:
                Vector3 toTakeoff = takeoffPos - currentPos;
                toTakeoff.y = 0f;
                float distToTakeoff = toTakeoff.magnitude;

                Vector3 desiredDir = distToTakeoff > 0.05f ? toTakeoff.normalized : heading;
                float k = _settings ? _settings.inputSmoothingK : 10f;
                _smoothedMoveDir = Vector3.Lerp(_smoothedMoveDir, desiredDir, 1f - Mathf.Exp(-k * dt));
                MoveDir = _smoothedMoveDir.normalized;

                float currentAngle = Vector3.Angle(_controller.transform.forward, heading);
                if (distToTakeoff < takeoffTolerance)
                {
                    if (currentAngle < alignAngle)
                        _jumpState = JumpExecutionState.LAUNCH;
                    else
                    {
                        _jumpState = JumpExecutionState.ALIGN;
                        _jumpStateTimer = 0f;
                    }
                }
                else if (_jumpStateTimer > 3f)
                {
                    RegisterEdgeFailure(edge);
                    _jumpState = JumpExecutionState.NONE;
                    ForceRepath();
                }
                break;

            case JumpExecutionState.ALIGN:
                MoveDir = heading;
                float angle = Vector3.Angle(_controller.transform.forward, heading);
                if (angle < alignAngle || _jumpStateTimer > 0.4f)
                {
                    _jumpState = JumpExecutionState.LAUNCH;
                }
                break;

            case JumpExecutionState.LAUNCH:
                Jump = true;
                MoveDir = heading;
                _jumpState = JumpExecutionState.AIR;
                _jumpStateTimer = 0f;
                if (_debugLogs)
                    Debug.Log($"[RatBrain] LAUNCH jump toward {targetWp.name}");
                break;

            case JumpExecutionState.AIR:
                MoveDir = heading;

                if (_controller.IsAttached || (_controller.IsGrounded && _jumpStateTimer > 0.3f && _controller.SurfaceCollider == targetWp.surfaceCollider))
                {
                    _jumpState = JumpExecutionState.LAND;
                }
                else if (_controller.IsGrounded && _jumpStateTimer > 0.35f)
                {
                    RegisterEdgeFailure(edge);
                    _jumpState = JumpExecutionState.NONE;
                    ForceRepath();
                }
                else if (_jumpStateTimer > jumpTimeout)
                {
                    RegisterEdgeFailure(edge);
                    _jumpState = JumpExecutionState.NONE;
                    ForceRepath();
                }
                break;

            case JumpExecutionState.LAND:
                if (_debugLogs)
                    Debug.Log($"[RatBrain] Jump LAND success! Now attached/grounded at {targetWp.name}");
                _jumpState = JumpExecutionState.NONE;
                _pathIndex++;
                break;
        }
    }

    private void CheckStuck(float dt, Vector3 currentPos)
    {
        if (_jumpState == JumpExecutionState.AIR) return;

        _stuckCheckTimer += dt;
        if (_stuckCheckTimer >= 0.5f)
        {
            _stuckCheckTimer = 0f;
            float movedDist = Vector3.Distance(currentPos, _lastStuckCheckPos);
            _lastStuckCheckPos = currentPos;

            bool wantsToMove = MoveDir.sqrMagnitude > 0.01f;
            if (movedDist < 0.08f && wantsToMove)
            {
                _stuckTimer += 0.5f;
                if (_stuckTimer >= 4.0f)
                {
                    if (_path != null && _pathIndex < _path.Count && _path[_pathIndex].edge != null)
                        _path[_pathIndex].edge.blocked = true;
                    ForceRepath();
                    _stuckTimer = 0f;
                }
                else if (_stuckTimer >= 2.0f)
                {
                    Jump = true;
                    MoveDir = (_controller.transform.right + _controller.transform.forward).normalized;
                }
                else if (_stuckTimer >= 1.0f)
                {
                    ForceRepath();
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }
    }

    private void RegisterEdgeFailure(RatEdge edge)
    {
        if (edge != null)
        {
            edge.cost *= 1.5f;
            if (_debugLogs)
                Debug.LogWarning($"[RatBrain] Edge {edge.from?.name} -> {edge.to?.name} failed! Cost increased to {edge.cost:F2}");
        }
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
