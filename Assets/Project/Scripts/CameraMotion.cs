using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CameraMotion : MonoBehaviour, ICameraMotion
{
    [SerializeField] float _sensitivity = 0.1f;
    [SerializeField] float _smoothness = 0.07f;
    [SerializeField] float _distance = 1f;
    [SerializeField] private float min = 90;
    [SerializeField] private float max = 360 - 30;
    [SerializeField] bool _isInTightSpace = false;
    [SerializeField] Vector3 _offset;
    [SerializeField] Vector3 _lookInput;
    [SerializeField] Camera _camera;
    // [SerializeField]  _camera;
    [SerializeField] Camera _fpsCamera;
    [SerializeField] Transform _followTarget;
    [SerializeField] Transform _origin;
    [SerializeField] private LayerMask _ignoreLayers;

    private Coroutine _lookTransitionRoutine;
    private Collider[] _nearbyColliders;
    private Vector3 targetLookDirection;
    private float hitDistance = 0;
    private bool _doLookFollow;
    private float xRot;
    public float yRot;
    
    private void Awake()
    {
        targetLookDirection = _followTarget.forward;
        _camera.transform.localPosition = Vector3.back * _distance;
        // _ignoreLayers = ~_followTarget.gameObject.layer;
        //if (transform.parent == _followTarget)
    }


    private void Start()
    {
        _nearbyColliders = new Collider[10];
        transform.parent = null;
        _offset = transform.position - _followTarget.position;
    }

    private void FixedUpdate()
    {
        bool isHit = Physics.Raycast(_origin.position, -transform.forward, out var hitInfo, _distance, ~_ignoreLayers);
        hitDistance = isHit ? Vector3.Distance(_followTarget.position, hitInfo.point) - 0.08f : _distance;
        
        _isInTightSpace = Physics.OverlapSphereNonAlloc(_origin.position, 0.2f, _nearbyColliders, ~_ignoreLayers) > 2;
        // if (_nearbyColliders is { Length: > 0 })
        // {
        //     foreach (var nearbyCollider in _nearbyColliders)
        //     {
        //         if (nearbyCollider == null) continue;
        //         print($"{nearbyCollider.name}");
        //     }
        // }
        hitDistance = Mathf.Clamp(_isInTightSpace ? 0 : hitDistance, 0.06f, _distance);

        //hitDistance = Mathf.Min(0, hitDistance);
        
    }

    void LateUpdate()
    {
        SetLookInput();
        //Vector3 offset = transform.right * _offset.x + transform.up * _offset.y + transform.forward * _offset.z;
        Vector3 offset = _followTarget.TransformPoint(_offset);
        transform.position = offset;
        
        // _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, Vector3.back * hitDistance, 20 * Time.deltaTime);
        _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, Vector3.back * hitDistance, 1);
        
        xRot -= _lookInput.y * _sensitivity;
        yRot += _lookInput.x * _sensitivity;
        
        yRot = Mathf.Repeat(yRot + 180f, 360f) - 180f;
        xRot = Mathf.Clamp(xRot, min, max);

        if (_doLookFollow)
        {   
            transform.rotation = Quaternion.Slerp(transform.rotation, 
                Quaternion.LookRotation((_followTarget.forward - 0.5f * _followTarget.up).normalized, _followTarget.up),
                8 * Time.deltaTime);
        }
        else
        {
            // Apply horizontal rotation around the world/player's up axis
            var horizontalRotation = Quaternion.AngleAxis(_lookInput.x * _sensitivity, Vector3.up);
            targetLookDirection = horizontalRotation * targetLookDirection;

            // Apply vertical rotation around the camera's current right axis (perpendicular to look direction)
            var rightAxis = Vector3.Cross(Vector3.up, targetLookDirection).normalized;
            var verticalRotation = Quaternion.AngleAxis(-_lookInput.y * _sensitivity, rightAxis);
            targetLookDirection = verticalRotation * targetLookDirection;
            targetLookDirection = ClampVerticalDirection(targetLookDirection, -70, 30);
            
            var targetRotation = Quaternion.LookRotation(targetLookDirection);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, (1/_smoothness) * Time.deltaTime);
            // transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yRot, 0) * Quaternion.Euler(xRot, 0, 0), 15 * Time.deltaTime);
        }
    }

    private void SetLookInput()
    {
        var xLook = TouchInputManager.InputMain.move.ReadValue<Vector2>().x * 10;
        _lookInput = new Vector3(0.5f * xLook + TouchInputManager.DeltaR.x, TouchInputManager.DeltaR.y, 0);
    }

    public void StartDirectionFollow()
    {
        if(_lookTransitionRoutine != null) StopCoroutine(_lookTransitionRoutine);
        _lookTransitionRoutine = StartCoroutine(StartFollowRoutine());
    }

    private IEnumerator StartFollowRoutine(float delay = 0.6f)
    {
        yield return new WaitForSeconds(delay);
        _doLookFollow = true;
    }

    public void EndDirectionFollow()
    {
        if(_lookTransitionRoutine != null) StopCoroutine(_lookTransitionRoutine);
        _doLookFollow = false;
        yRot = _followTarget.transform.eulerAngles.y;
    }

    public void SetFov(float fov, float time = 0)
    {
        if(time <= 0)
            _camera.fieldOfView = fov;
        else
            _camera.DOFieldOfView(fov, time);
    }

    public void SetDistance(float distance, float time = 0)
    {
        if (time <= 0)
            _distance = distance;
        else
            DOTween.To(() => _distance, x => _distance = x, distance, time);
    }
    
    Vector3 ClampVerticalDirection(Vector3 direction, float minAngle, float maxAngle)
    {
        // Convert angles to the vertical component range
        float minY = Mathf.Sin(minAngle * Mathf.Deg2Rad);
        float maxY = Mathf.Sin(maxAngle * Mathf.Deg2Rad);
    
        // Clamp the y component
        float clampedY = Mathf.Clamp(direction.y, minY, maxY);
    
        // If y didn't change, return original
        if (Mathf.Approximately(clampedY, direction.y))
            return direction;
    
        // Reconstruct the horizontal component to maintain unit length
        float horizontalMagnitude = Mathf.Sqrt(Mathf.Max(0f, 1f - clampedY * clampedY));
    
        // Get the current horizontal direction (x, z)
        Vector3 horizontalDir = new Vector3(direction.x, 0f, direction.z);
    
        // If horizontal direction is too small, pick a default direction
        if (horizontalDir.sqrMagnitude < 0.001f)
        {
            horizontalDir = Vector3.forward;
        }
        else
        {
            horizontalDir.Normalize();
        }
    
        // Reconstruct the clamped direction
        return new Vector3(
            horizontalDir.x * horizontalMagnitude,
            clampedY,
            horizontalDir.z * horizontalMagnitude
        );
    }}
//xRot = Mathf.Clamp(xRot, 70, 220);
