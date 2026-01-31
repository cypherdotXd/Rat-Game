using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

public class MobileCameraInputController : InputAxisControllerBase<MobileCameraInputController.MyReader>
{
    // We process user input on the Update clock
    void Update()
    {
        if (Application.isPlaying)
            UpdateControllers();
    }
    
    [Serializable]
    public class MyReader : IInputAxisReader
    {
        public InputActionReference m_action;
        public InputActionReference m_lookAction;
        public float multiplier = 1.0f;
        Vector2 m_Value; // the cached value of the input

        // IInputAxisReader interface: Called by the framework to read the input value
        public float GetValue(Object context, IInputAxisOwner.AxisDescriptor.Hints hint)
        {
            // if (EventSystem.current.IsPointerOverGameObject(0))
            //     return 0;
            var lookInput = TouchInputManager.DeltaR;
            var xLook = m_action.action.ReadValue<Vector2>().x;
            m_Value = new Vector3(multiplier * xLook + 15f * lookInput.x, -lookInput.y);
            return (hint == IInputAxisOwner.AxisDescriptor.Hints.Y ? m_Value.y : m_Value.x);
        }
    }
}