using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CineCameraMotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineFreeLook _cinemachine;
    [SerializeField] private float _xLookSpeed = 1;
    [SerializeField] private float _yLookSpeed = 1;

    [Space(20)]
    private Vector2 _additionalAdjustment = Vector2.zero;
    private float _time = 0;
    private float _duration = 0;

    private void Update()
    {
        Vector2 lookInput = TouchInputManager.DeltaR;

        _cinemachine.m_XAxis.Value += 10f * _xLookSpeed * lookInput.x * Time.deltaTime;
        _cinemachine.m_YAxis.Value -= 0.1f * _yLookSpeed * lookInput.y * Time.deltaTime;

        //if(_time < _duration)
        //{
        //    _cinemachine.m_XAxis.Value += 100f * _additionalAdjustment.x * Time.deltaTime;
        //    _cinemachine.m_YAxis.Value -= _additionalAdjustment.y * Time.deltaTime;
        //    _time += Time.deltaTime;
        //}
    }

    public void AdjustCameraXY(float x, float y, float smoothTime)
    {
        _additionalAdjustment.x = x / smoothTime;
        _additionalAdjustment.y = y / smoothTime;
        _duration = smoothTime;
        _time = 0f;
    }

    public void SetLowFOVandLook()
    {
        _xLookSpeed = 0.2f;
        _yLookSpeed = 0.2f;
        _cinemachine.m_Lens.FieldOfView = 20f;
    }

    public void SetNormalFOVandLook()
    {
        _xLookSpeed = 1f;
        _yLookSpeed = 1f;
        _cinemachine.m_Lens.FieldOfView = 40f;
    }

    public void SwitchLookSpeed(bool high)
    {
        if (high)
        {
            _xLookSpeed = 0.6f;
            _yLookSpeed = 0.6f;
        }
        else
        {
            _xLookSpeed = 0.2f;
            _yLookSpeed = 0.4f;
        }
    }
}
