using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraMotion : MonoBehaviour
{
    [SerializeField] float _sensitivity = 0.1f;
    [SerializeField] float _distance = 1f;
    [SerializeField] bool _isInTightSpace = false;
    [SerializeField] Vector3 _offset;
    [SerializeField] Camera _camera;
    [SerializeField] Camera _fpsCamera;
    [SerializeField] Transform _followTarget;

    [Space(20)]
    private Collider[] _nearbyColliders;
    private LayerMask _ignoreLayers;
    float hitDistance = 0;

    private void Awake()
    {
        _camera.transform.localPosition = Vector3.back * _distance;
        _ignoreLayers = ~_followTarget.gameObject.layer;
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
        bool isHit = Physics.Raycast(_followTarget.position, -transform.forward, out RaycastHit hitInfo, _distance);
        hitDistance = isHit ? Vector3.Distance(_followTarget.position, hitInfo.point) - 0.05f : _distance;

        
        _isInTightSpace = Physics.OverlapSphereNonAlloc(_followTarget.position, _distance, _nearbyColliders) > 2;
        hitDistance = _isInTightSpace ? 0 : hitDistance;

        //hitDistance = Mathf.Min(0, hitDistance);
        
    }

    void LateUpdate()
    {
        Vector3 lookInput = TouchInputManager.DeltaR;

        //Vector3 offset = transform.right * _offset.x + transform.up * _offset.y + transform.forward * _offset.z;
        Vector3 offset = _followTarget.TransformPoint(_offset);
        transform.position = offset;
        
        _camera.transform.localPosition = Vector3.Lerp(_camera.transform.localPosition, Vector3.back * hitDistance, 1/_sensitivity * Time.deltaTime);


        float xRot = transform.eulerAngles.x - lookInput.y * _sensitivity;
        float min = 60;
        float max = 360 - 30;
        // Clamp xRot to be either greater than 300 or less than 15
        if (xRot >= min && xRot <= max)
        {
            // Determine whether to set xRot to 15 or 300 based on proximity
            xRot = (xRot - min < max - xRot) ? min : max;
        }
        float yRot = transform.eulerAngles.y + lookInput.x * _sensitivity;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yRot, 0) * Quaternion.Euler(xRot, 0, 0), 15 * Time.deltaTime);
    }
}
//xRot = Mathf.Clamp(xRot, 70, 220);
