using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraMotion : MonoBehaviour
{

    private Vector2 _additionalAdjustment = Vector2.zero;
    [Space(20)]

    public Camera _camera;
    public Transform _followTarget;
    public float _sensitivity = 0.1f;
    public Vector3 _offset;

    private void Start()
    {
        _offset = transform.position - _followTarget.position;
    }

    void LateUpdate()
    {
        Vector3 lookInput = TouchInputManager.DeltaR;

        transform.position = _followTarget.position + _offset;

        float yRot = transform.eulerAngles.y + lookInput.x * _sensitivity;
        transform.rotation = Quaternion.Euler(transform.eulerAngles.x, yRot, transform.eulerAngles.z);
        float xRot = transform.localEulerAngles.x - lookInput.y * _sensitivity;
        transform.localRotation = Quaternion.Euler(xRot, transform.localEulerAngles.y, transform.localEulerAngles.z);
    }

    public void AdjustCameraXY(float x, float y, float smoothTime)
    {
        _additionalAdjustment.x = x / smoothTime;
        _additionalAdjustment.y = y / smoothTime;
        //_duration = smoothTime;
        //_time = 0f;
    }

    public void SetLowFOVandLook()
    {
        //_xLookSpeed = 0.2f;
        //_yLookSpeed = 0.2f;
        //_cinemachine.m_Lens.FieldOfView = 20f;
    }

    public void SetNormalFOVandLook()
    {
        //_xLookSpeed = 1f;
        //_yLookSpeed = 1f;
        //_cinemachine.m_Lens.FieldOfView = 40f;
    }

    public void SwitchLookSpeed(bool high)
    {
        //if (high)
        //{
        //    _xLookSpeed = 0.6f;
        //    _yLookSpeed = 0.6f;
        //}
        //else
        //{
        //    _xLookSpeed = 0.2f;
        //    _yLookSpeed = 0.4f;
        //}
    }
}
