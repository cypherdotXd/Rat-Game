using System;
using UnityEngine;
using UnityEngine.Splines;

public class PathClimb : MonoBehaviour
{
    [SerializeField] private bool _reversed;
    [SerializeField] private int _segments;
    [SerializeField] private SplineContainer _spline;
    [SerializeField] private LineRenderer _line;
    [SerializeField] private Interactable _interactable;
    
    public float pathClimbed; // 0 to 1

    private void OnValidate()
    {
        DrawLine(_segments);
    }

    private void OnEnable()
    {
        _interactable.OnInteract += Attach;
    }

    private void OnDisable()
    {
        _interactable.OnInteract -= Attach;
    }

    private void Start()
    {
        DrawLine(_segments);
    }

    private void Attach(GameObject interactor)
    {
        if(!interactor.CompareTag("Player")) return;
        pathClimbed = 0.01f;
        var isPlayer = interactor.TryGetComponent(out MovementController movement);
        if(isPlayer)
            movement.NotifyClimbPathBegin(this);
    }

    public bool ClimbIncrementally(Transform target, float increment, float startClimb = -1f)
    {
        if(startClimb >= 0)
            pathClimbed = startClimb;
        pathClimbed = Mathf.Clamp(pathClimbed + increment, 0f, 1f);
        Climb(target, pathClimbed);
        if (pathClimbed is > 0 and < 1) return true;
        pathClimbed = 0;
        return false;
    }
    
    public void Climb(Transform target, float climbPercent)
    {
        _spline.Evaluate(climbPercent, out var p, out var t, out var upVector);
        target.position = p;
        target.rotation = Quaternion.LookRotation(t, -upVector);
    }

    private void DrawLine(int resolution)
    {
        _line.positionCount = resolution;
        for (int i = 0; i < _line.positionCount; i++)
        {
            var p = _spline.EvaluatePosition((float)(i + 1) / _line.positionCount);
            _line.SetPosition(i, p);
        }
    }
}
